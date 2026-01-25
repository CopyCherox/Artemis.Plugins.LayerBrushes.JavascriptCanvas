using Artemis.Core.LayerBrushes;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services;
using Artemis.UI.Shared.LayerBrushes;
using Avalonia.Media;
using ReactiveUI;
using RGB.NET.Core;
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
        private bool _isPreviewPaused = false;
        private double _timeScale = 1.0;
        private double _currentTime = 0;

        public JavascriptCanvasBrushConfigurationViewModel(JavascriptCanvasBrush layerBrush)
            : base(layerBrush)
        {
            _brush = layerBrush;
            _dialogService = new DialogService();

            _brush.Properties.ScriptsRefreshed += OnScriptsRefreshed;

            Scripts = _brush.Properties.Scripts;

            foreach (var script in Scripts)
            {
                _originalScriptNames[script] = script.ScriptName;
                script.PropertyChanged += Script_PropertyChanged;
            }

            _scriptManager = new ScriptManagementService(Scripts);

            _previewService = new PreviewRenderingService();
            _previewService.PreviewUpdated += (s, bitmap) => PreviewBitmap = bitmap;
            _previewService.ErrorOccurred += (s, error) => ErrorMessage = error;

            _previewService.TimeScaleChanged += (s, scale) => { TimeScale = scale; };
            _previewService.PausedChanged += (s, paused) =>
            {
                IsPreviewPaused = paused;
                this.RaisePropertyChanged(nameof(PlayPauseText));
            };
            _previewService.TimeChanged += (s, time) => { CurrentTime = time; };

            if (_brush.Properties.EnableAudio != null)
            {
                bool initialAudioState = _brush.Properties.EnableAudio.CurrentValue;
                _previewService.SetAudioEnabled(initialAudioState);

                _brush.Properties.EnableAudio.CurrentValueSet += (sender, args) =>
                {
                    bool enabled = _brush.Properties.EnableAudio.CurrentValue;
                    _previewService.SetAudioEnabled(enabled);
                };
            }

            SelectedScript = Scripts.FirstOrDefault(s => s.IsEnabled) ?? Scripts.FirstOrDefault();
            _frameSkip = _brush.Properties.UpdateEveryNFrames?.CurrentValue ?? 2;

            AddScriptCommand = ReactiveCommand.Create(AddScript);
            DeleteScriptCommand = ReactiveCommand.Create(DeleteScript,
                this.WhenAnyValue(x => x.SelectedScript).Select(s => s != null));
            ApplyScriptCommand = ReactiveCommand.Create(ApplyScript);
            ExportScriptCommand = ReactiveCommand.Create(ExportScript,
                this.WhenAnyValue(x => x.SelectedScript).Select(s => s != null));
            ImportScriptCommand = ReactiveCommand.Create(ImportScript);

            PlayPauseCommand = ReactiveCommand.Create(TogglePlayPause);
            ResetTimeCommand = ReactiveCommand.Create(ResetTime);
            SetSpeedCommand = ReactiveCommand.Create<double>(SetSpeed);

            this.WhenAnyValue(x => x.SelectedScript).Subscribe(OnScriptSelected);
            this.WhenAnyValue(x => x.EditorCode).Subscribe(code => { _previewService.SetScript(code); });
            this.WhenAnyValue(x => x.CanvasWidth, x => x.CanvasHeight).Subscribe(_ =>
            {
                _previewService.SetCanvasSize(_canvasWidth, _canvasHeight);
            });

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
            set => this.RaiseAndSetIfChanged(ref _canvasWidth, value);
        }

        public int CanvasHeight
        {
            get => _canvasHeight;
            set => this.RaiseAndSetIfChanged(ref _canvasHeight, value);
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
                    {
                        _brush.Properties.UpdateEveryNFrames.SetCurrentValue(value);
                    }
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public bool IsPreviewPaused
        {
            get => _isPreviewPaused;
            private set => this.RaiseAndSetIfChanged(ref _isPreviewPaused, value);
        }

        public double TimeScale
        {
            get => _timeScale;
            set => this.RaiseAndSetIfChanged(ref _timeScale, value);
        }

        public double CurrentTime
        {
            get => _currentTime;
            private set => this.RaiseAndSetIfChanged(ref _currentTime, value);
        }

        public string PlayPauseText => IsPreviewPaused ? "▶️ Resume" : "⏸️ Pause";

        public ReactiveCommand<Unit, Unit> AddScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> ImportScriptCommand { get; }
        public ReactiveCommand<Unit, Unit> PlayPauseCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetTimeCommand { get; }
        public ReactiveCommand<double, Unit> SetSpeedCommand { get; }

        private void OnScriptSelected(JavascriptScriptModel? script)
        {
            if (script != null)
            {
                EditorCode = script.JavaScriptCode;
                _savedEditorCode = script.JavaScriptCode;
                _savedScriptName = script.ScriptName;
                HasUnsavedChanges = false;
            }
        }

        private void Script_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is JavascriptScriptModel script && e.PropertyName == nameof(JavascriptScriptModel.ScriptName))
            {
                if (SelectedScript == script)
                {
                    HasUnsavedChanges = (_currentEditorCode != _savedEditorCode ||
                                        script.ScriptName != _savedScriptName);
                }
            }
        }

        private void TogglePlayPause()
        {
            _previewService.SetPaused(!IsPreviewPaused);
        }

        private void ResetTime()
        {
            _previewService.ResetTime();
        }

        private void SetSpeed(double speed)
        {
            _previewService.SetTimeScale(speed);
        }

        private void AddScript()
        {
            var newScript = _scriptManager.AddNewScript();

            // Verify the script is in the Scripts collection
            if (!Scripts.Contains(newScript))
            {
                Scripts.Add(newScript);
            }

            _originalScriptNames[newScript] = newScript.ScriptName;
            newScript.PropertyChanged += ScriptPropertyChanged;

            // Save immediately
            ScriptsFolderManager.SaveScriptToFile(newScript.ScriptName, newScript.JavaScriptCode);

            // Sort scripts alphabetically
            var sortedScripts = Scripts.OrderBy(s => s.ScriptName).ToList();
            Scripts.Clear();
            foreach (var script in sortedScripts)
            {
                Scripts.Add(script);
            }

            // Force UI refresh
            this.RaisePropertyChanged(nameof(Scripts));

            // Select with dispatcher
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await System.Threading.Tasks.Task.Delay(50);
                SelectedScript = newScript;
            });
        }


        private void ScriptPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is JavascriptScriptModel script && e.PropertyName == nameof(JavascriptScriptModel.ScriptName))
            {
                if (SelectedScript == script)
                {
                    HasUnsavedChanges = _currentEditorCode != _savedEditorCode ||
                                        (SelectedScript != null && SelectedScript.ScriptName != _savedScriptName);
                }
            }
        }



        private async void DeleteScript()
        {
            if (SelectedScript == null) return;

            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            bool confirmed = await _dialogService.ShowDeleteConfirmation(mainWindow, SelectedScript.ScriptName);
            if (!confirmed) return;

            var scriptToDelete = SelectedScript;
            SelectedScript = Scripts.FirstOrDefault(s => s != scriptToDelete);

            scriptToDelete.PropertyChanged -= Script_PropertyChanged;
            _originalScriptNames.Remove(scriptToDelete);
            _scriptManager.DeleteScript(scriptToDelete);

            _brush.Properties.SaveScripts();
        }

        private void ApplyScript()
        {
            if (SelectedScript == null) return;

            string originalName = _originalScriptNames[SelectedScript];

            if (originalName != SelectedScript.ScriptName)
            {
                ScriptsFolderManager.SaveScriptToFile(originalName, SelectedScript.JavaScriptCode);
                ScriptsFolderManager.RenameScriptFile(originalName, SelectedScript.ScriptName);
                _originalScriptNames[SelectedScript] = SelectedScript.ScriptName;
            }

            ScriptsFolderManager.SaveScriptToFile(SelectedScript.ScriptName, SelectedScript.JavaScriptCode);

            foreach (var script in Scripts)
                script.IsEnabled = (script == SelectedScript);

            _brush.Properties.SaveScripts();

            _brush.UpdateScript(SelectedScript);

            ScriptsFolderManager.NotifyScriptsChanged();

            _savedEditorCode = _currentEditorCode;
            _savedScriptName = SelectedScript.ScriptName;
            HasUnsavedChanges = false;
        }

        private async void ExportScript()
        {
            if (SelectedScript == null) return;
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            SelectedScript.JavaScriptCode = _currentEditorCode;
            await _scriptManager.ExportScript(SelectedScript, mainWindow);
        }

        private async void ImportScript()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null) return;

            var (successCount, errorCount) = await _scriptManager.ImportScripts(mainWindow);

            if (successCount > 0)
            {
                foreach (var script in Scripts)
                {
                    if (!_originalScriptNames.ContainsKey(script))
                    {
                        _originalScriptNames[script] = script.ScriptName;
                        script.PropertyChanged += Script_PropertyChanged;
                    }
                }

                _brush.Properties.SaveScripts();

                if (successCount + errorCount > 1)
                {
                    await _dialogService.ShowImportSummary(mainWindow, successCount,
                        successCount + errorCount, errorCount);
                }

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
                script.PropertyChanged -= ScriptPropertyChanged;
            }

            // Load and sort scripts
            var newScripts = _brush.Properties.Scripts.OrderBy(s => s.ScriptName).ToList();

            Scripts.Clear();
            foreach (var script in newScripts)
            {
                Scripts.Add(script);
                _originalScriptNames[script] = script.ScriptName;
                script.PropertyChanged += ScriptPropertyChanged;
            }

            this.RaisePropertyChanged(nameof(Scripts));

            // Restore selection
            if (!string.IsNullOrEmpty(previouslySelectedName))
            {
                SelectedScript = Scripts.FirstOrDefault(s => s.ScriptName == previouslySelectedName);
            }
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