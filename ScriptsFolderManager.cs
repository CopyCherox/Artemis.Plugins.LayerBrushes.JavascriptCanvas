using Artemis.Plugins.LayerBrushes.JavascriptCanvas;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public static class ScriptsFolderManager
    {
        private static readonly string ScriptsFolder;
        private static readonly string MetadataFolder;

        // Event to notify all instances when scripts change
        public static event EventHandler? ScriptsChanged;

        static ScriptsFolderManager()
        {
            // Get the actual plugin directory from where the assembly is loaded
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var pluginDirectory = Path.GetDirectoryName(assemblyLocation);

            // If that doesn't work, construct the path manually
            if (string.IsNullOrEmpty(pluginDirectory) || !pluginDirectory.Contains("Plugins"))
            {
                // Fallback: Use ProgramData\Artemis\Plugins
                var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                // Find the plugin folder by searching for our plugin
                var pluginsRoot = Path.Combine(programData, "Artemis", "Plugins");

                if (Directory.Exists(pluginsRoot))
                {
                    // Look for folder starting with our plugin name
                    var pluginFolders = Directory.GetDirectories(pluginsRoot, "Artemis.Plugins.LayerBrushes.JavascriptCanvas*");

                    if (pluginFolders.Length > 0)
                    {
                        pluginDirectory = pluginFolders[0];
                        System.Diagnostics.Debug.WriteLine($"📁 Found plugin folder: {pluginDirectory}");
                    }
                    else
                    {
                        // Create a default folder
                        pluginDirectory = Path.Combine(pluginsRoot, "Artemis.Plugins.LayerBrushes.JavascriptCanvas");
                        System.Diagnostics.Debug.WriteLine($"📁 Using default plugin folder: {pluginDirectory}");
                    }
                }
                else
                {
                    // Last resort fallback
                    pluginDirectory = Path.Combine(programData, "Artemis", "Plugins", "JavascriptCanvas");
                }
            }

            ScriptsFolder = Path.Combine(pluginDirectory, "Scripts");
            MetadataFolder = Path.Combine(pluginDirectory, "Metadata");

            System.Diagnostics.Debug.WriteLine($"📁 Plugin Directory: {pluginDirectory}");
            System.Diagnostics.Debug.WriteLine($"📁 Scripts Folder: {ScriptsFolder}");

            EnsureScriptsFolderExists();
        }

        public static string GetScriptsFolder() => ScriptsFolder;

        // Call this after any file operation that should notify other instances
        // Change from private to public so we can manually trigger it if needed
        public static void NotifyScriptsChanged()
        {
            var handlerCount = ScriptsChanged?.GetInvocationList()?.Length ?? 0;
            System.Diagnostics.Debug.WriteLine($"📢 NotifyScriptsChanged called - {handlerCount} subscribers");
            ScriptsChanged?.Invoke(null, EventArgs.Empty);
        }


        private static void EnsureScriptsFolderExists()
        {
            try
            {
                if (!Directory.Exists(ScriptsFolder))
                {
                    Directory.CreateDirectory(ScriptsFolder);
                    System.Diagnostics.Debug.WriteLine($"📁 Created scripts folder: {ScriptsFolder}");
                    CreateDefaultScripts();
                }

                if (!Directory.Exists(MetadataFolder))
                {
                    Directory.CreateDirectory(MetadataFolder);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to create scripts folder: {ex.Message}");
            }
        }

        private static void CreateDefaultScripts()
        {
            var defaultScripts = new[]
            {
                new { Name = "Moving Rainbow Wave", Code = @"// Moving rainbow wave
for (let x = 0; x < width; x++) {
  let hue = (x / width + time * 0.5) % 1.0;
  let rgb = ctx.hslToRgb(hue, 1.0, 0.5);
  ctx.fillStyle(rgb.r, rgb.g, rgb.b);
  ctx.fillRect(x, 0, 1, height);
}" },

                new { Name = "Breathing Pulse", Code = @"// Breathing pulse effect
let brightness = (Math.sin(time * 2) + 1) / 2;
let color = brightness * 255;
ctx.clear(color * 1, color * 0.4, 0);" },

                new { Name = "Moving Gradient", Code = @"// Moving gradient
for (let x = 0; x < width; x++) {
  let pos = (x / width + time * 0.3) % 1.0;
  let r = Math.floor(255 * pos);
  let g = Math.floor(128 * (1 - pos));
  let b = Math.floor(200 * Math.sin(pos * Math.PI));
  ctx.fillStyle(r, g, b);
  ctx.fillRect(x, 0, 1, height);
}" },

                new { Name = "Fire Effect", Code = @"// Fire effect
for (let x = 0; x < width; x++) {
  for (let y = 0; y < height; y++) {
    let yPos = y / height;
    let noise = Math.sin(x * 0.1 + time * 3) * 0.5 + 0.5;
    let intensity = (1 - yPos) * noise;
    let r = Math.floor(255 * intensity);
    let g = Math.floor(100 * intensity * 0.5);
    let b = 0;
    ctx.fillStyle(r, g, b);
    ctx.fillRect(x, y, 1, 1);
  }
}" },

                new { Name = "Scan Line", Code = @"// Moving scan line
ctx.clear(0, 0, 50);
let pos = (time * 0.5) % 1.0;
let x = Math.floor(pos * width);
ctx.fillStyle(0, 255, 255);
ctx.fillRect(x - 2, 0, 5, height);" },

                new { Name = "Plasma Waves", Code = @"// Optimized plasma waves
ctx.clear(0, 0, 0);

const step = 8;
const t1 = time * 2;
const t2 = time * 3;
const t3 = time;

for (let y = 0; y < height; y += step) {
  const yFactor = y * 0.01;
  
  for (let x = 0; x < width; x += step) {
    const xFactor = x * 0.01;
    
    let value = Math.sin(xFactor + t1) + 
                Math.sin(yFactor + t2) + 
                Math.sin((xFactor + yFactor) + t3);
    
    let r = 128 + 127 * Math.sin(value);
    let g = 128 + 127 * Math.sin(value + 2);
    let b = 128 + 127 * Math.sin(value + 4);
    
    ctx.fillStyle(r, g, b, 255);
    ctx.fillRect(x, y, step, step);
  }
}" }
            };

            foreach (var script in defaultScripts)
            {
                SaveScriptToFile(script.Name, script.Code);
            }

            System.Diagnostics.Debug.WriteLine($"✅ Created {defaultScripts.Length} default scripts");
        }

        public static ObservableCollection<JavascriptScriptModel> LoadAllScripts()
        {
            var scripts = new ObservableCollection<JavascriptScriptModel>();

            try
            {
                EnsureScriptsFolderExists();

                var jsonFiles = Directory.GetFiles(ScriptsFolder, "*.json");
                System.Diagnostics.Debug.WriteLine($"📂 Loading {jsonFiles.Length} scripts from: {ScriptsFolder}");

                foreach (var filePath in jsonFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        using var jsonDoc = JsonDocument.Parse(json);
                        var root = jsonDoc.RootElement;

                        var scriptName = root.GetProperty("ScriptName").GetString();
                        var scriptCode = root.GetProperty("JavaScriptCode").GetString();

                        if (!string.IsNullOrEmpty(scriptName) && !string.IsNullOrEmpty(scriptCode))
                        {
                            scripts.Add(new JavascriptScriptModel
                            {
                                ScriptName = scriptName,
                                JavaScriptCode = scriptCode,
                                IsEnabled = false
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Failed to load script {filePath}: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Loaded {scripts.Count} scripts");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load scripts: {ex.Message}");
            }

            return scripts;
        }

        public static void SaveScriptToFile(string scriptName, string code)
        {
            try
            {
                EnsureScriptsFolderExists();

                // Sanitize filename
                var safeFileName = string.Join("_", scriptName.Split(Path.GetInvalidFileNameChars()));
                var filePath = Path.Combine(ScriptsFolder, $"{safeFileName}.json");

                // Create JSON in same format as export
                var exportData = new
                {
                    ScriptName = scriptName,
                    JavaScriptCode = code
                };

                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);

                System.Diagnostics.Debug.WriteLine($"💾 Saved script: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to save script '{scriptName}': {ex.Message}");
            }
        }

        public static void DeleteScriptFile(string scriptName)
        {
            try
            {
                var safeFileName = string.Join("_", scriptName.Split(Path.GetInvalidFileNameChars()));
                var filePath = Path.Combine(ScriptsFolder, $"{safeFileName}.json");

                System.Diagnostics.Debug.WriteLine($"🔍 Attempting to delete: {filePath}");
                System.Diagnostics.Debug.WriteLine($"🔍 File exists: {File.Exists(filePath)}");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    System.Diagnostics.Debug.WriteLine($"🗑️ Successfully deleted script: {filePath}");

                    // Notify other instances
                    NotifyScriptsChanged();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ File not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to delete script '{scriptName}': {ex.Message}");
            }
        }

        public static void RenameScriptFile(string oldName, string newName)
        {
            try
            {
                var oldSafeName = string.Join("_", oldName.Split(Path.GetInvalidFileNameChars()));
                var newSafeName = string.Join("_", newName.Split(Path.GetInvalidFileNameChars()));
                var oldPath = Path.Combine(ScriptsFolder, $"{oldSafeName}.json");
                var newPath = Path.Combine(ScriptsFolder, $"{newSafeName}.json");

                if (File.Exists(oldPath) && !File.Exists(newPath))
                {
                    var json = File.ReadAllText(oldPath);
                    using var jsonDoc = JsonDocument.Parse(json);
                    var root = jsonDoc.RootElement;
                    var code = root.GetProperty("JavaScriptCode").GetString() ?? "";

                    SaveScriptToFile(newName, code);
                    File.Delete(oldPath);
                    System.Diagnostics.Debug.WriteLine($"Renamed script file {oldName} → {newName}");

                    // KEEP THIS to notify other layers:
                    NotifyScriptsChanged();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to rename script: {ex.Message}");
            }
        }




        // Save which script is enabled (per brush instance)
        public static void SaveEnabledScript(string brushId, string scriptName)
        {
            try
            {
                var metadataPath = Path.Combine(MetadataFolder, $"{brushId}.json");
                var metadata = new { EnabledScript = scriptName };
                File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to save metadata: {ex.Message}");
            }
        }

        public static string? LoadEnabledScript(string brushId)
        {
            try
            {
                var metadataPath = Path.Combine(MetadataFolder, $"{brushId}.json");
                if (File.Exists(metadataPath))
                {
                    var json = File.ReadAllText(metadataPath);
                    var metadata = JsonSerializer.Deserialize<EnabledScriptMetadata>(json);
                    return metadata?.EnabledScript;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load metadata: {ex.Message}");
            }

            return null;
        }

        private class EnabledScriptMetadata
        {
            public string? EnabledScript { get; set; }
        }
    }
}
