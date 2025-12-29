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
using System.Collections.ObjectModel;
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
        private int canvasWidth = 800;
        private int canvasHeight = 150;
        private string errorMessage = string.Empty;
        private string currentEditorCode = string.Empty;
        private DispatcherTimer? updateTimer;

        private int frameSkip = 2;



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

            // Use the SAME collection from Properties (don't create a copy)
            Scripts = brush.Properties.Scripts;
            SelectedScript = Scripts.FirstOrDefault(s => s.IsEnabled) ?? Scripts.FirstOrDefault();

            // Initialize editor code
            if (SelectedScript != null)
            {
                currentEditorCode = SelectedScript.JavaScriptCode;
            }

            // Initialize frame skip from brush property
            frameSkip = brush.Properties.UpdateEveryNFrames?.CurrentValue ?? 2;

            // ✅ Subscribe to CurrentValueSet event
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

            // ✅ NEW: Watch for IsGlobal changes on ALL scripts
            foreach (var script in Scripts)
            {
                script.PropertyChanged += Script_PropertyChanged;
            }

            AddScriptCommand = ReactiveCommand.Create(AddScript);
            DeleteScriptCommand = ReactiveCommand.Create(DeleteScript, this.WhenAnyValue(x => x.SelectedScript).Select(s => s != null));
            ApplyScriptCommand = ReactiveCommand.Create(ApplyScript);

            // Watch for script selection changes
            this.WhenAnyValue(x => x.SelectedScript)
                .Subscribe(script =>
                {
                    if (script != null && script.JavaScriptCode != currentEditorCode)
                    {
                        currentEditorCode = script.JavaScriptCode;
                        this.RaisePropertyChanged(nameof(EditorCode));
                        time = 0; // Reset animation
                    }
                });

            // Start animation timer
            StartPreviewTimer();
        }

        // ✅ NEW: Handle property changes on scripts (especially IsGlobal)
        private void Script_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(JavascriptScriptModel.IsGlobal))
            {
                var script = sender as JavascriptScriptModel;
                if (script != null)
                {
                    // Save immediately when IsGlobal changes
                    brush.Properties.SaveScripts();

                    if (script.IsGlobal)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Script '{script.ScriptName}' marked as GLOBAL and saved immediately");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ℹ️ Script '{script.ScriptName}' unmarked as global and saved immediately");
                    }
                }
            }
            else if (e.PropertyName == nameof(JavascriptScriptModel.ScriptName))
            {
                // Also save when script name changes (for global scripts)
                var script = sender as JavascriptScriptModel;
                if (script != null && script.IsGlobal)
                {
                    brush.Properties.SaveScripts();
                    System.Diagnostics.Debug.WriteLine($"✅ Global script renamed to '{script.ScriptName}' and saved");
                }
            }
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

        public ObservableCollection<JavascriptScriptModel> Scripts { get; }

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
                }
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
            var newScript = new JavascriptScriptModel
            {
                ScriptName = $"Custom Script {Scripts.Count + 1}",
                JavaScriptCode = "// New script\nctx.clear(0, 255, 0);",
                IsEnabled = false,
                IsGlobal = false
            };

            // ✅ Subscribe to property changes for the new script
            newScript.PropertyChanged += Script_PropertyChanged;

            Scripts.Add(newScript);
            SelectedScript = newScript;

            // Save to LayerProperty
            brush.Properties.SaveScripts();
        }


        private async void DeleteScript()
        {
            if (SelectedScript == null) return;

            // Show confirmation dialog
            var dialog = new Avalonia.Controls.Window
            {
                Title = "Confirm Delete",
                Width = 400,
                Height = 180,
                WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.Parse("#2D2D2D"))
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 20
            };

            panel.Children.Add(new TextBlock
            {
                Text = $"Are you sure you want to delete '{SelectedScript.ScriptName}'?",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 10
            };

            var yesButton = new Button
            {
                Content = "Yes, Delete",
                Width = 120,
                Padding = new Thickness(10, 5),
                Background = new SolidColorBrush(Color.Parse("#E74C3C")),
                Foreground = Brushes.White
            };

            var noButton = new Button
            {
                Content = "Cancel",
                Width = 120,
                Padding = new Thickness(10, 5),
                Background = new SolidColorBrush(Color.Parse("#95A5A6")),
                Foreground = Brushes.White
            };

            yesButton.Click += (s, e) =>
            {
                dialog.Close(true);
            };

            noButton.Click += (s, e) =>
            {
                dialog.Close(false);
            };

            buttonPanel.Children.Add(noButton);
            buttonPanel.Children.Add(yesButton);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;

            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
    ? desktop.MainWindow
    : null;

            if (mainWindow == null) return;  // ✅ Early return if no window

            // Show dialog and wait for result
            var result = await dialog.ShowDialog<bool?>(mainWindow);


            // Only delete if user confirmed
            if (result == true)
            {
                var index = Scripts.IndexOf(SelectedScript);
                Scripts.Remove(SelectedScript);
                if (Scripts.Count > 0)
                    SelectedScript = Scripts[Math.Max(0, index - 1)];

                // Save to LayerProperty
                brush.Properties.SaveScripts();
            }
        }


        private void ApplyScript()
        {
            if (SelectedScript != null)
            {
                // Save current editor code to the script
                SelectedScript.JavaScriptCode = currentEditorCode;

                // Disable ALL scripts first
                foreach (var script in Scripts)
                {
                    script.IsEnabled = false;
                }

                // Enable ONLY the selected script
                SelectedScript.IsEnabled = true;

                // Apply to the actual layer brush
                brush.UpdateScript(SelectedScript);

                // Reset animation
                time = 0;

                // Force immediate preview update
                UpdatePreview();

                // Save to LayerProperty AND global scripts
                brush.Properties.SaveScripts();

                // ✅ Force properties to refresh from global storage
                brush.Properties.RefreshScripts();

                // ✅ Show confirmation if marked as global
                if (SelectedScript.IsGlobal)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Script '{SelectedScript.ScriptName}' saved as GLOBAL - will appear in new brushes");
                }
            }
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


        public new void Dispose()
        {
            updateTimer?.Stop();
            updateTimer = null;
            previewBitmap?.Dispose();
            previewBitmap = null;
            jsExecutor?.Dispose();

            // ✅ Unsubscribe from all script property changes
            foreach (var script in Scripts)
            {
                script.PropertyChanged -= Script_PropertyChanged;
            }
        }


        ~JavascriptCanvasBrushConfigurationViewModel()
        {
            Dispose();
        }
    }
}