using Artemis.Core.LayerBrushes;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas;
using Artemis.UI.Shared.LayerBrushes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.ViewModels
{
    public class JavascriptCanvasBrushConfigurationViewModel : BrushConfigurationViewModel
    {
        private readonly JavaScriptExecutor jsExecutor;
        private readonly JavascriptCanvasBrush brush;
        private double time = 0;
        private SKBitmap? previewBitmap;
        private JavascriptScriptModel? selectedScript;
        private int canvasWidth = 630;
        private int canvasHeight = 250;
        private string errorMessage = string.Empty;
        private string currentEditorCode = string.Empty;
        private DispatcherTimer? updateTimer;
        private readonly Dictionary<JavascriptScriptModel, string> _originalScriptNames = new Dictionary<JavascriptScriptModel, string>();

        private bool hasUnsavedChanges = false;
        private string savedEditorCode = string.Empty; 
        private string savedScriptName = string.Empty;



        private int frameSkip = 2;
        private bool _isRenamingLocally = false;



        public int FrameSkip
        {
            get => frameSkip;
            set
            {
                if (frameSkip != value)
                {
                    frameSkip = value;
                    this.RaisePropertyChanged();

                    if (brush?.Properties?.UpdateEveryNFrames != null)
                    {
                        brush.Properties.UpdateEveryNFrames.SetCurrentValue(value);
                    }
                }
            }
        }



        public JavascriptCanvasBrushConfigurationViewModel(JavascriptCanvasBrush layerBrush) : base(layerBrush)
        {
            brush = layerBrush;
            jsExecutor = new JavaScriptExecutor();

            var vmInstanceId = this.GetHashCode();
            var propsInstanceId = brush.Properties.GetHashCode();

            System.Diagnostics.Debug.WriteLine($"🎨 Creating ViewModel {vmInstanceId} for Properties {propsInstanceId}");

            // ✅ SUBSCRIBE FIRST - before accessing Scripts property
            brush.Properties.ScriptsRefreshed += OnScriptsRefreshedHandler;

            System.Diagnostics.Debug.WriteLine($"✅ ViewModel subscribing to ScriptsRefreshed event");


            var subscriberCount = brush.Properties.GetScriptsRefreshedSubscriberCount();
            System.Diagnostics.Debug.WriteLine($"✅ ViewModel {vmInstanceId} subscribed to Properties {propsInstanceId} - now has {subscriberCount} subscriber(s)");

            // NOW access Scripts (this will trigger Properties.Scripts getter)
            Scripts = brush.Properties.Scripts;

            // Track original names of all scripts
            foreach (var script in Scripts)
            {
                _originalScriptNames[script] = script.ScriptName;
                script.PropertyChanged += Script_PropertyChanged;
            }

            SelectedScript = Scripts.FirstOrDefault(s => s.IsEnabled) ?? Scripts.FirstOrDefault();

            // Initialize editor code
            if (SelectedScript != null)
            {
                currentEditorCode = SelectedScript.JavaScriptCode;
            }

            // Initialize frame skip from brush property
            frameSkip = brush.Properties.UpdateEveryNFrames?.CurrentValue ?? 2;

            // Subscribe to CurrentValueSet event
            if (brush.Properties.UpdateEveryNFrames != null)
            {
                brush.Properties.UpdateEveryNFrames.CurrentValueSet += (sender, args) =>
                {
                    var newValue = brush.Properties.UpdateEveryNFrames.CurrentValue;
                    if (frameSkip != newValue)
                    {
                        frameSkip = newValue;
                        this.RaisePropertyChanged(nameof(FrameSkip));
                    }
                };
            }

            AddScriptCommand = ReactiveCommand.Create(AddScript);
            DeleteScriptCommand = ReactiveCommand.Create(DeleteScript, this.WhenAnyValue(x => x.SelectedScript).Select(s => s != null));
            ApplyScriptCommand = ReactiveCommand.Create(ApplyScript);
            ExportScriptCommand = ReactiveCommand.Create(ExportScript,
                this.WhenAnyValue(x => x.SelectedScript).Select(s => s != null));
            ImportScriptCommand = ReactiveCommand.Create(ImportScript);

            // Watch for script selection changes
            this.WhenAnyValue(x => x.SelectedScript)
                .Subscribe(script =>
                {
                    if (script != null && script.JavaScriptCode != currentEditorCode)
                    {
                        currentEditorCode = script.JavaScriptCode;
                        savedEditorCode = script.JavaScriptCode; // Store as saved version
                        savedScriptName = script.ScriptName; // ADD THIS - Store saved name
                        this.RaisePropertyChanged(nameof(EditorCode));
                        time = 0;
                    }

                    // Reset unsaved changes when switching scripts
                    HasUnsavedChanges = false;
                });



            // Start animation timer
            StartPreviewTimer();

            System.Diagnostics.Debug.WriteLine($"✅ ViewModel initialized - has {Scripts.Count} scripts");
        }

        // ✅ Extract the handler to a separate method
        private void OnScriptsRefreshedHandler(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📥 ViewModel received ScriptsRefreshed event, reloading...");

            // Skip refresh if we're the one doing the rename
            if (_isRenamingLocally)
            {
                System.Diagnostics.Debug.WriteLine("📥 Ignoring refresh - local rename in progress");
                return;
            }

            // ✅ Remember the currently selected script NAME (not reference)
            var previouslySelectedName = SelectedScript?.ScriptName;

            // Unsubscribe from old scripts
            foreach (var script in Scripts)
            {
                script.PropertyChanged -= Script_PropertyChanged;
            }

            // Reload the scripts reference
            Scripts = brush.Properties.Scripts;
            System.Diagnostics.Debug.WriteLine($"📥 Reloaded {Scripts.Count} scripts");

            // Re-track all scripts
            _originalScriptNames.Clear();
            foreach (var script in Scripts)
            {
                _originalScriptNames[script] = script.ScriptName;
                script.PropertyChanged += Script_PropertyChanged;
            }

            // ✅ Re-select by NAME, not by reference
            if (!string.IsNullOrEmpty(previouslySelectedName))
            {
                SelectedScript = Scripts.FirstOrDefault(s => s.ScriptName == previouslySelectedName)
                                ?? Scripts.FirstOrDefault(s => s.IsEnabled)
                                ?? Scripts.FirstOrDefault();
            }
            else
            {
                SelectedScript = Scripts.FirstOrDefault(s => s.IsEnabled) ?? Scripts.FirstOrDefault();
            }

            System.Diagnostics.Debug.WriteLine($"📥 Selected script: {SelectedScript?.ScriptName ?? "none"}");
            this.RaisePropertyChanged(nameof(Scripts));
            this.RaisePropertyChanged(nameof(SelectedScript));
        }



        // ✅ NEW: Handle property changes on scripts (especially IsGlobal)
        private System.Timers.Timer? _renameTimer;
        private JavascriptScriptModel? _scriptBeingRenamed;

        private void Script_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not JavascriptScriptModel script) return;

            if (e.PropertyName == nameof(JavascriptScriptModel.ScriptName))
            {
                // Check if this is the currently selected script
                if (script == SelectedScript)
                {
                    // Mark as unsaved if name changed
                    HasUnsavedChanges = (script.ScriptName != savedScriptName);
                }

                // ✅ Debounce: Wait for user to stop typing before processing
                _renameTimer?.Stop();
                _scriptBeingRenamed = script;
                _renameTimer = new System.Timers.Timer(1000); // 1 second delay
                _renameTimer.Elapsed += (s, args) =>
                {
                    _renameTimer.Stop();
                    // Process the rename on UI thread
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        FinalizeScriptRename(_scriptBeingRenamed);
                    });
                };
                _renameTimer.Start();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"🔔 Script property changed: {e.PropertyName} on '{script.ScriptName}'");
        }


        private bool isRenamingLocally = false;

        private void FinalizeScriptRename(JavascriptScriptModel? script)
        {
            if (script == null || !_originalScriptNames.TryGetValue(script, out var originalName))
                return;

            if (originalName == script.ScriptName)
                return;

            // Validate
            if (string.IsNullOrWhiteSpace(script.ScriptName))
            {
                script.ScriptName = originalName;
                return;
            }

            if (Scripts.Any(s => s != script && s.ScriptName == script.ScriptName))
            {
                script.ScriptName = originalName;
                System.Diagnostics.Debug.WriteLine($"⚠️ Duplicate name, reverted to '{originalName}'");
                return;
            }

            // Store the new name before renaming
            var newScriptName = script.ScriptName;
            var wasSelected = (script == SelectedScript);

            // Rename the file
            System.Diagnostics.Debug.WriteLine($"✏️ Renaming '{originalName}' → '{newScriptName}'");

            ScriptsFolderManager.RenameScriptFile(originalName, newScriptName);
            _originalScriptNames[script] = newScriptName;

            // Force refresh to get new object references after rename
            // Use Dispatcher to ensure file system has processed the rename
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Reload scripts from Properties (which loads from disk)
                var previouslySelectedName = SelectedScript?.ScriptName;

                // Unsubscribe from old scripts
                foreach (var s in Scripts)
                {
                    s.PropertyChanged -= Script_PropertyChanged;
                }

                // Reload
                Scripts = brush.Properties.Scripts;

                // Re-track all scripts
                _originalScriptNames.Clear();
                foreach (var s in Scripts)
                {
                    _originalScriptNames[s] = s.ScriptName;
                    s.PropertyChanged += Script_PropertyChanged;
                }

                // Re-select the renamed script by name
                if (wasSelected && !string.IsNullOrEmpty(newScriptName))
                {
                    SelectedScript = Scripts.FirstOrDefault(s => s.ScriptName == newScriptName);
                    savedScriptName = newScriptName;
                }

                // Update saved name and clear unsaved flag
                HasUnsavedChanges = false;

                this.RaisePropertyChanged(nameof(Scripts));
                this.RaisePropertyChanged(nameof(SelectedScript));

                System.Diagnostics.Debug.WriteLine($"✅ Refreshed after rename, selected: {SelectedScript?.ScriptName ?? "none"}");

            }, Avalonia.Threading.DispatcherPriority.Background);
        }





        private void StartPreviewTimer()
        {
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };

            // And in the tick handler:
            updateTimer.Tick += (s, e) =>
            {
                time += 0.05;
                UpdatePreview();
            };
            updateTimer.Start();
        }

        public ObservableCollection<JavascriptScriptModel> Scripts { get; private set; }


        public JavascriptScriptModel? SelectedScript
        {
            get => selectedScript;
            set => this.RaiseAndSetIfChanged(ref selectedScript, value);
        }

        public string EditorCode
        {
            get => currentEditorCode;
            set
            {
                if (currentEditorCode != value)
                {
                    currentEditorCode = value;
                    this.RaisePropertyChanged();

                    // Update the selected script's code
                    if (SelectedScript != null)
                    {
                        SelectedScript.JavaScriptCode = value;
                    }

                    // Check if different from saved version
                    HasUnsavedChanges = (currentEditorCode != savedEditorCode);
                }
            }
        }


        public bool HasUnsavedChanges
        {
            get => hasUnsavedChanges;
            private set => this.RaiseAndSetIfChanged(ref hasUnsavedChanges, value);
        }


        private void CheckForUnsavedChanges()
        {
            if (SelectedScript == null)
            {
                HasUnsavedChanges = false;
                return;
            }

            // Compare current editor code with what's saved in the file
            // We need to get the saved version from disk
            try
            {
                var scripts = ScriptsFolderManager.LoadAllScripts();
                var savedScript = scripts.FirstOrDefault(s => s.ScriptName == SelectedScript.ScriptName);

                if (savedScript != null)
                {
                    HasUnsavedChanges = currentEditorCode != savedScript.JavaScriptCode;
                }
                else
                {
                    HasUnsavedChanges = true; // New script not yet saved
                }
            }
            catch
            {
                HasUnsavedChanges = false;
            }
        }

        public SKBitmap? PreviewBitmap
        {
            get => previewBitmap;
            private set => this.RaiseAndSetIfChanged(ref previewBitmap, value);
        }

        public int CanvasWidth
        {
            get => canvasWidth;
            set
            {
                if (canvasWidth != value)
                {
                    canvasWidth = value;
                    this.RaisePropertyChanged();
                    time = 0;
                }
            }
        }

        public int CanvasHeight
        {
            get => canvasHeight;
            set
            {
                if (canvasHeight != value)
                {
                    canvasHeight = value;
                    this.RaisePropertyChanged();
                    time = 0;
                }
            }
        }

        public string ErrorMessage
        {
            get => errorMessage;
            private set => this.RaiseAndSetIfChanged(ref errorMessage, value);
        }

        public ReactiveCommand<Unit, Unit> AddScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyScriptCommand { get; }

        private void AddScript()
        {
            var newScriptName = $"Custom Script {Scripts.Count + 1}";

            var newScript = new JavascriptScriptModel
            {
                ScriptName = newScriptName,
                JavaScriptCode = "// New script\nctx.clear(0, 255, 0);",
                IsEnabled = false
            };

            newScript.PropertyChanged += Script_PropertyChanged;
            Scripts.Add(newScript);

            // Track original name
            _originalScriptNames[newScript] = newScript.ScriptName;

            // Save to file
            ScriptsFolderManager.SaveScriptToFile(newScript.ScriptName, newScript.JavaScriptCode);

            // Manually notify all instances
            System.Diagnostics.Debug.WriteLine($"➕ Added new script, manually notifying...");
            ScriptsFolderManager.NotifyScriptsChanged();

            // ✅ Find and select the new script by name (after potential refresh)
            // Use Dispatcher to ensure this happens after the refresh completes
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var scriptToSelect = Scripts.FirstOrDefault(s => s.ScriptName == newScriptName);
                if (scriptToSelect != null)
                {
                    SelectedScript = scriptToSelect;
                    this.RaisePropertyChanged(nameof(SelectedScript));
                    System.Diagnostics.Debug.WriteLine($"✅ Selected new script '{scriptToSelect.ScriptName}'");
                }
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        }





        private async void DeleteScript()
        {
            if (SelectedScript == null) return;

            var scriptToDelete = SelectedScript;

            // Show confirmation dialog
            var dialog = new Avalonia.Controls.Window
            {
                Title = "Confirm Delete",
                Width = 400,
                Height = 160,
                WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.Parse("#2D2D2D"))
            };

            var mainPanel = new StackPanel
            {
                Margin = new Thickness(30, 20, 30, 20),
                Spacing = 25,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Message text - centered
            mainPanel.Children.Add(new TextBlock
            {
                Text = $"Are you sure you want to delete '{scriptToDelete.ScriptName}'?",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Button container using Grid for perfect centering
            var buttonGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,15,Auto,*"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 120,
                Height = 32,
                Padding = new Thickness(10, 5),
                Background = new SolidColorBrush(Color.Parse("#95A5A6")),
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var deleteButton = new Button
            {
                Content = "Yes, Delete",
                Width = 120,
                Height = 32,
                Padding = new Thickness(10, 5),
                Background = new SolidColorBrush(Color.Parse("#E74C3C")),
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            cancelButton.Click += (s, e) => dialog.Close(false);
            deleteButton.Click += (s, e) => dialog.Close(true);

            Grid.SetColumn(cancelButton, 1);
            Grid.SetColumn(deleteButton, 3);

            buttonGrid.Children.Add(cancelButton);
            buttonGrid.Children.Add(deleteButton);

            mainPanel.Children.Add(buttonGrid);

            dialog.Content = mainPanel;

            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;

            if (mainWindow == null) return;

            var result = await dialog.ShowDialog<bool?>(mainWindow);

            // Only delete if user confirmed
            if (result == true)
            {
                var index = Scripts.IndexOf(scriptToDelete);

                // Remove from tracking dictionary
                _originalScriptNames.Remove(scriptToDelete);

                // Remove from collection
                Scripts.Remove(scriptToDelete);

                // Delete the file
                ScriptsFolderManager.DeleteScriptFile(scriptToDelete.ScriptName);

                // ✅ Select the next available script
                if (Scripts.Count > 0)
                {
                    // Try to select the script at the same index, or the previous one
                    var newIndex = Math.Min(index, Scripts.Count - 1);
                    SelectedScript = Scripts[newIndex];
                    this.RaisePropertyChanged(nameof(SelectedScript));
                    System.Diagnostics.Debug.WriteLine($"✅ Selected script '{SelectedScript.ScriptName}' after deletion");
                }
                else
                {
                    SelectedScript = null;
                    this.RaisePropertyChanged(nameof(SelectedScript));
                    System.Diagnostics.Debug.WriteLine($"⚠️ No scripts remaining");
                }

                System.Diagnostics.Debug.WriteLine($"🗑️ Deleted script '{scriptToDelete.ScriptName}' from collection and file");
            }
        }


        private void ApplyScript()
        {
            if (SelectedScript == null) return;

            // Update the script's code from editor
            SelectedScript.JavaScriptCode = currentEditorCode;

            // Save the code change
            ScriptsFolderManager.SaveScriptToFile(SelectedScript.ScriptName, SelectedScript.JavaScriptCode);

            // Enable this script, disable others
            foreach (var script in Scripts)
            {
                script.IsEnabled = (script == SelectedScript);
            }

            // ✅ Save the enabled state
            brush.Properties.SaveScripts();

            // Update saved version and clear unsaved flag
            savedEditorCode = currentEditorCode;
            HasUnsavedChanges = false;

            System.Diagnostics.Debug.WriteLine($"✅ Script '{SelectedScript.ScriptName}' applied and saved");
        }




        private void UpdatePreview()
        {
            if (SelectedScript == null || string.IsNullOrWhiteSpace(SelectedScript.JavaScriptCode))
            {
                ErrorMessage = "⚠ No script selected";
                return;
            }

            try
            {
                var oldBitmap = PreviewBitmap;

                // Execute the current script code
                PreviewBitmap = jsExecutor.ExecuteScriptOnCanvas(
                    SelectedScript.JavaScriptCode,
                    canvasWidth,
                    canvasHeight,
                    time
                );

                oldBitmap?.Dispose();

                // Check for errors from JavaScript executor
                if (!string.IsNullOrEmpty(jsExecutor.LastError))
                {
                    // Format error message with line/column info
                    if (jsExecutor.ErrorLine > 0)
                    {
                        ErrorMessage = $"❌ Line {jsExecutor.ErrorLine}, Col {jsExecutor.ErrorColumn}: {jsExecutor.LastError}";
                    }
                    else
                    {
                        ErrorMessage = $"❌ {jsExecutor.LastError}";
                    }
                }
                else
                {
                    ErrorMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                // Fallback for unexpected errors
                string errorMsg = ex.Message;
                if (errorMsg.Length > 200)
                    errorMsg = errorMsg.Substring(0, 200) + "...";

                ErrorMessage = $"❌ Unexpected error: {errorMsg}";
                System.Diagnostics.Debug.WriteLine($"Preview error: {ex}");
            }
        }





        public ReactiveCommand<Unit, Unit> ExportScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> ImportScriptCommand { get; }

        private async void ExportScript()
        {
            if (SelectedScript == null) return;

            // Store reference to avoid null warnings
            var scriptToExport = SelectedScript;

            try
            {
                var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow : null;

                if (mainWindow == null) return;

                var storageProvider = mainWindow.StorageProvider;
                var file = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Export Script",
                    SuggestedFileName = $"{scriptToExport.ScriptName}.json",  // Use scriptToExport
                    DefaultExtension = "json",
                    FileTypeChoices = new[]
                    {
                new Avalonia.Platform.Storage.FilePickerFileType("JSON Files")
                {
                    Patterns = new[] { "*.json" }
                },
                new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
                });

                if (file != null)
                {
                    // Save current editor code to script before export
                    scriptToExport.JavaScriptCode = currentEditorCode;  // Use scriptToExport

                    // Create anonymous object with only the fields we want to export
                    var exportData = new
                    {
                        ScriptName = scriptToExport.ScriptName,  // Use scriptToExport
                        JavaScriptCode = scriptToExport.JavaScriptCode  // Use scriptToExport
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(exportData,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                    await using var stream = await file.OpenWriteAsync();
                    await using var writer = new System.IO.StreamWriter(stream);
                    await writer.WriteAsync(json);

                    System.Diagnostics.Debug.WriteLine($"Script exported to: {file.Name}");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Export failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Export error: {ex}");
            }
        }




        private async void ImportScript()
        {
            try
            {
                var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow : null;

                if (mainWindow == null) return;

                var storageProvider = mainWindow.StorageProvider;

                var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Import Script(s)",
                    AllowMultiple = true,  // Changed to true
                    FileTypeFilter = new[]
                    {
                new Avalonia.Platform.Storage.FilePickerFileType("JSON Files")
                {
                    Patterns = new[] { "*.json" }
                },
                new Avalonia.Platform.Storage.FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
                });

                if (files.Count > 0)
                {
                    int successCount = 0;
                    int errorCount = 0;
                    JavascriptScriptModel? lastImportedScript = null;

                    foreach (var file in files)
                    {
                        try
                        {
                            await using var stream = await file.OpenReadAsync();
                            using var reader = new System.IO.StreamReader(stream);
                            var json = await reader.ReadToEndAsync();

                            // Deserialize to get only ScriptName and JavaScriptCode
                            using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
                            var root = jsonDoc.RootElement;

                            var scriptName = root.GetProperty("ScriptName").GetString();
                            var scriptCode = root.GetProperty("JavaScriptCode").GetString();

                            if (string.IsNullOrEmpty(scriptName) || string.IsNullOrEmpty(scriptCode))
                            {
                                System.Diagnostics.Debug.WriteLine($"Invalid script data in {file.Name}");
                                errorCount++;
                                continue;
                            }

                            // Create new script with imported data
                            var importedScript = new JavascriptScriptModel
                            {
                                ScriptName = scriptName,
                                JavaScriptCode = scriptCode,
                                IsEnabled = false
                            };

                            var existingScript = Scripts.FirstOrDefault(s =>
                                s.ScriptName == importedScript.ScriptName);

                            if (existingScript != null)
                            {
                                // For multiple imports, auto-rename conflicts
                                int counter = 1;
                                string newName = $"{importedScript.ScriptName} ({counter})";
                                while (Scripts.Any(s => s.ScriptName == newName))
                                {
                                    counter++;
                                    newName = $"{importedScript.ScriptName} ({counter})";
                                }
                                importedScript.ScriptName = newName;

                                System.Diagnostics.Debug.WriteLine($"Script '{scriptName}' renamed to '{newName}' due to conflict");
                            }

                            // After successfully adding an imported script:
                            importedScript.PropertyChanged += Script_PropertyChanged;
                            Scripts.Add(importedScript);
                            _originalScriptNames[importedScript] = importedScript.ScriptName; // Add this line
                            lastImportedScript = importedScript;
                            successCount++;


                            System.Diagnostics.Debug.WriteLine($"Script imported from: {file.Name}");
                        }
                        catch (Exception fileEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error importing {file.Name}: {fileEx.Message}");
                            errorCount++;
                        }
                    }

                    // Save all imported scripts at once
                    if (successCount > 0)
                    {
                        brush.Properties.SaveScripts();

                        // Select the last imported script
                        if (lastImportedScript != null)
                        {
                            SelectedScript = lastImportedScript;
                        }
                    }

                    // Show summary message
                    if (files.Count > 1)
                    {
                        var summaryParts = new System.Collections.Generic.List<string>();
                        if (successCount > 0) summaryParts.Add($"{successCount} imported");
                        if (errorCount > 0) summaryParts.Add($"{errorCount} failed");

                        var summaryMessage = $"Import complete: {string.Join(", ", summaryParts)}";
                        System.Diagnostics.Debug.WriteLine(summaryMessage);

                        // Show summary dialog
                        var summaryDialog = new Window
                        {
                            Title = "Import Complete",
                            Width = 350,
                            Height = 180,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            CanResize = false,
                            Background = new SolidColorBrush(Color.Parse("#2D2D2D"))
                        };

                        var panel = new StackPanel
                        {
                            Margin = new Thickness(20),
                            Spacing = 20
                        };

                        var messageText = new System.Text.StringBuilder();
                        messageText.AppendLine($"Imported {successCount} of {files.Count} script(s).");
                        if (errorCount > 0)
                        {
                            messageText.AppendLine($"\n{errorCount} script(s) failed to import.");
                        }

                        panel.Children.Add(new TextBlock
                        {
                            Text = messageText.ToString(),
                            FontSize = 14,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = Brushes.White
                        });

                        var okButton = new Button
                        {
                            Content = "OK",
                            Width = 100,
                            Padding = new Thickness(10, 5),
                            Background = new SolidColorBrush(Color.Parse("#3498DB")),
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };

                        okButton.Click += (s, e) => summaryDialog.Close();

                        panel.Children.Add(okButton);
                        summaryDialog.Content = panel;

                        await summaryDialog.ShowDialog(mainWindow);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Import failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Import error: {ex}");
            }
        }




        private void ScriptPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var script = sender as JavascriptScriptModel;
            if (script == null) return;

            if (e.PropertyName == nameof(JavascriptScriptModel.ScriptName))
            {
                // Save whenever script name changes
                brush.Properties.SaveScripts();
                System.Diagnostics.Debug.WriteLine($"Script renamed to {script.ScriptName} and saved");
            }
            // Note: Don't save on JavaScriptCode changes to avoid saving on every keystroke
        }




        public new void Dispose()
        {
            System.Diagnostics.Debug.WriteLine($"🗑️ Disposing ViewModel {this.GetHashCode()} for Properties {brush.Properties.GetHashCode()}");

            // Unsubscribe from events
            brush.Properties.ScriptsRefreshed -= OnScriptsRefreshedHandler;

            var remainingSubscribers = brush.Properties.GetScriptsRefreshedSubscriberCount();
            System.Diagnostics.Debug.WriteLine($"🗑️ After unsubscribe - {remainingSubscribers} subscriber(s) remaining");

            // Save scripts before disposing
            brush.Properties.SaveScripts();

            updateTimer?.Stop();
            updateTimer = null;
            previewBitmap?.Dispose();
            previewBitmap = null;
            jsExecutor?.Dispose();

            // Unsubscribe from all script property changes
            foreach (var script in Scripts)
            {
                script.PropertyChanged -= Script_PropertyChanged;
            }

            base.Dispose();
        }

        ~JavascriptCanvasBrushConfigurationViewModel()
        {
            Dispose();
        }

    }
}