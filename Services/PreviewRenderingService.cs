using Avalonia.Threading;
using SkiaSharp;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services
{
    public class PreviewRenderingService : IDisposable
    {
        private readonly JavaScriptExecutor _jsExecutor;
        private readonly AudioReactivityService? audioService;
        private DispatcherTimer? _updateTimer;
        private double _time = 0;
        private double _timeScale = 1.0;
        private bool _isPaused = false;
        private SKBitmap? _previewBitmap;
        private string _currentScript = string.Empty;
        private int _width = 630;
        private int _height = 250;

        public event EventHandler<SKBitmap?>? PreviewUpdated;
        public event EventHandler<string>? ErrorOccurred;

        // NEW: Events for time control changes
        public event EventHandler<double>? TimeScaleChanged;
        public event EventHandler<bool>? PausedChanged;
        public event EventHandler<double>? TimeChanged;

        public double CurrentTime => _time;
        public double TimeScale => _timeScale;
        public bool IsPaused => _isPaused;

        public PreviewRenderingService()
        {
            _jsExecutor = new JavaScriptExecutor();
            audioService = new AudioReactivityService();
            audioService?.Start();
        }

        public void SetTimeScale(double scale)
        {
            var newScale = Math.Clamp(scale, 0.0, 10.0);
            if (Math.Abs(_timeScale - newScale) > 0.001)
            {
                _timeScale = newScale;
                TimeScaleChanged?.Invoke(this, _timeScale);
            }
        }

        public void SetPaused(bool paused)
        {
            if (_isPaused != paused)
            {
                _isPaused = paused;
                PausedChanged?.Invoke(this, _isPaused);
            }
        }

        public void StartPreviewTimer(int intervalMs = 50)
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(intervalMs)
            };
            _updateTimer.Tick += (s, e) =>
            {
                // Update time only if not paused
                if (!_isPaused)
                {
                    _time += (intervalMs / 1000.0) * _timeScale;
                    TimeChanged?.Invoke(this, _time);
                }

                // ALWAYS update preview, even when paused (for audio visualization)
                UpdatePreview();
            };
            _updateTimer.Start();
        }


        public void SetScript(string scriptCode)
        {
            _currentScript = scriptCode;
        }

        public void SetCanvasSize(int width, int height)
        {
            _width = width;
            _height = height;
            _time = 0;
            TimeChanged?.Invoke(this, _time);
        }

        private void UpdatePreview()
        {
            if (string.IsNullOrWhiteSpace(_currentScript))
            {
                ErrorOccurred?.Invoke(this, "⚠ No script selected");
                return;
            }

            try
            {
                var oldBitmap = _previewBitmap;
                _previewBitmap = _jsExecutor.ExecuteScriptOnCanvas(
                    _currentScript,
                    _width,
                    _height,
                    _time,
                    audioService,
                    SetTimeScale,
                    SetPaused,
                    () => _time
                );
                oldBitmap?.Dispose();
                PreviewUpdated?.Invoke(this, _previewBitmap);

                if (!string.IsNullOrEmpty(_jsExecutor.LastError))
                {
                    string errorMsg = _jsExecutor.ErrorLine > 0
                        ? $"❌ Line {_jsExecutor.ErrorLine}, Col {_jsExecutor.ErrorColumn}: {_jsExecutor.LastError}"
                        : $"❌ {_jsExecutor.LastError}";
                    ErrorOccurred?.Invoke(this, errorMsg);
                }
                else
                {
                    ErrorOccurred?.Invoke(this, string.Empty);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = ex.Message.Length > 200
                    ? ex.Message.Substring(0, 200) + "..."
                    : ex.Message;
                ErrorOccurred?.Invoke(this, $"❌ Unexpected error: {errorMsg}");
            }
        }

        public void ResetTime()
        {
            _time = 0;
            TimeChanged?.Invoke(this, _time);
        }

        public void Dispose()
        {
            _updateTimer?.Stop();
            _updateTimer = null;
            _previewBitmap?.Dispose();
            _previewBitmap = null;
            audioService?.Dispose();
            _jsExecutor?.Dispose();
        }
    }
}
