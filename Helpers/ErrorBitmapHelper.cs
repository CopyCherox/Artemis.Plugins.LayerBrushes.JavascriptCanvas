using SkiaSharp;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public static class ErrorBitmapHelper
    {
        public static SKBitmap CreateErrorBitmap(int width, int height, string reason)
        {
            try
            {
                width = Math.Clamp(width, 10, 10000);
                height = Math.Clamp(height, 10, 10000);

                var bitmap = new SKBitmap(width, height);
                using var canvas = new SKCanvas(bitmap);

                canvas.Clear(new SKColor(50, 0, 0));

                using var paint = new SKPaint
                {
                    Color = new SKColor(255, 100, 100),
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 3
                };

                float margin = Math.Min(width, height) * 0.2f;
                canvas.DrawLine(margin, margin, width - margin, height - margin, paint);
                canvas.DrawLine(width - margin, margin, margin, height - margin, paint);

                System.Diagnostics.Debug.WriteLine($"Error bitmap created: {reason}");

                return bitmap;
            }
            catch
            {
                return new SKBitmap(100, 100);
            }
        }
    }
}