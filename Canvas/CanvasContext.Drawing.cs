using Avalonia.Controls;
using SkiaSharp;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public partial class CanvasContext
    {
        // ============= RECTANGLES =============
        public void fillRect(double x, double y, double w, double h)
        {
            ApplyPaintState(SKPaintStyle.Fill);
            DrawWithShadow(() => _canvas.DrawRect((float)x, (float)y, (float)w, (float)h, _paint));
        }

        public void strokeRect(double x, double y, double w, double h)
        {
            ApplyPaintState(SKPaintStyle.Stroke);
            DrawWithShadow(() => _canvas.DrawRect((float)x, (float)y, (float)w, (float)h, _paint));
        }

        public void clearRect(double x, double y, double w, double h)
        {
            var originalBlendMode = _paint.BlendMode;
            _paint.BlendMode = SKBlendMode.Clear;
            _canvas.DrawRect((float)x, (float)y, (float)w, (float)h, _paint);
            _paint.BlendMode = originalBlendMode;
        }

        // ============= LEGACY/HELPER METHODS =============
        public void fillCircle(double x, double y, double radius)
        {
            ApplyPaintState(SKPaintStyle.Fill);
            DrawWithShadow(() => _canvas.DrawCircle((float)x, (float)y, (float)Math.Max(0, radius), _paint));
        }

        public void strokeCircle(double x, double y, double radius)
        {
            ApplyPaintState(SKPaintStyle.Stroke);
            DrawWithShadow(() => _canvas.DrawCircle((float)x, (float)y, (float)Math.Max(0, radius), _paint));
        }

        public void drawLine(double x1, double y1, double x2, double y2)
        {
            ApplyPaintState(SKPaintStyle.Stroke);
            DrawWithShadow(() => _canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, _paint));
        }

        public void clear(double r = 0, double g = 0, double b = 0)
        {
            _canvas.Clear(new SKColor(
                (byte)Math.Clamp((int)r, 0, 255),
                (byte)Math.Clamp((int)g, 0, 255),
                (byte)Math.Clamp((int)b, 0, 255)
            ));
        }
    }
}
