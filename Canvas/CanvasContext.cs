using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public partial class CanvasContext : IDisposable
    {
        private readonly SKCanvas _canvas;
        private readonly SKPaint _paint;
        private readonly int _width;
        private readonly int _height;
        private readonly Stack<CanvasState> _stateStack = new Stack<CanvasState>();
        private SKPath? _currentPath;
        private CanvasState _currentState;
        private bool _disposed = false;

        public CanvasContext(SKCanvas canvas, SKPaint paint, int width, int height)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _paint = paint ?? throw new ArgumentNullException(nameof(paint));
            _width = width;
            _height = height;

            _currentState = new CanvasState
            {
                Transform = SKMatrix.Identity,
                GlobalAlpha = 1.0f,
                FillColor = SKColors.White,
                StrokeColor = SKColors.White,
                LineWidth = 1.0f,
                LineCap = SKStrokeCap.Butt,
                LineJoin = SKStrokeJoin.Miter,
                MiterLimit = 10.0f,
                ShadowBlur = 0,
                ShadowColor = SKColors.Transparent,
                ShadowOffsetX = 0,
                ShadowOffsetY = 0,
                TextAlign = "start",
                TextBaseline = "alphabetic",
                FontSize = 12,
                FontFamily = "Arial",
                BlendMode = SKBlendMode.SrcOver
            };
        }

        // MEMORY LEAK FIX: Implement IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _currentPath?.Dispose();
                _currentPath = null;

                // Dispose all shaders in current state
                _currentState.FillShader?.Dispose();
                _currentState.StrokeShader?.Dispose();

                // Dispose all shaders in state stack
                while (_stateStack.Count > 0)
                {
                    var state = _stateStack.Pop();
                    state.FillShader?.Dispose();
                    state.StrokeShader?.Dispose();
                }
            }

            _disposed = true;
        }

        // Internal helpers
        private void ApplyPaintState(SKPaintStyle style)
        {
            _paint.Style = style;
            _paint.BlendMode = _currentState.BlendMode;

            if (style == SKPaintStyle.Fill)
            {
                if (_currentState.FillShader != null)
                    _paint.Shader = _currentState.FillShader;
                else
                {
                    _paint.Shader = null;
                    _paint.Color = ApplyAlpha(_currentState.FillColor);
                }
            }
            else
            {
                if (_currentState.StrokeShader != null)
                    _paint.Shader = _currentState.StrokeShader;
                else
                {
                    _paint.Shader = null;
                    _paint.Color = ApplyAlpha(_currentState.StrokeColor);
                }
                _paint.StrokeWidth = _currentState.LineWidth;
                _paint.StrokeCap = _currentState.LineCap;
                _paint.StrokeJoin = _currentState.LineJoin;
                _paint.StrokeMiter = _currentState.MiterLimit;
            }

            _paint.ImageFilter = null;
            _paint.MaskFilter = null;
        }

        private bool HasActiveShadow => _currentState.ShadowBlur > 0 && _currentState.ShadowColor.Alpha > 0;

        private void DrawWithShadow(Action drawAction)
        {
            if (_currentState.ShadowBlur <= 0 || _currentState.ShadowColor.Alpha == 0)
            {
                drawAction();
                return;
            }

            float blurRadius = Math.Max(1f, _currentState.ShadowBlur * 0.5f);
            var originalColor = _paint.Color;
            var originalShader = _paint.Shader;
            var originalMaskFilter = _paint.MaskFilter;

            // Shadow pass
            _canvas.Save();
            _canvas.Translate(_currentState.ShadowOffsetX, _currentState.ShadowOffsetY);

            var shadowMaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blurRadius);
            _paint.MaskFilter = shadowMaskFilter;
            _paint.Color = _currentState.ShadowColor;
            _paint.Shader = null;
            drawAction();
            _canvas.Restore();

            // MEMORY LEAK FIX: Dispose mask filter
            shadowMaskFilter?.Dispose();

            // Reset paint state
            _paint.MaskFilter = originalMaskFilter;
            _paint.Color = originalColor;
            _paint.Shader = originalShader;

            // Main draw pass
            drawAction();
        }

        private SKColor ApplyAlpha(SKColor color)
        {
            byte alpha = (byte)(color.Alpha * _currentState.GlobalAlpha);
            return new SKColor(color.Red, color.Green, color.Blue, alpha);
        }
    }
}
