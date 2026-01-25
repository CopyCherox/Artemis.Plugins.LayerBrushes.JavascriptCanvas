using Avalonia.Controls;
using SkiaSharp;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public partial class CanvasContext
    {
        // ============= PATH METHODS =============
        public void beginPath()
        {
            _currentPath?.Dispose();
            _currentPath = new SKPath();
        }

        public void closePath()
        {
            _currentPath?.Close();
        }

        public void moveTo(double x, double y)
        {
            if (_currentPath == null) beginPath();
            _currentPath?.MoveTo((float)x, (float)y);
        }

        public void lineTo(double x, double y)
        {
            if (_currentPath == null) beginPath();
            _currentPath?.LineTo((float)x, (float)y);
        }

        public void arc(double x, double y, double radius, double startAngle, double endAngle, bool counterclockwise = false)
        {
            if (_currentPath == null) beginPath();
            float startDeg = (float)(startAngle * 180 / Math.PI);
            float endDeg = (float)(endAngle * 180 / Math.PI);
            float sweep = endDeg - startDeg;
            if (counterclockwise && sweep > 0)
                sweep -= 360;
            else if (!counterclockwise && sweep < 0)
                sweep += 360;
            var rect = new SKRect(
                (float)(x - radius),
                (float)(y - radius),
                (float)(x + radius),
                (float)(y + radius)
            );
            _currentPath?.ArcTo(rect, startDeg, sweep, false);
        }

        public void arcTo(double x1, double y1, double x2, double y2, double radius)
        {
            if (_currentPath == null) beginPath();
            _currentPath?.ArcTo((float)x1, (float)y1, (float)x2, (float)y2, (float)radius);
        }

        public void quadraticCurveTo(double cpx, double cpy, double x, double y)
        {
            if (_currentPath == null) beginPath();
            _currentPath?.QuadTo((float)cpx, (float)cpy, (float)x, (float)y);
        }

        public void bezierCurveTo(double cp1x, double cp1y, double cp2x, double cp2y, double x, double y)
        {
            if (_currentPath == null) beginPath();
            _currentPath?.CubicTo((float)cp1x, (float)cp1y, (float)cp2x, (float)cp2y, (float)x, (float)y);
        }

        public void rect(double x, double y, double w, double h)
        {
            if (_currentPath == null) beginPath();
            _currentPath?.AddRect(new SKRect((float)x, (float)y, (float)(x + w), (float)(y + h)));
        }

        public void ellipse(double x, double y, double radiusX, double radiusY, double rotation, double startAngle, double endAngle, bool counterclockwise = false)
        {
            if (_currentPath == null) beginPath();
            var rect = new SKRect(
                (float)(x - radiusX),
                (float)(y - radiusY),
                (float)(x + radiusX),
                (float)(y + radiusY)
            );
            float startDeg = (float)(startAngle * 180 / Math.PI);
            float endDeg = (float)(endAngle * 180 / Math.PI);
            float sweep = endDeg - startDeg;
            if (counterclockwise && sweep > 0)
                sweep -= 360;
            else if (!counterclockwise && sweep < 0)
                sweep += 360;

            using var tempPath = new SKPath();
            tempPath.AddArc(rect, startDeg, sweep);

            if (rotation != 0)
            {
                var matrix = SKMatrix.CreateRotation((float)rotation, (float)x, (float)y);
                // FIXED: Use AddPath with matrix overload instead of Transform
                _currentPath?.AddPath(tempPath, in matrix);
            }
            else
            {
                _currentPath?.AddPath(tempPath);
            }
        }

        public void fill()
        {
            if (_currentPath == null) return;
            ApplyPaintState(SKPaintStyle.Fill);
            DrawWithShadow(() => _canvas.DrawPath(_currentPath, _paint));
        }

        public void stroke()
        {
            if (_currentPath == null) return;
            ApplyPaintState(SKPaintStyle.Stroke);
            DrawWithShadow(() => _canvas.DrawPath(_currentPath, _paint));
        }

        public void clip()
        {
            if (_currentPath != null)
                _canvas.ClipPath(_currentPath);
        }

        public bool isPointInPath(double x, double y)
        {
            if (_currentPath == null) return false;
            return _currentPath.Contains((float)x, (float)y);
        }

        public bool isPointInStroke(double x, double y)
        {
            if (_currentPath == null) return false;
            using var strokedPath = _paint.GetFillPath(_currentPath);
            return strokedPath?.Contains((float)x, (float)y) ?? false;
        }
    }
}
