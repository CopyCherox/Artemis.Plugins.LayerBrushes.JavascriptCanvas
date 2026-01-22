using Artemis.Core.LayerBrushes;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services;
using Artemis.UI.Shared.LayerBrushes;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.ViewModels
{
    public class JavascriptCanvasBrushConfigurationViewModel : BrushConfigurationViewModel
    {
        private readonly JavascriptCanvasBrush _brush;
        private readonly PreviewRenderingService _previewService;
        private readonly ScriptManagementService _scriptManager;
        private readonly DialogService _dialogService;
        private readonly Dictionary<JavascriptScriptModel, string> _originalScriptNames = new();

        private JavascriptScriptModel? _selectedScript;
        private int _canvasWidth = 630;
        private int _canvasHeight = 250;
        private string _errorMessage = string.Empty;
        private string _currentEditorCode = string.Empty;
        private bool _hasUnsavedChanges = false;
        private string _savedEditorCode = string.Empty;
        private string _savedScriptName = string.Empty;
        private int _frameSkip = 2;
        private SKBitmap? _previewBitmap;

        // Time control fields
        private bool _isPreviewPaused = false;
        private double _timeScale = 1.0;
        private double _currentTime = 0;

        public JavascriptCanvasBrushConfigurationViewModel(JavascriptCanvasBrush layerBrush)
            : base(layerBrush)
        {
            _brush = layerBrush;
            _dialogService = new DialogService();

            // Subscribe to events
            _brush.Properties.ScriptsRefreshed += OnScriptsRefreshed;

            // Load scripts
            Scripts = _brush.Properties.Scripts;

            // Track original names and subscribe to property changes
            foreach (var script in Scripts)
            {
                _originalScriptNames[script] = script.ScriptName;
                script.PropertyChanged += Script_PropertyChanged;
            }

            // Initialize script manager
            _scriptManager = new ScriptManagementService(Scripts);

            // Setup preview service
            _previewService = new PreviewRenderingService();
            _previewService.PreviewUpdated += (s, bitmap) => PreviewBitmap = bitmap;
            _previewService.ErrorOccurred += (s, error) => ErrorMessage = error;

            // Subscribe to time control events
            _previewService.TimeScaleChanged += (s, scale) =>
            {
                TimeScale = scale;
            };

            _previewService.PausedChanged += (s, paused) =>
            {
                IsPreviewPaused = paused;
                this.RaisePropertyChanged(nameof(PlayPauseText));
            };

            _previewService.TimeChanged += (s, time) =>
            {
                CurrentTime = time;
            };

            // Initialize
            SelectedScript = Scripts.FirstOrDefault(s => s.IsEnabled) ?? Scripts.FirstOrDefault();
            _frameSkip = _brush.Properties.UpdateEveryNFrames?.CurrentValue ?? 2;

            // Setup commands
            AddScriptCommand = ReactiveCommand.Create(AddScript);
            DeleteScriptCommand = ReactiveCommand.Create(DeleteScript,
                this.WhenAnyValue(x => x.SelectedScript).Select(s => s != null));
            ApplyScriptCommand = ReactiveCommand.Create(ApplyScript);
            ExportScriptCommand = ReactiveCommand.Create(ExportScript,
                this.WhenAnyValue(x => x.SelectedScript).Select(s => s != null));
            ImportScriptCommand = ReactiveCommand.Create(ImportScript);

            // Time control commands
            PlayPauseCommand = ReactiveCommand.Create(TogglePlayPause);
            ResetTimeCommand = ReactiveCommand.Create(ResetTime);
            SetSpeedCommand = ReactiveCommand.Create<double>(SetSpeed);

            // Watch for changes
            this.WhenAnyValue(x => x.SelectedScript).Subscribe(OnScriptSelected);
            this.WhenAnyValue(x => x.EditorCode).Subscribe(code =>
            {
                _previewService.SetScript(code);
            });
            this.WhenAnyValue(x => x.CanvasWidth, x => x.CanvasHeight).Subscribe(_ =>
            {
                _previewService.SetCanvasSize(_canvasWidth, _canvasHeight);
            });

            // Start preview timer AFTER everything is set up
            _previewService.StartPreviewTimer(50);
        }

        public ObservableCollection<JavascriptScriptModel> Scripts { get; private set; }

        public JavascriptScriptModel? SelectedScript
        {
            get => _selectedScript;
            set => this.RaiseAndSetIfChanged(ref _selectedScript, value);
        }

        public string EditorCode
        {
            get => _currentEditorCode;
            set
            {
                if (_currentEditorCode != value)
                {
                    _currentEditorCode = value;
                    this.RaisePropertyChanged();
                    if (SelectedScript != null)
                        SelectedScript.JavaScriptCode = value;
                    HasUnsavedChanges = (_currentEditorCode != _savedEditorCode ||
                        (SelectedScript != null && SelectedScript.ScriptName != _savedScriptName));
                }
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
        }

        public SKBitmap? PreviewBitmap
        {
            get => _previewBitmap;
            private set => this.RaiseAndSetIfChanged(ref _previewBitmap, value);
        }

        public int CanvasWidth
        {
            get => _canvasWidth;
            set
            {
                if (_canvasWidth != value)
                {
                    _canvasWidth = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public int CanvasHeight
        {
            get => _canvasHeight;
            set
            {
                if (_canvasHeight != value)
                {
                    _canvasHeight = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public int FrameSkip
        {
            get => _frameSkip;
            set
            {
                if (_frameSkip != value)
                {
                    _frameSkip = value;
                    this.RaisePropertyChanged();
                    if (_brush?.Properties?.UpdateEveryNFrames != null)
                        _brush.Properties.UpdateEveryNFrames.SetCurrentValue(value);
                }
            }
        }

        // Time control properties
        public bool IsPreviewPaused
        {
            get => _isPreviewPaused;
            private set => this.RaiseAndSetIfChanged(ref _isPreviewPaused, value);
        }

        public double TimeScale
        {
            get => _timeScale;
            private set
            {
                if (Math.Abs(_timeScale - value) > 0.001)
                {
                    _timeScale = value;
                    this.RaisePropertyChanged();
                    this.RaisePropertyChanged(nameof(TimeScaleText));
                }
            }
        }

        public double CurrentTime
        {
            get => _currentTime;
            private set => this.RaiseAndSetIfChanged(ref _currentTime, value);
        }

        public string TimeScaleText => $"{_timeScale:F2}x";
        public string PlayPauseText => _isPreviewPaused ? "▶ Play" : "⏸ Pause";

        // Commands
        public ReactiveCommand<Unit, Unit> AddScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> ImportScriptCommand { get; }

        // Time control commands
        public ReactiveCommand<Unit, Unit> PlayPauseCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetTimeCommand { get; }
        public ReactiveCommand<double, Unit> SetSpeedCommand { get; }

        // Time control methods
        private void TogglePlayPause()
        {
            _previewService.SetPaused(!_previewService.IsPaused);
        }

        private void ResetTime()
        {
            _previewService.ResetTime();
        }

        private void SetSpeed(double speed)
        {
            _previewService.SetTimeScale(Math.Clamp(speed, 0.1, 10.0));
        }

        private void Script_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not JavascriptScriptModel script) return;

            if (e.PropertyName == nameof(JavascriptScriptModel.ScriptName))
            {
                // Check if this is the currently selected script
                if (script == SelectedScript)
                {
                    // Mark as unsaved if name changed
                    HasUnsavedChanges = (script.ScriptName != _savedScriptName || _currentEditorCode != _savedEditorCode);
                }
            }
        }

        private void OnScriptSelected(JavascriptScriptModel? script)
        {
            if (script != null && script.JavaScriptCode != _currentEditorCode)
            {
                _currentEditorCode = script.JavaScriptCode;
                _savedEditorCode = script.JavaScriptCode;
                _savedScriptName = script.ScriptName;
                this.RaisePropertyChanged(nameof(EditorCode));
                _previewService.ResetTime();
                _previewService.SetScript(script.JavaScriptCode);
                HasUnsavedChanges = false;
            }
        }

        private void AddScript()
        {
            var newScript = _scriptManager.AddNewScript();
            // Track original name and subscribe to property changes
            _originalScriptNames[newScript] = newScript.ScriptName;
            newScript.PropertyChanged += Script_PropertyChanged;
            SelectedScript = Scripts.FirstOrDefault(s => s.ScriptName == newScript.ScriptName);
        }

        private async void DeleteScript()
        {
            if (SelectedScript == null) return;

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            bool confirmed = await _dialogService.ShowDeleteConfirmation(mainWindow, SelectedScript.ScriptName);
            if (confirmed)
            {
                var index = Scripts.IndexOf(SelectedScript);
                var scriptToDelete = SelectedScript;

                // Remove from tracking
                _originalScriptNames.Remove(scriptToDelete);
                scriptToDelete.PropertyChanged -= Script_PropertyChanged;

                _scriptManager.DeleteScript(scriptToDelete);

                SelectedScript = Scripts.Count > 0 ? Scripts[Math.Min(index, Scripts.Count - 1)] : null;
            }
        }

        private void ApplyScript()
        {
            if (SelectedScript == null) return;

            // Update the script's code from editor
            SelectedScript.JavaScriptCode = _currentEditorCode;

            // Check if the script name changed
            if (_originalScriptNames.TryGetValue(SelectedScript, out var originalName))
            {
                if (originalName != SelectedScript.ScriptName)
                {
                    // Validate new name
                    if (string.IsNullOrWhiteSpace(SelectedScript.ScriptName))
                    {
                        ErrorMessage = "Script name cannot be empty.";
                        SelectedScript.ScriptName = originalName;
                        return;
                    }

                    // Check for duplicates
                    if (Scripts.Any(s => s != SelectedScript && s.ScriptName == SelectedScript.ScriptName))
                    {
                        ErrorMessage = $"A script named '{SelectedScript.ScriptName}' already exists.";
                        SelectedScript.ScriptName = originalName;
                        return;
                    }

                    // **SAVE THE NEW CODE TO THE OLD FILE FIRST** - before renaming!
                    ScriptsFolderManager.SaveScriptToFile(originalName, SelectedScript.JavaScriptCode);

                    // Now rename the file (it will copy the updated content)
                    System.Diagnostics.Debug.WriteLine($"Renaming script: {originalName} → {SelectedScript.ScriptName}");
                    ScriptsFolderManager.RenameScriptFile(originalName, SelectedScript.ScriptName);

                    // Update tracking
                    _originalScriptNames[SelectedScript] = SelectedScript.ScriptName;
                }
            }

            // Save the script (in case name didn't change)
            ScriptsFolderManager.SaveScriptToFile(SelectedScript.ScriptName, SelectedScript.JavaScriptCode);

            // Enable this script, disable others
            foreach (var script in Scripts)
                script.IsEnabled = (script == SelectedScript);

            // Save all scripts
            _brush.Properties.SaveScripts();

            // Update saved versions and clear unsaved flag
            _savedEditorCode = _currentEditorCode;
            _savedScriptName = SelectedScript.ScriptName;
            HasUnsavedChanges = false;

            System.Diagnostics.Debug.WriteLine($"✅ Script '{SelectedScript.ScriptName}' applied and saved");
        }

        private async void ExportScript()
        {
            if (SelectedScript == null) return;

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            // Save current editor code to the script before exporting
            SelectedScript.JavaScriptCode = _currentEditorCode;

            bool success = await _scriptManager.ExportScript(SelectedScript, mainWindow);
            if (success)
            {
                System.Diagnostics.Debug.WriteLine($"Script '{SelectedScript.ScriptName}' exported successfully.");
            }
            else
            {
                ErrorMessage = "Failed to export script.";
            }
        }

        private async void ImportScript()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            var (successCount, errorCount) = await _scriptManager.ImportScripts(mainWindow);

            if (successCount > 0)
            {
                // Track new scripts
                foreach (var script in Scripts)
                {
                    if (!_originalScriptNames.ContainsKey(script))
                    {
                        _originalScriptNames[script] = script.ScriptName;
                        script.PropertyChanged += Script_PropertyChanged;
                    }
                }

                // Save the imported scripts
                _brush.Properties.SaveScripts();

                // Show summary dialog if multiple files were selected
                if (successCount + errorCount > 1)
                {
                    await _dialogService.ShowImportSummary(mainWindow, successCount,
                        successCount + errorCount, errorCount);
                }

                // Select the last imported script
                var lastScript = Scripts.LastOrDefault();
                if (lastScript != null)
                {
                    SelectedScript = lastScript;
                }
            }
            else if (errorCount > 0)
            {
                ErrorMessage = "Failed to import script(s).";
            }
        }

        private void OnScriptsRefreshed(object? sender, EventArgs e)
        {
            var previouslySelectedName = SelectedScript?.ScriptName;

            // Unsubscribe from old scripts
            foreach (var script in Scripts)
            {
                script.PropertyChanged -= Script_PropertyChanged;
            }

            Scripts = _brush.Properties.Scripts;

            // Re-track all scripts
            _originalScriptNames.Clear();
            foreach (var script in Scripts)
            {
                _originalScriptNames[script] = script.ScriptName;
                script.PropertyChanged += Script_PropertyChanged;
            }

            this.RaisePropertyChanged(nameof(Scripts));

            if (!string.IsNullOrEmpty(previouslySelectedName))
                SelectedScript = Scripts.FirstOrDefault(s => s.ScriptName == previouslySelectedName);
        }

        private Avalonia.Controls.Window? GetMainWindow()
        {
            return Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null;
        }

        public new void Dispose()
        {
            _brush.Properties.ScriptsRefreshed -= OnScriptsRefreshed;

            // Unsubscribe from all scripts
            foreach (var script in Scripts)
            {
                script.PropertyChanged -= Script_PropertyChanged;
            }

            _brush.Properties.SaveScripts();
            _previewService?.Dispose();
            base.Dispose();
        }
    }
}
