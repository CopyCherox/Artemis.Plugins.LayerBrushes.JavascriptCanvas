using Artemis.Core;
using Artemis.Core.LayerBrushes;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas;
using Artemis.UI.Shared.LayerBrushes;
using SkiaSharp;
using System;
using System.Linq;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas.ViewModels;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class JavascriptCanvasBrush : LayerBrush<JavascriptCanvasBrushProperties>
    {
        private SKBitmap? _canvasBitmap;
        private double _time = 0;
        private JavaScriptExecutor? _jsExecutor;
        private readonly object _lockObject = new object();
        private string? _lastError;

        // FRAME SKIPPING for performance
        private int _frameCounter = 0;


        // Cache LED layout calculations
        private float _cachedOffsetX = 0;
        private float _cachedOffsetY = 0;
        private float _cachedLayerWidth = 0;
        private float _cachedLayerHeight = 0;
        private int _cachedLedCount = 0;

        private Services.AudioReactivityService? _audioService;

        public override void EnableLayerBrush()
        {
            ConfigurationDialog = new LayerBrushConfigurationDialog<JavascriptCanvasBrushConfigurationViewModel>(1300, 800);
            try
            {
                _jsExecutor = new JavaScriptExecutor();
                _audioService = new Services.AudioReactivityService();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Enable brush error: {ex.Message}");
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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Disable brush error: {ex.Message}");
                }
            }
        }

        public override void Update(double deltaTime)
        {
            _time += deltaTime;
            _frameCounter++;

            if (_frameCounter == 1)
            {
                System.Diagnostics.Debug.WriteLine($"🎮 First Update, audioService is null: {_audioService == null}");
            }

            // Start audio
            _audioService?.Start();

            int updateInterval = Properties?.UpdateEveryNFrames?.CurrentValue ?? 2;
            if (_frameCounter % updateInterval != 0)
            {
                return;
            }

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

                    // Update cached layout only if LED count changed
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
                    _canvasBitmap = _jsExecutor.ExecuteScriptOnCanvas(
                        currentScript.JavaScriptCode,
                        canvasWidth,
                        canvasHeight,
                        _time,
                        _audioService,
                        null,
                        null,
                        null
                    );
                    oldBitmap?.Dispose();
                    _lastError = null;
                }
            }
            catch (Exception ex)
            {
                if (_lastError != ex.Message)
                {
                    _lastError = ex.Message;

                    // Get detailed error from executor
                    if (_jsExecutor != null && !string.IsNullOrEmpty(_jsExecutor.LastError))
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Script Error: {_jsExecutor.LastError}");
                        if (_jsExecutor.ErrorLine > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"   at Line {_jsExecutor.ErrorLine}, Column {_jsExecutor.ErrorColumn}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Update error: {ex.Message}");
                    }
                }
                GenerateFallbackEffect();
            }

        }

        public override void Render(SKCanvas canvas, SKRect bounds, SKPaint paint)
        {
            if (Layer?.Leds == null) return;

            lock (_lockObject)
            {
                if (_canvasBitmap == null || _canvasBitmap.Width == 0 || _canvasBitmap.Height == 0)
                    return;

                try
                {
                    float brightness = Properties?.Brightness?.CurrentValue ?? 100;
                    brightness = brightness / 100f;

                    var leds = Layer.Leds;
                    if (leds.Count == 0) return;
                    if (_cachedLayerWidth <= 0 || _cachedLayerHeight <= 0) return;

                    // KEY FIX: Map LEDs directly to bitmap dimensions
                    // This ensures JavaScript canvas coordinate space matches LED sampling
                    for (int i = 0; i < leds.Count; i++)
                    {
                        var led = leds[i];
                        float ledCenterX = led.AbsoluteRectangle.MidX - _cachedOffsetX;
                        float ledCenterY = led.AbsoluteRectangle.MidY - _cachedOffsetY;

                        // Sample from bitmap using normalized coordinates (0.0 to 1.0)
                        // Then scale to actual bitmap dimensions
                        float normalizedX = ledCenterX / _cachedLayerWidth;
                        float normalizedY = ledCenterY / _cachedLayerHeight;

                        int canvasX = (int)(normalizedX * (_canvasBitmap.Width - 1));
                        int canvasY = (int)(normalizedY * (_canvasBitmap.Height - 1));

                        // Clamp to valid bitmap coordinates
                        canvasX = Math.Clamp(canvasX, 0, _canvasBitmap.Width - 1);
                        canvasY = Math.Clamp(canvasY, 0, _canvasBitmap.Height - 1);

                        var pixelColor = _canvasBitmap.GetPixel(canvasX, canvasY);

                        paint.Color = new SKColor(
                            (byte)Math.Clamp((int)(pixelColor.Red * brightness), 0, 255),
                            (byte)Math.Clamp((int)(pixelColor.Green * brightness), 0, 255),
                            (byte)Math.Clamp((int)(pixelColor.Blue * brightness), 0, 255)
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
                    System.Diagnostics.Debug.WriteLine($"Render error: {ex.Message}");
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
                        @"// Fallback rainbow
ctx.clear(0, 0, 0);
for (let x = 0; x < width; x++) {
    let hue = (x / width + time * 0.5) % 1.0;
    let rgb = ctx.hslToRgb(hue, 1.0, 0.5);
    ctx.fillStyle(rgb.r, rgb.g, rgb.b);
    ctx.fillRect(x, 0, 1, height);
}",
                        800,
                        100,
                        _time,
                        null,   // No audio service in fallback
                        null,   // No time scale callback
                        null,   // No pause callback
                        null    // No get time callback                  
                    );
                    oldBitmap?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback error: {ex.Message}");
                }
            }
        }
    }
}