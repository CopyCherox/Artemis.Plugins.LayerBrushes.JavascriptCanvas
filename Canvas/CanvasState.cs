using SkiaSharp;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class CanvasState
    {
        public SKMatrix Transform { get; set; }
        public float GlobalAlpha { get; set; }
        public SKColor FillColor { get; set; }
        public SKColor StrokeColor { get; set; }
        public float LineWidth { get; set; }
        public SKStrokeCap LineCap { get; set; }
        public SKStrokeJoin LineJoin { get; set; }
        public float MiterLimit { get; set; }
        public float ShadowBlur { get; set; }
        public SKColor ShadowColor { get; set; }
        public float ShadowOffsetX { get; set; }
        public float ShadowOffsetY { get; set; }
        public SKShader? FillShader { get; set; }
        public SKShader? StrokeShader { get; set; }
        public string TextAlign { get; set; } = "start";
        public string TextBaseline { get; set; } = "alphabetic";
        public float FontSize { get; set; } = 12;
        public string FontFamily { get; set; } = "Arial";
        public SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver;

        public CanvasState Clone()
        {
            return new CanvasState
            {
                Transform = Transform,
                GlobalAlpha = GlobalAlpha,
                FillColor = FillColor,
                StrokeColor = StrokeColor,
                LineWidth = LineWidth,
                LineCap = LineCap,
                LineJoin = LineJoin,
                MiterLimit = MiterLimit,
                ShadowBlur = ShadowBlur,
                ShadowColor = ShadowColor,
                ShadowOffsetX = ShadowOffsetX,
                ShadowOffsetY = ShadowOffsetY,
                FillShader = FillShader,
                StrokeShader = StrokeShader,
                TextAlign = TextAlign,
                TextBaseline = TextBaseline,
                FontSize = FontSize,
                FontFamily = FontFamily,
                BlendMode = BlendMode
            };
        }
    }
}