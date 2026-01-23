using SkiaSharp;
using System;
using System.Text.RegularExpressions;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public partial class CanvasContext
    {
        // RELAXED PARSER: Finds any number (e.g. "30", "12.5") to use as size
        private static readonly Regex SizeParser = new Regex(@"(\d+(\.\d+)?)", RegexOptions.Compiled);

        // ==================================================================================
        // FONT METHOD (Renamed to setFont as requested)
        // ==================================================================================
        public void setFont(string fontSpec)
        {
            if (string.IsNullOrWhiteSpace(fontSpec)) return;

            try
            {
                var match = SizeParser.Match(fontSpec);
                if (match.Success && float.TryParse(match.Value, out float size))
                {
                    _currentState.FontSize = size;
                    // Remove the number to isolate the Family
                    string temp = fontSpec.Remove(match.Index, match.Length);

                    // Clean up "px", "pt", quotes
                    string family = temp.Replace("px", "", StringComparison.OrdinalIgnoreCase)
                                        .Replace("pt", "", StringComparison.OrdinalIgnoreCase)
                                        .Replace("\"", "")
                                        .Replace("'", "")
                                        .Trim();

                    if (!string.IsNullOrWhiteSpace(family))
                        _currentState.FontFamily = family;
                }
                else
                {
                    // No number found? Assume it's just a family name
                    _currentState.FontFamily = fontSpec.Replace("\"", "").Replace("'", "").Trim();
                }
            }
            catch
            {
                _currentState.FontFamily = fontSpec;
            }
        }

        // ==================================================================================
        // ALIGNMENT
        // ==================================================================================
        public void textAlign(string align) => _currentState.TextAlign = align?.ToLower() ?? "start";
        public void textBaseline(string baseline) => _currentState.TextBaseline = baseline?.ToLower() ?? "alphabetic";

        // ==================================================================================
        // DRAWING
        // ==================================================================================
        public void fillText(string text, double x, double y)
        {
            if (string.IsNullOrEmpty(text)) return;
            ApplyPaintState(SKPaintStyle.Fill);
            DrawWithShadow(() => DrawTextInternal(text, (float)x, (float)y));
        }

        public void strokeText(string text, double x, double y)
        {
            if (string.IsNullOrEmpty(text)) return;
            ApplyPaintState(SKPaintStyle.Stroke);
            DrawWithShadow(() => DrawTextInternal(text, (float)x, (float)y));
        }

        public object measureText(string text)
        {
            if (string.IsNullOrEmpty(text)) return new { width = 0.0 };

            using var font = CreateFont();

            // SUPPRESS WARNINGS: We must use the obsolete SKPaint methods
            // because SKFont methods cause a MethodNotFound exception at runtime.
#pragma warning disable CS0618
            _paint.Typeface = font.Typeface;
            _paint.TextSize = font.Size;
            return new { width = (double)_paint.MeasureText(text) };
#pragma warning restore CS0618
        }

        // ==================================================================================
        // HELPERS
        // ==================================================================================
        private SKFont CreateFont()
        {
            var typeface = SKTypeface.FromFamilyName(_currentState.FontFamily);
            if (typeface == null || typeface.FamilyName == SKTypeface.Default.FamilyName)
            {
                string[] fallbacks = { "Segoe UI", "Arial", "Calibri", "Consolas" };
                foreach (var family in fallbacks)
                {
                    if (family.Equals(_currentState.FontFamily, StringComparison.OrdinalIgnoreCase)) continue;
                    var fb = SKTypeface.FromFamilyName(family);
                    if (fb != null && fb.FamilyName != SKTypeface.Default.FamilyName)
                    {
                        typeface = fb;
                        break;
                    }
                }
            }
            return new SKFont(typeface ?? SKTypeface.Default, _currentState.FontSize);
        }

        private void DrawTextInternal(string text, float x, float y)
        {
            using var font = CreateFont();

#pragma warning disable CS0618 
            // Sync paint for consistent measuring
            _paint.Typeface = font.Typeface;
            _paint.TextSize = font.Size;
            float width = _paint.MeasureText(text);
#pragma warning restore CS0618

            float adjX = x;
            float adjY = y;
            var metrics = font.Metrics;

            if (_currentState.TextAlign == "center") adjX -= width / 2;
            else if (_currentState.TextAlign == "right" || _currentState.TextAlign == "end") adjX -= width;

            switch (_currentState.TextBaseline)
            {
                case "top": adjY -= metrics.Ascent; break;
                case "middle": adjY += (metrics.CapHeight / 2) - metrics.Descent; break;
            }

            _canvas.DrawText(text, adjX, adjY, font, _paint);
        }
    }
}