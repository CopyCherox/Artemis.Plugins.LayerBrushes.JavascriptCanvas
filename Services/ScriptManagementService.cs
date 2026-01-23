using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services
{
    public class ScriptManagementService
    {
        private readonly ObservableCollection<JavascriptScriptModel> _scripts;

        public ScriptManagementService(ObservableCollection<JavascriptScriptModel> scripts)
        {
            _scripts = scripts;
        }

        public JavascriptScriptModel AddNewScript()
        {
            var newScriptName = $"Custom Script {_scripts.Count + 1}";
            var newScript = new JavascriptScriptModel
            {
                ScriptName = newScriptName,
                JavaScriptCode = "// New script\nctx.clear(0, 255, 0);",
                IsEnabled = false
            };

            _scripts.Add(newScript);
            ScriptsFolderManager.SaveScriptToFile(newScript.ScriptName, newScript.JavaScriptCode);

            // FIX: DON'T call NotifyScriptsChanged() here!
            // It causes a reload from disk which invalidates the object reference
            // ScriptsFolderManager.NotifyScriptsChanged();

            return newScript;
        }

        public void DeleteScript(JavascriptScriptModel script)
        {
            _scripts.Remove(script);
            ScriptsFolderManager.DeleteScriptFile(script.ScriptName);
        }

        // ... rest of the class remains the same
        public async Task<bool> ExportScript(JavascriptScriptModel script, Window mainWindow)
        {
            try
            {
                var storageProvider = mainWindow.StorageProvider;
                var file = await storageProvider.SaveFilePickerAsync(
                    new Avalonia.Platform.Storage.FilePickerSaveOptions
                    {
                        Title = "Export Script",
                        SuggestedFileName = $"{script.ScriptName}.json",
                        DefaultExtension = "json",
                        FileTypeChoices = new[]
                        {
                            new Avalonia.Platform.Storage.FilePickerFileType("JSON Files")
                            {
                                Patterns = new[] { "*.json" }
                            }
                        }
                    });

                if (file != null)
                {
                    var exportData = new
                    {
                        ScriptName = script.ScriptName,
                        JavaScriptCode = script.JavaScriptCode
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(exportData,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                    await using var stream = await file.OpenWriteAsync();
                    await using var writer = new System.IO.StreamWriter(stream);
                    await writer.WriteAsync(json);

                    return true;
                }
            }
            catch (Exception)
            {
                // Silently handle errors
            }

            return false;
        }

        public async Task<(int successCount, int errorCount)> ImportScripts(Window mainWindow)
        {
            int successCount = 0;
            int errorCount = 0;

            try
            {
                var storageProvider = mainWindow.StorageProvider;
                var files = await storageProvider.OpenFilePickerAsync(
                    new Avalonia.Platform.Storage.FilePickerOpenOptions
                    {
                        Title = "Import Script(s)",
                        AllowMultiple = true,
                        FileTypeFilter = new[]
                        {
                            new Avalonia.Platform.Storage.FilePickerFileType("JSON Files")
                            {
                                Patterns = new[] { "*.json" }
                            }
                        }
                    });

                foreach (var file in files)
                {
                    try
                    {
                        await using var stream = await file.OpenReadAsync();
                        using var reader = new System.IO.StreamReader(stream);
                        var json = await reader.ReadToEndAsync();
                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
                        var root = jsonDoc.RootElement;

                        var scriptName = root.GetProperty("ScriptName").GetString();
                        var scriptCode = root.GetProperty("JavaScriptCode").GetString();

                        if (string.IsNullOrEmpty(scriptName) || string.IsNullOrEmpty(scriptCode))
                        {
                            errorCount++;
                            continue;
                        }

                        var importedScript = new JavascriptScriptModel
                        {
                            ScriptName = EnsureUniqueScriptName(scriptName),
                            JavaScriptCode = scriptCode,
                            IsEnabled = false
                        };

                        _scripts.Add(importedScript);
                        ScriptsFolderManager.SaveScriptToFile(importedScript.ScriptName, importedScript.JavaScriptCode);
                        successCount++;
                    }
                    catch
                    {
                        errorCount++;
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle errors
            }

            return (successCount, errorCount);
        }

        private string EnsureUniqueScriptName(string baseName)
        {
            if (!_scripts.Any(s => s.ScriptName == baseName))
                return baseName;

            int counter = 1;
            string newName = $"{baseName} ({counter})";

            while (_scripts.Any(s => s.ScriptName == newName))
            {
                counter++;
                newName = $"{baseName} ({counter})";
            }

            return newName;
        }
    }
}