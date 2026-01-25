using Avalonia.Controls;
using SkiaSharp;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public partial class CanvasContext
    {
        // ============= TRANSFORMATIONS =============

        public void save()
        {
            var state = _currentState.Clone();
            _stateStack.Push(state);
            _canvas.Save();
        }

        public void restore()
        {
            if (_stateStack.Count > 0)
            {
                _currentState = _stateStack.Pop();
                _canvas.Restore();
            }
        }

        public void translate(double x, double y)
        {
            _canvas.Translate((float)x, (float)y);
        }

        public void rotate(double angle)
        {
            _canvas.RotateRadians((float)angle);
        }

        public void scale(double x, double y)
        {
            _canvas.Scale((float)x, (float)y);
        }

        public void transform(double a, double b, double c, double d, double e, double f)
        {
            var matrix = new SKMatrix
            {
                ScaleX = (float)a,
                SkewY = (float)b,
                SkewX = (float)c,
                ScaleY = (float)d,
                TransX = (float)e,
                TransY = (float)f,
                Persp2 = 1
            };
            _canvas.Concat(in matrix);
        }

        public void setTransform(double a, double b, double c, double d, double e, double f)
        {
            _canvas.ResetMatrix();
            transform(a, b, c, d, e, f);
        }

        public void resetTransform()
        {
            _canvas.ResetMatrix();
        }
    }
}