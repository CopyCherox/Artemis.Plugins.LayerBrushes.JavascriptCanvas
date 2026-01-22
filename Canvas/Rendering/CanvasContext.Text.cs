using SkiaSharp;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public partial class CanvasContext
    {
        public void Font(string fontSpec)
        {
            try
            {
                var parts = fontSpec.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string sizeStr = parts[0].Replace("px", "").Replace("pt", "");
                    if (float.TryParse(sizeStr, out float size))
                    {
                        _currentState.FontSize = size;
                    }
                    _currentState.FontFamily = string.Join(" ", parts, 1, parts.Length - 1);
                }
            }
            catch { }
        }

        public void TextAlign(string align)
        {
            _currentState.TextAlign = align.ToLower();
        }

        public void TextBaseline(string baseline)
        {
            _currentState.TextBaseline = baseline.ToLower();
        }

        public void FillText(string text, float x, float y)
        {
            if (string.IsNullOrEmpty(text)) return;
            DrawText(text, x, y, false);
        }

        public void StrokeText(string text, float x, float y)
        {
            if (string.IsNullOrEmpty(text)) return;
            DrawText(text, x, y, true);
        }

        public object MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new { width = 0.0 };

            try
            {
                using var font = new SKFont(
                    SKTypeface.FromFamilyName(_currentState.FontFamily),
                    _currentState.FontSize
                );

                float width = font.MeasureText(text);
                return new { width = (double)width };
            }
            catch
            {
                return new { width = text.Length * _currentState.FontSize * 0.6 };
            }
        }

        private void DrawText(string text, float x, float y, bool stroke = false)
        {
            try
            {
                // Create SKFont instead of using obsolete SKPaint properties
                using var font = new SKFont(
                    SKTypeface.FromFamilyName(_currentState.FontFamily),
                    _currentState.FontSize
                );

                // Measure text using SKFont
                float textWidth = font.MeasureText(text);

                // Adjust X for alignment
                float adjustedX = x;
                switch (_currentState.TextAlign)
                {
                    case "center":
                        adjustedX = x - (textWidth / 2);
                        break;
                    case "right":
                    case "end":
                        adjustedX = x - textWidth;
                        break;
                }

                // Adjust Y for baseline (SKCanvas baseline is at bottom)
                float adjustedY = y;
                switch (_currentState.TextBaseline)
                {
                    case "top":
                        adjustedY = y + (_currentState.FontSize * 0.85f);
                        break;
                    case "middle":
                        adjustedY = y + (_currentState.FontSize * 0.3f);
                        break;
                    case "bottom":
                        adjustedY = y;
                        break;
                    case "alphabetic":
                    default:
                        adjustedY = y;
                        break;
                }

                // Set style
                if (stroke)
                {
                    ApplyPaintState(SKPaintStyle.Stroke);
                }
                else
                {
                    ApplyPaintState(SKPaintStyle.Fill);
                }

                // Draw using the new non-obsolete method
                DrawWithShadow(() => _canvas.DrawText(text, adjustedX, adjustedY, font, _paint));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Text error: {ex.Message}");
            }
        }
    }
}
