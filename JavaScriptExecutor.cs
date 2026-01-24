using Jint;
using SkiaSharp;
using System;
using System.Diagnostics;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class JavaScriptExecutor : IDisposable
    {
        private Engine? _engine;
        private bool _engineInitialized = false;
        public string LastError { get; private set; } = string.Empty;
        public int ErrorLine { get; private set; } = -1;
        public int ErrorColumn { get; private set; } = -1;

        public JavaScriptExecutor() => InitializeEngine();

        private void InitializeEngine()
        {
            try
            {
                _engine = new Engine(opts =>
                {
                    opts.TimeoutInterval(TimeSpan.FromSeconds(2)); // Prevent infinite loops
                    opts.LimitRecursion(100);
                });

                // Initialize State
                _engine.Execute("var state = {};");

                // ADDED: Console support for debugging
                _engine.SetValue("console", new
                {
                    log = new Action<object>(o => Debug.WriteLine($"[JS Log] {o}")),
                    error = new Action<object>(o => Debug.WriteLine($"[JS Error] {o}")),
                    warn = new Action<object>(o => Debug.WriteLine($"[JS Warn] {o}"))
                });

                _engineInitialized = true;
            }
            catch (Exception ex)
            {
                LastError = $"Engine init failed: {ex.Message}";
                _engineInitialized = false;
            }
        }

        public SKBitmap ExecuteScriptOnCanvas(string userCode, int width, int height, double time,
            Services.AudioReactivityService? audioService = null,
            Action<double>? setTimeScale = null,
            Action<bool>? setPaused = null,
            Func<double>? getTime = null)
        {
            if (!_engineInitialized || _engine == null) InitializeEngine();

            if (_engine == null)
            {
                return ErrorBitmapHelper.CreateErrorBitmap(width, height, "JS Engine Failed");
            }

            // Setup Canvas
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            using var paint = new SKPaint { IsAntialias = true };

            // IMPORTANT: Context is new every frame!
            var ctx = new CanvasContext(canvas, paint, width, height);

            try
            {
                LastError = string.Empty;

                // Inject Variables
                _engine.SetValue("time", time);
                _engine.SetValue("width", width);
                _engine.SetValue("height", height);
                _engine.SetValue("ctx", ctx);

                // Simple Audio Mock/Proxy
                if (audioService != null) _engine.SetValue("audio", new AudioContext(audioService));
                else _engine.Execute("var audio = { Bass:0, Midrange:0, Treble:0, Volume:0 };");

                if (setTimeScale != null && setPaused != null && getTime != null)
                {
                    var timeControl = new TimeControl(setTimeScale, setPaused, getTime);
                    _engine.SetValue("timeControl", timeControl);
                }

                // Execute User Code (wrapped to protect scope)
                _engine.Execute($"(function(){{ {userCode} \n}})();");

                canvas.Flush();
                return bitmap.Copy(); // Return a copy so we can dispose the original safely
            }
            catch (Jint.Runtime.JavaScriptException jsEx)
            {
                ErrorLine = jsEx.Location.Start.Line;
                ErrorColumn = jsEx.Location.Start.Column;
                LastError = jsEx.Message;
                Debug.WriteLine($"JS Error at Line {ErrorLine}, Col {ErrorColumn}: {LastError}");
                return ErrorBitmapHelper.CreateErrorBitmap(width, height, jsEx.Message);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return ErrorBitmapHelper.CreateErrorBitmap(width, height, "Script Error");
            }
        }

        public void Dispose() => _engine = null;
    }
}