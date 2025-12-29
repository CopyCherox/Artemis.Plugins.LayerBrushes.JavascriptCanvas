using SkiaSharp;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public partial class CanvasContext
    {
        // ============= TEXT =============

        public void font(string fontString)
        {
            // Parse font string (e.g., "24px Arial", "bold 16px Helvetica")
            var parts = fontString.Trim().Split(' ');

            foreach (var part in parts)
            {
                if (part.Contains("px"))
                {
                    var sizeStr = part.Replace("px", "");
                    if (float.TryParse(sizeStr, out float size))
                    {
                        _currentState.FontSize = size;
                    }
                }
                else if (!part.Equals("bold", StringComparison.OrdinalIgnoreCase) &&
                         !part.Equals("italic", StringComparison.OrdinalIgnoreCase))
                {
                    _currentState.FontFamily = part;
                }
            }
        }

        public void textAlign(string align)
        {
            _currentState.TextAlign = align; // "start", "end", "left", "right", "center"
        }

        public void textBaseline(string baseline)
        {
            _currentState.TextBaseline = baseline; // "top", "hanging", "middle", "alphabetic", "ideographic", "bottom"
        }

        public void fillText(string text, double x, double y, double maxWidth = 0)
        {
            ApplyPaintState(SKPaintStyle.Fill);
            DrawTextWithAlignment(text, (float)x, (float)y, (float)maxWidth);
        }

        public void strokeText(string text, double x, double y, double maxWidth = 0)
        {
            ApplyPaintState(SKPaintStyle.Stroke);
            DrawTextWithAlignment(text, (float)x, (float)y, (float)maxWidth);
        }

        public object measureText(string text)
        {
            using var font = new SKFont { Size = _currentState.FontSize };
            var bounds = new SKRect();
            font.MeasureText(text, out bounds);
            return new { width = (double)bounds.Width };
        }

        private void DrawTextWithAlignment(string text, float x, float y, float maxWidth)
        {
            using var font = new SKFont { Size = _currentState.FontSize };

            // Measure text
            var bounds = new SKRect();
            font.MeasureText(text, out bounds);

            // Apply horizontal alignment
            float xOffset = _currentState.TextAlign switch
            {
                "center" => -bounds.Width / 2,
                "right" => -bounds.Width,
                "end" => -bounds.Width,
                _ => 0
            };

            // Apply vertical baseline
            float yOffset = _currentState.TextBaseline switch
            {
                "top" => -bounds.Top,
                "middle" => -bounds.MidY,
                "bottom" => -bounds.Bottom,
                "hanging" => -bounds.Top * 0.8f,
                "ideographic" => -bounds.Bottom * 0.9f,
                _ => 0 // alphabetic
            };

            DrawWithShadow(() => _canvas.DrawText(text, x + xOffset, y + yOffset, font, _paint));
        }
    }
}