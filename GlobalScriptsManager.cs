using Artemis.Plugins.LayerBrushes.JavascriptCanvas;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public static class GlobalScriptsManager
    {
        private static readonly string GlobalScriptsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Artemis",
            "HTMLCanvas_GlobalScripts.json"
        );


        public static void SaveGlobalScripts(ObservableCollection<JavascriptScriptModel> allScripts)
        {
            try
            {
                // Filter only global scripts
                var globalScripts = new ObservableCollection<JavascriptScriptModel>();
                foreach (var script in allScripts)
                {
                    if (script.IsGlobal)
                    {
                        globalScripts.Add(script);
                    }
                }

                // ✅ Debug output
                System.Diagnostics.Debug.WriteLine($"💾 Saving {globalScripts.Count} global scripts to: {GlobalScriptsPath}");

                var json = JsonSerializer.Serialize(globalScripts, new JsonSerializerOptions { WriteIndented = true });

                // Ensure directory exists
                var directory = Path.GetDirectoryName(GlobalScriptsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    System.Diagnostics.Debug.WriteLine($"📁 Created directory: {directory}");
                }

                File.WriteAllText(GlobalScriptsPath, json);
                System.Diagnostics.Debug.WriteLine($"✅ Global scripts saved successfully");

                // ✅ Verify by reading back
                var savedContent = File.ReadAllText(GlobalScriptsPath);
                System.Diagnostics.Debug.WriteLine($"📄 Saved content:\n{savedContent}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to save global scripts: {ex.Message}");
            }
        }

        public static ObservableCollection<JavascriptScriptModel> LoadGlobalScripts()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📂 Loading global scripts from: {GlobalScriptsPath}");

                if (File.Exists(GlobalScriptsPath))
                {
                    var json = File.ReadAllText(GlobalScriptsPath);
                    System.Diagnostics.Debug.WriteLine($"📄 Loaded JSON:\n{json}");

                    var scripts = JsonSerializer.Deserialize<ObservableCollection<JavascriptScriptModel>>(json);
                    if (scripts != null)
                    {
                        // Ensure all loaded scripts are marked as global
                        foreach (var script in scripts)
                        {
                            script.IsGlobal = true;
                        }

                        System.Diagnostics.Debug.WriteLine($"✅ Loaded {scripts.Count} global scripts");
                        return scripts;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Global scripts file does not exist yet");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load global scripts: {ex.Message}\n{ex.StackTrace}");
            }

            return new ObservableCollection<JavascriptScriptModel>();
        }

    }
}