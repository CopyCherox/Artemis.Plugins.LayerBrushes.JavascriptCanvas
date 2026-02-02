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
            string? pluginDirectory = null;

            // Try assembly location first
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                pluginDirectory = Path.GetDirectoryName(assemblyLocation);
            }

            // Fallback: Search for folder containing our DLL
            if (string.IsNullOrEmpty(pluginDirectory))
            {
                var dllName = "Artemis.Plugins.LayerBrushes.JavascriptCanvas.dll";
                var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var artemisRoot = Path.Combine(programData, "Artemis");

                var searchPaths = new[]
                {
            Path.Combine(artemisRoot, "Plugins"),
            Path.Combine(artemisRoot, "workshop")
        };

                foreach (var searchPath in searchPaths)
                {
                    if (Directory.Exists(searchPath))
                    {
                        // Recursively search for our DLL
                        var foundFiles = Directory.GetFiles(searchPath, dllName, SearchOption.AllDirectories);
                        if (foundFiles.Length > 0)
                        {
                            // Use the directory of the most recently modified DLL
                            var mostRecent = foundFiles.OrderByDescending(File.GetLastWriteTime).First();
                            pluginDirectory = Path.GetDirectoryName(mostRecent);
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(pluginDirectory))
            {
                throw new InvalidOperationException(
                    $"Could not determine plugin directory. Assembly.Location is empty and DLL not found in Artemis folders.");
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