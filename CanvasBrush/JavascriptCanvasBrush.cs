using Artemis.Core;
using Artemis.Core.LayerBrushes;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas.ViewModels;
using Artemis.UI.Shared.LayerBrushes;
using SkiaSharp;
using System;
using System.Linq;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class JavascriptCanvasBrush : LayerBrush<JavascriptCanvasBrushProperties>
    {
        private SKBitmap? _canvasBitmap;
        private double _time = 0;
        private JavaScriptExecutor? _jsExecutor;
        private readonly object _lockObject = new object();
        private string? _lastError;
        private int _frameCounter = 0;
        private float _cachedOffsetX = 0;
        private float _cachedOffsetY = 0;
        private float _cachedLayerWidth = 0;
        private float _cachedLayerHeight = 0;
        private int _cachedLedCount = 0;
        private Services.AudioReactivityService? _audioService;
        private bool _lastAudioEnabled = false;

        // PERFORMANCE FIX: Cache pixel data instead of GetPixel() calls
        private byte[]? _pixelCache;
        private int _cacheWidth;
        private int _cacheHeight;

        public override void EnableLayerBrush()
        {
            ConfigurationDialog = new LayerBrushConfigurationDialog<JavascriptCanvasBrushConfigurationViewModel>(1300, 800);
            try
            {
                _jsExecutor = new JavaScriptExecutor();
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }
        }

        public override void DisableLayerBrush()
        {
            lock (_lockObject)
            {
                try
                {
                    _canvasBitmap?.Dispose();
                    _canvasBitmap = null;
                    _jsExecutor?.Dispose();
                    _jsExecutor = null;
                    _audioService?.Dispose();
                    _audioService = null;
                    _pixelCache = null;
                }
                catch (Exception ex)
                {
                    _lastError = ex.Message;
                }
            }
        }

        public override void Update(double deltaTime)
        {
            _time += deltaTime;
            bool audioEnabled = Properties?.EnableAudio?.CurrentValue ?? false;

            // THREADING FIX: Lock audio service access
            lock (_lockObject)
            {
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
                var currentScript = Properties?.Scripts?.FirstOrDefault(s => s.IsEnabled);
                if (currentScript == null || Layer?.Leds == null || _jsExecutor == null)
                {
                    GenerateFallbackEffect();
                    return;
                }

                if (string.IsNullOrWhiteSpace(currentScript.JavaScriptCode))
                {
                    GenerateFallbackEffect();
                    return;
                }

                lock (_lockObject)
                {
                    var leds = Layer.Leds;
                    if (leds.Count == 0) return;

                    if (_cachedLedCount != leds.Count)
                    {
                        float minX = leds.Min(led => led.AbsoluteRectangle.Left);
                        float maxX = leds.Max(led => led.AbsoluteRectangle.Right);
                        float minY = leds.Min(led => led.AbsoluteRectangle.Top);
                        float maxY = leds.Max(led => led.AbsoluteRectangle.Bottom);
                        _cachedOffsetX = minX;
                        _cachedOffsetY = minY;
                        _cachedLayerWidth = maxX - minX;
                        _cachedLayerHeight = maxY - minY;
                        _cachedLedCount = leds.Count;
                    }

                    int canvasWidth = Math.Max(100, Math.Min(2000, (int)_cachedLayerWidth));
                    int canvasHeight = Math.Max(100, Math.Min(500, (int)_cachedLayerHeight));

                    var oldBitmap = _canvasBitmap;
                    Services.AudioReactivityService? audioToUse = (audioEnabled && _audioService != null) ? _audioService : null;

                    _canvasBitmap = _jsExecutor.ExecuteScriptOnCanvas(
                        currentScript.JavaScriptCode,
                        canvasWidth,
                        canvasHeight,
                        _time,
                        audioToUse,
                        null,
                        null,
                        null
                    );

                    oldBitmap?.Dispose();

                    // PERFORMANCE FIX: Extract pixel data once
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

        // PERFORMANCE FIX: Extract pixel data to byte array for fast access
        private void UpdatePixelCache()
        {
            if (_canvasBitmap == null) return;

            _cacheWidth = _canvasBitmap.Width;
            _cacheHeight = _canvasBitmap.Height;
            int pixelCount = _cacheWidth * _cacheHeight;

            if (_pixelCache == null || _pixelCache.Length != pixelCount * 4)
            {
                _pixelCache = new byte[pixelCount * 4];
            }

            // Extract all pixels at once - much faster than GetPixel()
            IntPtr pixelsPtr = _canvasBitmap.GetPixels();
            if (pixelsPtr != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.Copy(pixelsPtr, _pixelCache, 0, _pixelCache.Length);
            }
        }

        public override void Render(SKCanvas canvas, SKRect bounds, SKPaint paint)
        {
            if (Layer?.Leds == null) return;

            lock (_lockObject)
            {
                if (_canvasBitmap == null || _pixelCache == null || _cacheWidth == 0 || _cacheHeight == 0)
                    return;

                try
                {
                    float brightness = Properties?.Brightness?.CurrentValue ?? 100;
                    brightness = brightness / 100f;
                    var leds = Layer.Leds;
                    if (leds.Count == 0) return;
                    if (_cachedLayerWidth <= 0 || _cachedLayerHeight <= 0) return;

                    for (int i = 0; i < leds.Count; i++)
                    {
                        var led = leds[i];
                        float ledCenterX = led.AbsoluteRectangle.MidX - _cachedOffsetX;
                        float ledCenterY = led.AbsoluteRectangle.MidY - _cachedOffsetY;
                        float normalizedX = ledCenterX / _cachedLayerWidth;
                        float normalizedY = ledCenterY / _cachedLayerHeight;

                        int canvasX = (int)(normalizedX * (_cacheWidth - 1));
                        int canvasY = (int)(normalizedY * (_cacheHeight - 1));
                        canvasX = Math.Clamp(canvasX, 0, _cacheWidth - 1);
                        canvasY = Math.Clamp(canvasY, 0, _cacheHeight - 1);

                        // PERFORMANCE FIX: Direct byte array access instead of GetPixel()
                        // SkiaSharp uses BGRA byte order
                        int pixelIndex = (canvasY * _cacheWidth + canvasX) * 4;
                        byte b = _pixelCache[pixelIndex];
                        byte g = _pixelCache[pixelIndex + 1];
                        byte r = _pixelCache[pixelIndex + 2];
                        // byte a = _pixelCache[pixelIndex + 3]; // Alpha not used

                        paint.Color = new SKColor(
                            (byte)Math.Clamp((int)(r * brightness), 0, 255),
                            (byte)Math.Clamp((int)(g * brightness), 0, 255),
                            (byte)Math.Clamp((int)(b * brightness), 0, 255)
                        );

                        var relativeRect = new SKRect(
                            led.AbsoluteRectangle.Left - _cachedOffsetX,
                            led.AbsoluteRectangle.Top - _cachedOffsetY,
                            led.AbsoluteRectangle.Right - _cachedOffsetX,
                            led.AbsoluteRectangle.Bottom - _cachedOffsetY
                        );
                        canvas.DrawRect(relativeRect, paint);
                    }
                }
                catch (Exception ex)
                {
                    _lastError = ex.Message;
                }
            }
        }

        public void UpdateScript(JavascriptScriptModel script)
        {
            try
            {
                _time = 0;
            }
            catch { }
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
