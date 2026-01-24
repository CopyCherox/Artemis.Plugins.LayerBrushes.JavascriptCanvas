using SkiaSharp;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public partial class CanvasContext
    {
        // ============= STYLE PROPERTIES =============

        public void fillStyle(double r, double g, double b, double a = 255)
        {
            _currentState.FillColor = new SKColor(
                (byte)Math.Clamp((int)r, 0, 255),
                (byte)Math.Clamp((int)g, 0, 255),
                (byte)Math.Clamp((int)b, 0, 255),
                (byte)Math.Clamp((int)a, 0, 255)
            );
            _currentState.FillShader = null;
        }

        public void strokeStyle(double r, double g, double b, double a = 255)
        {
            _currentState.StrokeColor = new SKColor(
                (byte)Math.Clamp((int)r, 0, 255),
                (byte)Math.Clamp((int)g, 0, 255),
                (byte)Math.Clamp((int)b, 0, 255),
                (byte)Math.Clamp((int)a, 0, 255)
            );
            _currentState.StrokeShader = null;
        }

        public void lineWidth(double width)
        {
            _currentState.LineWidth = (float)Math.Max(0.01, width);
        }

        public void lineCap(string cap)
        {
            _currentState.LineCap = cap switch
            {
                "round" => SKStrokeCap.Round,
                "square" => SKStrokeCap.Square,
                _ => SKStrokeCap.Butt
            };
        }

        public void lineJoin(string join)
        {
            _currentState.LineJoin = join switch
            {
                "round" => SKStrokeJoin.Round,
                "bevel" => SKStrokeJoin.Bevel,
                _ => SKStrokeJoin.Miter
            };
        }

        public void miterLimit(double limit)
        {
            _currentState.MiterLimit = (float)Math.Max(1, limit);
        }

        public void globalAlpha(double alpha)
        {
            _currentState.GlobalAlpha = (float)Math.Clamp(alpha, 0, 1);
        }

        public void globalCompositeOperation(string operation)
        {
            _currentState.BlendMode = operation switch
            {
                "source-over" => SKBlendMode.SrcOver,
                "source-in" => SKBlendMode.SrcIn,
                "source-out" => SKBlendMode.SrcOut,
                "source-atop" => SKBlendMode.SrcATop,
                "destination-over" => SKBlendMode.DstOver,
                "destination-in" => SKBlendMode.DstIn,
                "destination-out" => SKBlendMode.DstOut,
                "destination-atop" => SKBlendMode.DstATop,
                "lighter" => SKBlendMode.Plus,
                "copy" => SKBlendMode.Src,
                "xor" => SKBlendMode.Xor,
                "multiply" => SKBlendMode.Multiply,
                "screen" => SKBlendMode.Screen,
                "overlay" => SKBlendMode.Overlay,
                "darken" => SKBlendMode.Darken,
                "lighten" => SKBlendMode.Lighten,
                "color-dodge" => SKBlendMode.ColorDodge,
                "color-burn" => SKBlendMode.ColorBurn,
                "hard-light" => SKBlendMode.HardLight,
                "soft-light" => SKBlendMode.SoftLight,
                "difference" => SKBlendMode.Difference,
                "exclusion" => SKBlendMode.Exclusion,
                "hue" => SKBlendMode.Hue,
                "saturation" => SKBlendMode.Saturation,
                "color" => SKBlendMode.Color,
                "luminosity" => SKBlendMode.Luminosity,
                _ => SKBlendMode.SrcOver
            };
        }

        // ============= SHADOW PROPERTIES =============

        public void shadowBlur(double blur)
        {
            _currentState.ShadowBlur = (float)Math.Max(0, blur);
        }

        public void shadowColor(double r, double g, double b, double a = 255)
        {
            _currentState.ShadowColor = new SKColor(
                (byte)Math.Clamp((int)r, 0, 255),
                (byte)Math.Clamp((int)g, 0, 255),
                (byte)Math.Clamp((int)b, 0, 255),
                (byte)Math.Clamp((int)a, 0, 255)
            );
        }

        public void shadowOffsetX(double offset)
        {
            _currentState.ShadowOffsetX = (float)offset;
        }

        public void shadowOffsetY(double offset)
        {
            _currentState.ShadowOffsetY = (float)offset;
        }

        // ============= GRADIENTS =============

        public object createLinearGradient(double x0, double y0, double x1, double y1)
        {
            return new CanvasGradient(GradientType.Linear, (float)x0, (float)y0, (float)x1, (float)y1);
        }

        public object createRadialGradient(double x0, double y0, double r0, double x1, double y1, double r1)
        {
            return new CanvasGradient(GradientType.Radial, (float)x0, (float)y0, (float)x1, (float)y1, (float)r0, (float)r1);
        }

        public void fillStyleGradient(object gradient)
        {
            if (gradient is CanvasGradient gb)
            {
                _currentState.FillShader?.Dispose();
                _currentState.FillShader = gb.Build();
            }
        }

        public void strokeStyleGradient(object gradient)
        {
            if (gradient is CanvasGradient gb)
            {
                _currentState.StrokeShader?.Dispose();
                _currentState.StrokeShader = gb.Build();
            }
        }

        // ============= COLOR UTILITIES =============

        public object hslToRgb(double h, double s, double l)
        {
            h = Math.Clamp(h, 0, 1);
            s = Math.Clamp(s, 0, 1);
            l = Math.Clamp(l, 0, 1);

            var color = SKColor.FromHsl((float)(h * 360), (float)(s * 100), (float)(l * 100));
            return new { r = (int)color.Red, g = (int)color.Green, b = (int)color.Blue };
        }

        public object rgbToHsl(double r, double g, double b)
        {
            r = Math.Clamp(r / 255.0, 0, 1);
            g = Math.Clamp(g / 255.0, 0, 1);
            b = Math.Clamp(b / 255.0, 0, 1);

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double l = (max + min) / 2;

            if (max == min)
            {
                return new { h = 0.0, s = 0.0, l = l };
            }

            double d = max - min;
            double s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            double h = 0;

            if (max == r)
                h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g)
                h = (b - r) / d + 2;
            else
                h = (r - g) / d + 4;

            h /= 6;
            return new { h = h, s = s, l = l };
        }

        // ============= LEGACY GRADIENT METHOD =============

        public void linearGradient(double x0, double y0, double x1, double y1, double r1, double g1, double b1, double r2, double g2, double b2)
        {
            var colors = new SKColor[]
            {
                new SKColor(
                    (byte)Math.Clamp((int)r1, 0, 255),
                    (byte)Math.Clamp((int)g1, 0, 255),
                    (byte)Math.Clamp((int)b1, 0, 255)
                ),
                new SKColor(
                    (byte)Math.Clamp((int)r2, 0, 255),
                    (byte)Math.Clamp((int)g2, 0, 255),
                    (byte)Math.Clamp((int)b2, 0, 255)
                )
            };

            _currentState.FillShader?.Dispose();

            var shader = SKShader.CreateLinearGradient(
                new SKPoint((float)x0, (float)y0),
                new SKPoint((float)x1, (float)y1),
                colors,
                SKShaderTileMode.Clamp
            );

            _currentState.FillShader = shader;
        }
    }
}