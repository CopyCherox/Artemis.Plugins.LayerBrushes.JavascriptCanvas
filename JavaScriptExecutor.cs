using Jint;
using System;
using SkiaSharp;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class JavaScriptExecutor : IDisposable
    {
        private Engine? _engine;

        public string LastError { get; private set; } = string.Empty;
        public int ErrorLine { get; private set; } = -1;
        public int ErrorColumn { get; private set; } = -1;

        public JavaScriptExecutor()
        {
            InitializeEngine();
        }

        private void InitializeEngine()
        {
            try
            {
                _engine = new Engine(options =>
                {
                    options.TimeoutInterval(TimeSpan.Zero);
                    options.LimitRecursion(0);
                    options.MaxStatements(0);
                });
            }
            catch (Exception ex)
            {
                LastError = $"Engine initialization failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Engine init error: {ex.Message}");
            }
        }

        public SKBitmap ExecuteScriptOnCanvas(
    string userCode,
    int canvasWidth,
    int canvasHeight,
    double time,
    Services.AudioReactivityService? audioService = null,
    Action<double>? setTimeScaleCallback = null,
    Action<bool>? setPausedCallback = null,
    Func<double>? getTimeCallback = null)
        {
            SKBitmap? canvasBitmap = null;
            SKCanvas? canvas = null;
            SKPaint? paint = null;
            LastError = string.Empty;
            ErrorLine = -1;
            ErrorColumn = -1;

            try
            {
                if (string.IsNullOrWhiteSpace(userCode))
                {
                    LastError = "No script code provided";
                    return ErrorBitmapHelper.CreateErrorBitmap(canvasWidth, canvasHeight, "No script");
                }

                if (canvasWidth <= 0 || canvasHeight <= 0)
                {
                    LastError = $"Invalid canvas dimensions: {canvasWidth}x{canvasHeight}";
                    return ErrorBitmapHelper.CreateErrorBitmap(100, 100, "Invalid size");
                }

                canvasWidth = Math.Clamp(canvasWidth, 1, 10000);
                canvasHeight = Math.Clamp(canvasHeight, 1, 10000);

                InitializeEngine();
                if (_engine == null)
                {
                    LastError = "JavaScript engine is null";
                    return ErrorBitmapHelper.CreateErrorBitmap(canvasWidth, canvasHeight, "Engine null");
                }

                canvasBitmap = new SKBitmap(canvasWidth, canvasHeight);
                canvas = new SKCanvas(canvasBitmap);
                paint = new SKPaint { IsAntialias = true };
                canvas.Clear(SKColors.Black);

                // Set basic values
                _engine.SetValue("time", time);
                _engine.SetValue("width", canvasWidth);
                _engine.SetValue("height", canvasHeight);

                // Set canvas context
                var ctx = new CanvasContext(canvas, paint, canvasWidth, canvasHeight);
                _engine.SetValue("ctx", ctx);

                // Set audio context
                if (audioService != null)
                {
                    var audioCtx = new AudioContext(audioService);
                    _engine.SetValue("audio", audioCtx);
                }
                else
                {
                    _engine.Execute(@"
                var audio = {
                    Bass: 0,
                    Midrange: 0,
                    Treble: 0,
                    Volume: 0,
                    IsEnabled: false,
                    GetBand: function() { return 0; },
                    GetRange: function() { return 0; }
                };
            ");
                }

                // NEW: Set time control context
                if (setTimeScaleCallback != null && setPausedCallback != null && getTimeCallback != null)
                {
                    var timeControl = new TimeControl(setTimeScaleCallback, setPausedCallback, getTimeCallback);
                    _engine.SetValue("timeControl", timeControl);
                }
                else
                {
                    // Create dummy time control for non-preview contexts
                    _engine.Execute(@"
                var timeControl = {
                    Speed: 1.0,
                    IsPaused: false,
                    Current: 0,
                    SetSpeed: function(speed) { this.Speed = speed; },
                    Pause: function() { this.IsPaused = true; },
                    Resume: function() { this.IsPaused = false; },
                    Toggle: function() { this.IsPaused = !this.IsPaused; }
                };
            ");
                }

                try
                {
                    _engine.Execute(userCode);
                }
                catch (Jint.Runtime.JavaScriptException jsEx)
                {
                    ErrorLine = jsEx.Location.Start.Line;
                    ErrorColumn = jsEx.Location.Start.Column;
                    LastError = $"JS Error at Line {ErrorLine}, Col {ErrorColumn}: {jsEx.Error}";
                    System.Diagnostics.Debug.WriteLine(LastError);
                    canvas?.Dispose();
                    paint?.Dispose();
                    canvasBitmap?.Dispose();
                    return ErrorBitmapHelper.CreateErrorBitmap(canvasWidth, canvasHeight, $"Line {ErrorLine}: {jsEx.Error}");
                }
                catch (Exception innerEx)
                {
                    LastError = $"Script execution error: {innerEx.Message}";
                    System.Diagnostics.Debug.WriteLine(LastError);
                    canvas?.Dispose();
                    paint?.Dispose();
                    canvasBitmap?.Dispose();
                    return ErrorBitmapHelper.CreateErrorBitmap(canvasWidth, canvasHeight, "Script error");
                }

                canvas.Flush();
                return canvasBitmap;
            }
            catch (Exception ex)
            {
                LastError = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex}");
                canvas?.Dispose();
                paint?.Dispose();
                canvasBitmap?.Dispose();
                return ErrorBitmapHelper.CreateErrorBitmap(canvasWidth, canvasHeight, "Script error");
            }
            finally
            {
                canvas?.Dispose();
                paint?.Dispose();
            }
        }


        public void Dispose()
        {
            try
            {
                _engine = null;
            }
            catch { }
        }
    }
}