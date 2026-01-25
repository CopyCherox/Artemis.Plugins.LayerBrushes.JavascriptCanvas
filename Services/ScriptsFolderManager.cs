using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services
{
    public static class ScriptsFolderManager
    {
        private static readonly string ScriptsFolder;
        private static readonly string MetadataFolder;

        public static event EventHandler? ScriptsChanged;

        static ScriptsFolderManager()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var pluginDirectory = Path.GetDirectoryName(assemblyLocation);

            if (string.IsNullOrEmpty(pluginDirectory) || !pluginDirectory.Contains("Plugins"))
            {
                var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var pluginsRoot = Path.Combine(programData, "Artemis", "Plugins");

                if (Directory.Exists(pluginsRoot))
                {
                    var pluginFolders = Directory.GetDirectories(pluginsRoot, "Artemis.Plugins.LayerBrushes.JavascriptCanvas*");
                    if (pluginFolders.Length > 0)
                    {
                        pluginDirectory = pluginFolders[0];
                    }
                    else
                    {
                        pluginDirectory = Path.Combine(pluginsRoot, "Artemis.Plugins.LayerBrushes.JavascriptCanvas");
                    }
                }
                else
                {
                    pluginDirectory = Path.Combine(programData, "Artemis", "Plugins", "JavascriptCanvas");
                }
            }

            ScriptsFolder = Path.Combine(pluginDirectory, "Scripts");
            MetadataFolder = Path.Combine(pluginDirectory, "Metadata");
            EnsureScriptsFolderExists();
        }

        public static string GetScriptsFolder() => ScriptsFolder;

        public static void NotifyScriptsChanged()
        {
            ScriptsChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void EnsureScriptsFolderExists()
        {
            try
            {
                if (!Directory.Exists(ScriptsFolder))
                {
                    Directory.CreateDirectory(ScriptsFolder);
                    CreateDefaultScripts();
                }

                if (!Directory.Exists(MetadataFolder))
                {
                    Directory.CreateDirectory(MetadataFolder);
                }
            }
            catch (Exception)
            {
                // Silently handle folder creation errors
            }
        }

        private static void CreateDefaultScripts()
        {
            var defaultScripts = new[]
            {
                new { Name = "Moving Rainbow Wave", Code = @"for (let x = 0; x < width; x++) {
    let hue = (x / width + time * 0.5) % 1.0;
    let rgb = ctx.hslToRgb(hue, 1.0, 0.5);
    ctx.fillStyle(rgb.r, rgb.g, rgb.b);
    ctx.fillRect(x, 0, 1, height);
}" },
                new { Name = "Breathing Pulse", Code = @"let brightness = (Math.sin(time * 2) + 1) / 2;
let color = brightness * 255;
ctx.clear(color * 1, color * 0.4, 0);" },
                new { Name = "Moving Gradient", Code = @"for (let x = 0; x < width; x++) {
    let pos = (x / width + time * 0.3) % 1.0;
    let r = Math.floor(255 * pos);
    let g = Math.floor(128 * (1 - pos));
    let b = Math.floor(200 * Math.sin(pos * Math.PI));
    ctx.fillStyle(r, g, b);
    ctx.fillRect(x, 0, 1, height);
}" },
                new { Name = "Fire Effect", Code = @"for (let x = 0; x < width; x++) {
    for (let y = 0; y < height; y++) {
        let yPos = y / height;
        let noise = Math.sin(x * 0.1 + time * 3) * 0.5 + 0.5;
        let intensity = (1 - yPos) * noise;
        let r = Math.floor(255 * intensity);
        let g = Math.floor(100 * intensity * 0.5);
        ctx.fillStyle(r, g, 0);
        ctx.fillRect(x, y, 1, 1);
    }
}" }
            };

            foreach (var script in defaultScripts)
            {
                SaveScriptToFile(script.Name, script.Code);
            }
        }

        public static ObservableCollection<JavascriptScriptModel> LoadAllScripts()
        {
            var scripts = new ObservableCollection<JavascriptScriptModel>();

            try
            {
                if (!Directory.Exists(ScriptsFolder))
                    return scripts;

                var jsonFiles = Directory.GetFiles(ScriptsFolder, "*.json");

                foreach (var filePath in jsonFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        using var jsonDoc = JsonDocument.Parse(json);
                        var root = jsonDoc.RootElement;

                        var scriptName = root.GetProperty("ScriptName").GetString();
                        var code = root.GetProperty("JavaScriptCode").GetString();

                        if (!string.IsNullOrEmpty(scriptName) && !string.IsNullOrEmpty(code))
                        {
                            scripts.Add(new JavascriptScriptModel
                            {
                                ScriptName = scriptName,
                                JavaScriptCode = code,
                                IsEnabled = false
                            });
                        }
                    }
                    catch (Exception)
                    {
                        // Skip invalid files
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle errors
            }

            return scripts;
        }

        public static void SaveScriptToFile(string scriptName, string code)
        {
            try
            {
                var sanitizedName = string.Join("_", scriptName.Split(Path.GetInvalidFileNameChars()));
                var filePath = Path.Combine(ScriptsFolder, $"{sanitizedName}.json");

                var scriptData = new
                {
                    ScriptName = scriptName,
                    JavaScriptCode = code
                };

                var json = JsonSerializer.Serialize(scriptData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch (Exception)
            {
                // Silently handle errors
            }
        }

        public static void DeleteScriptFile(string scriptName)
        {
            try
            {
                var sanitizedName = string.Join("_", scriptName.Split(Path.GetInvalidFileNameChars()));
                var filePath = Path.Combine(ScriptsFolder, $"{sanitizedName}.json");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    NotifyScriptsChanged();
                }
            }
            catch (Exception)
            {
                // Silently handle errors
            }
        }

        public static void RenameScriptFile(string oldName, string newName)
        {
            try
            {
                var oldSanitized = string.Join("_", oldName.Split(Path.GetInvalidFileNameChars()));
                var newSanitized = string.Join("_", newName.Split(Path.GetInvalidFileNameChars()));
                var oldPath = Path.Combine(ScriptsFolder, $"{oldSanitized}.json");

                if (!File.Exists(oldPath))
                    return;

                var json = File.ReadAllText(oldPath);
                using var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;
                var code = root.GetProperty("JavaScriptCode").GetString() ?? "";

                SaveScriptToFile(newName, code);
                File.Delete(oldPath);
                //NotifyScriptsChanged();
            }
            catch (Exception)
            {
                // Silently handle errors
            }
        }

        public static void SaveEnabledScript(string brushId, string scriptName)
        {
            try
            {
                var metadataPath = Path.Combine(MetadataFolder, $"{brushId}.json");
                var metadata = new { EnabledScript = scriptName };
                File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata));
            }
            catch (Exception)
            {
                // Silently handle errors
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
            catch (Exception)
            {
                // Silently handle errors
            }

            return null;
        }

        private class EnabledScriptMetadata
        {
            public string? EnabledScript { get; set; }
        }
    }
}