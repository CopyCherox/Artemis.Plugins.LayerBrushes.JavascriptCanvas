using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public enum GradientType { Linear, Radial }

    public class CanvasGradient : IDisposable
    {
        private readonly GradientType _type;
        private readonly float _x0, _y0, _x1, _y1, _r0, _r1;
        private readonly List<(float offset, SKColor color)> _colorStops = new List<(float, SKColor)>();
        private SKShader? _cachedShader;
        private bool _disposed = false;

        public CanvasGradient(GradientType type, float x0, float y0, float x1, float y1, float r0 = 0, float r1 = 0)
        {
            _type = type;
            _x0 = x0;
            _y0 = y0;
            _x1 = x1;
            _y1 = y1;
            _r0 = r0;
            _r1 = r1;
        }

        public void addColorStop(double offset, double r, double g, double b, double a = 255)
        {
            var color = new SKColor(
                (byte)Math.Clamp((int)r, 0, 255),
                (byte)Math.Clamp((int)g, 0, 255),
                (byte)Math.Clamp((int)b, 0, 255),
                (byte)Math.Clamp((int)a, 0, 255)
            );
            _colorStops.Add(((float)offset, color));

            // MEMORY LEAK FIX: Invalidate cached shader when colors change
            _cachedShader?.Dispose();
            _cachedShader = null;
        }

        public SKShader Build()
        {
            // Return cached shader if available
            if (_cachedShader != null)
                return _cachedShader;

            if (_colorStops.Count == 0)
            {
                _colorStops.Add((0, SKColors.Black));
                _colorStops.Add((1, SKColors.White));
            }

            var colors = new SKColor[_colorStops.Count];
            var positions = new float[_colorStops.Count];
            for (int i = 0; i < _colorStops.Count; i++)
            {
                positions[i] = _colorStops[i].offset;
                colors[i] = _colorStops[i].color;
            }

            if (_type == GradientType.Linear)
            {
                _cachedShader = SKShader.CreateLinearGradient(
                    new SKPoint(_x0, _y0),
                    new SKPoint(_x1, _y1),
                    colors,
                    positions,
                    SKShaderTileMode.Clamp
                );
            }
            else
            {
                _cachedShader = SKShader.CreateRadialGradient(
                    new SKPoint(_x1, _y1),
                    _r1,
                    colors,
                    positions,
                    SKShaderTileMode.Clamp
                );
            }

            return _cachedShader;
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
                _cachedShader?.Dispose();
                _cachedShader = null;
            }

            _disposed = true;
        }
    }
}
