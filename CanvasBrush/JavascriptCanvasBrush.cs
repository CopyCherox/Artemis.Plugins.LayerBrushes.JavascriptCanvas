using Artemis.Core;
using Artemis.Core.LayerBrushes;
using Artemis.Core.Modules;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas.ViewModels;
using Artemis.UI.Shared.LayerBrushes;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    [PluginFeature(Name = "Javascript Canvas")]
    public class JavascriptCanvasBrush : LayerBrush<JavascriptCanvasBrushProperties>
    {
        private SKBitmap? _canvasBitmap;
        private double _time = 0;
        private JavaScriptExecutor? _jsExecutor;
        private readonly object _lockObject = new object();
        private string? _lastError;
        private int _frameCounter = 0;

        // Cached layout values
        private float _cachedOffsetX = 0;
        private float _cachedOffsetY = 0;
        private float _cachedLayerWidth = 0;
        private float _cachedLayerHeight = 0;
        private int _cachedLedCount = 0;

        // Performance: Cache reciprocals to avoid division
        private float _invLayerWidth = 1f;
        private float _invLayerHeight = 1f;

        private Services.AudioReactivityService? _audioService;
        private bool _lastAudioEnabled = false;

        // Pixel cache
        private byte[]? _pixelCache;
        private int _cacheWidth;
        private int _cacheHeight;

        // Timer for continuous updates
        private DispatcherTimer? _updateTimer;
        private DateTime _lastUpdateTime;
        private const double TARGET_FPS = 60.0;
        private const double FRAME_TIME = 1.0 / TARGET_FPS;

        // Performance: Cache current script to avoid LINQ each frame
        private JavascriptScriptModel? _currentScript;
        private bool _scriptDirty = true;

        public override void EnableLayerBrush()
        {
            ConfigurationDialog = new LayerBrushConfigurationDialog<JavascriptCanvasBrushConfigurationViewModel>(1300, 800);

            try
            {
                _jsExecutor = new JavaScriptExecutor();
                _lastUpdateTime = DateTime.UtcNow;

                _updateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(1000.0 / TARGET_FPS) // ~16.67ms for 60 FPS
                };

                _updateTimer.Tick += (s, e) =>
                {
                    var now = DateTime.UtcNow;
                    double deltaTime = (now - _lastUpdateTime).TotalSeconds;
                    _lastUpdateTime = now;
                    PerformUpdate(deltaTime);
                };

                _updateTimer.Start();
                System.Diagnostics.Debug.WriteLine("[JavascriptCanvasBrush] Enabled - Timer started for continuous updates");
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[JavascriptCanvasBrush] Enable failed: {ex.Message}");
            }
        }

        public override void DisableLayerBrush()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer = null;
                System.Diagnostics.Debug.WriteLine("[JavascriptCanvasBrush] Timer stopped");
            }

            // Dispose audio service outside lock
            if (_audioService != null)
            {
                _audioService.Dispose();
                _audioService = null;
            }

            lock (_lockObject)
            {
                try
                {
                    _canvasBitmap?.Dispose();
                    _canvasBitmap = null;
                    _jsExecutor?.Dispose();
                    _jsExecutor = null;
                    _pixelCache = null;
                    _currentScript = null;
                }
                catch (Exception ex)
                {
                    _lastError = ex.Message;
                }
            }
        }

        public override void Update(double deltaTime)
        {
            PerformUpdate(deltaTime);
        }

        private void PerformUpdate(double deltaTime)
        {
            _time += deltaTime;

            // Performance: Handle audio service outside lock
            bool audioEnabled = Properties?.EnableAudio?.CurrentValue ?? false;

            if (audioEnabled != _lastAudioEnabled)
            {
                if (audioEnabled)
                {
                    if (_audioService == null)
                    {
                        _audioService = new Services.AudioReactivityService();
                        _audioService.Start();
                    }
                }
                else
                {
                    if (_audioService != null)
                    {
                        _audioService.Dispose();
                        _audioService = null;
                    }
                }
                _lastAudioEnabled = audioEnabled;
            }

            if (audioEnabled && _audioService != null && !_audioService.IsEnabled)
            {
                _audioService.Start();
            }

            int updateInterval = Properties?.UpdateEveryNFrames?.CurrentValue ?? 2;
            if (_frameCounter % updateInterval != 0)
            {
                _frameCounter++;
                return;
            }
            _frameCounter++;

            try
            {
                // Performance: Cache script to avoid LINQ each frame
                if (_scriptDirty || _currentScript == null)
                {
                    _currentScript = Properties?.Scripts?.FirstOrDefault(s => s.IsEnabled);
                    _scriptDirty = false;
                }

                if (_currentScript == null || Layer?.Leds == null || _jsExecutor == null)
                {
                    GenerateFallbackEffect();
                    return;
                }

                if (string.IsNullOrWhiteSpace(_currentScript.JavaScriptCode))
                {
                    GenerateFallbackEffect();
                    return;
                }

                // Single lock for all canvas operations
                lock (_lockObject)
                {
                    var leds = Layer.Leds;
                    if (leds.Count == 0) return;

                    // Performance: Single-pass LED bounds calculation
                    if (_cachedLedCount != leds.Count)
                    {
                        float minX = float.MaxValue, maxX = float.MinValue;
                        float minY = float.MaxValue, maxY = float.MinValue;

                        for (int i = 0; i < leds.Count; i++)
                        {
                            var rect = leds[i].AbsoluteRectangle;
                            if (rect.Left < minX) minX = rect.Left;
                            if (rect.Right > maxX) maxX = rect.Right;
                            if (rect.Top < minY) minY = rect.Top;
                            if (rect.Bottom > maxY) maxY = rect.Bottom;
                        }

                        _cachedOffsetX = minX;
                        _cachedOffsetY = minY;
                        _cachedLayerWidth = maxX - minX;
                        _cachedLayerHeight = maxY - minY;
                        _cachedLedCount = leds.Count;

                        // Performance: Cache reciprocals
                        _invLayerWidth = _cachedLayerWidth > 0 ? 1f / _cachedLayerWidth : 1f;
                        _invLayerHeight = _cachedLayerHeight > 0 ? 1f / _cachedLayerHeight : 1f;
                    }

                    int canvasWidth = Math.Max(100, Math.Min(2000, (int)_cachedLayerWidth));
                    int canvasHeight = Math.Max(100, Math.Min(500, (int)_cachedLayerHeight));

                    var oldBitmap = _canvasBitmap;
                    Services.AudioReactivityService? audioToUse = (audioEnabled && _audioService != null) ? _audioService : null;

                    _canvasBitmap = _jsExecutor.ExecuteScriptOnCanvas(
                        _currentScript.JavaScriptCode,
                        canvasWidth,
                        canvasHeight,
                        _time,
                        audioToUse,
                        null,
                        null,
                        null
                    );

                    oldBitmap?.Dispose();
                    UpdatePixelCache();
                    _lastError = null;
                }
            }
            catch (Exception ex)
            {
                if (_lastError != ex.Message)
                {
                    _lastError = ex.Message;
                }
                GenerateFallbackEffect();
            }
        }

        // Performance: Copy pixel data (safe version - no unsafe code required)
        private void UpdatePixelCache()
        {
            if (_canvasBitmap == null) return;

            _cacheWidth = _canvasBitmap.Width;
            _cacheHeight = _canvasBitmap.Height;
            int pixelCount = _cacheWidth * _cacheHeight * 4;

            if (_pixelCache == null || _pixelCache.Length != pixelCount)
            {
                _pixelCache = new byte[pixelCount];
            }

            IntPtr pixelsPtr = _canvasBitmap.GetPixels();
            if (pixelsPtr != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.Copy(pixelsPtr, _pixelCache, 0, pixelCount);
            }
        }

        public override void Render(SKCanvas canvas, SKRect bounds, SKPaint paint)
        {
            if (Layer?.Leds == null) return;

            // Performance: Minimize lock duration
            byte[]? localCache;
            int cacheW, cacheH;
            float brightness;

            lock (_lockObject)
            {
                if (_canvasBitmap == null || _pixelCache == null || _cacheWidth == 0 || _cacheHeight == 0)
                    return;

                localCache = _pixelCache;
                cacheW = _cacheWidth;
                cacheH = _cacheHeight;
                brightness = (Properties?.Brightness?.CurrentValue ?? 100) / 100f;
            }

            var leds = Layer.Leds;
            if (leds.Count == 0) return;
            if (_cachedLayerWidth <= 0 || _cachedLayerHeight <= 0) return;

            try
            {
                // Performance: Pre-calculate commonly used values
                int cacheWMinus1 = cacheW - 1;
                int cacheHMinus1 = cacheH - 1;
                float invLayerW = _invLayerWidth;
                float invLayerH = _invLayerHeight;
                float offsetX = _cachedOffsetX;
                float offsetY = _cachedOffsetY;

                for (int i = 0; i < leds.Count; i++)
                {
                    var led = leds[i];
                    var rect = led.AbsoluteRectangle;

                    // Performance: Multiplication instead of division
                    float ledCenterX = rect.MidX - offsetX;
                    float ledCenterY = rect.MidY - offsetY;
                    float normalizedX = ledCenterX * invLayerW;
                    float normalizedY = ledCenterY * invLayerH;

                    int canvasX = Math.Clamp((int)(normalizedX * cacheWMinus1), 0, cacheWMinus1);
                    int canvasY = Math.Clamp((int)(normalizedY * cacheHMinus1), 0, cacheHMinus1);

                    // Performance: Bit shift instead of multiply by 4
                    int pixelIndex = (canvasY * cacheW + canvasX) << 2;

                    if (pixelIndex >= 0 && pixelIndex + 3 < localCache.Length)
                    {
                        byte r = (byte)(localCache[pixelIndex + 2] * brightness);
                        byte g = (byte)(localCache[pixelIndex + 1] * brightness);
                        byte b = (byte)(localCache[pixelIndex] * brightness);

                        paint.Color = new SKColor(r, g, b);
                    }
                    else
                    {
                        paint.Color = SKColors.Black;
                    }

                    var relativeRect = new SKRect(
                        rect.Left - offsetX,
                        rect.Top - offsetY,
                        rect.Right - offsetX,
                        rect.Bottom - offsetY
                    );

                    canvas.DrawRect(relativeRect, paint);
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }
        }

        public void UpdateScript(JavascriptScriptModel script)
        {
            _time = 0;
            _scriptDirty = true;
            _currentScript = null;
        }

        private void GenerateFallbackEffect()
        {
            if (_jsExecutor == null) return;

            lock (_lockObject)
            {
                try
                {
                    var oldBitmap = _canvasBitmap;
                    _canvasBitmap = _jsExecutor.ExecuteScriptOnCanvas(
                        @"ctx.clear(0, 0, 0);
for (let x = 0; x < width; x++) {
    let hue = (x / width + time * 0.5) % 1.0;
    let rgb = ctx.hslToRgb(hue, 1.0, 0.5);
    ctx.fillStyle(rgb.r, rgb.g, rgb.b);
    ctx.fillRect(x, 0, 1, height);
}",
                        800,
                        100,
                        _time,
                        null,
                        null,
                        null,
                        null
                    );

                    oldBitmap?.Dispose();
                    UpdatePixelCache();
                }
                catch (Exception ex)
                {
                    _lastError = ex.Message;
                }
            }
        }
    }
}