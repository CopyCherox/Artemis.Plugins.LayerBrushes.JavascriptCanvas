using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class TimeControl
    {
        private readonly Action<double> _setTimeScaleCallback;
        private readonly Action<bool> _setPausedCallback;
        private readonly Func<double> _getTimeCallback;

        private double _timeScale = 1.0;
        private bool _isPaused = false;

        public TimeControl(
            Action<double> setTimeScaleCallback,
            Action<bool> setPausedCallback,
            Func<double> getTimeCallback)
        {
            _setTimeScaleCallback = setTimeScaleCallback;
            _setPausedCallback = setPausedCallback;
            _getTimeCallback = getTimeCallback;
        }

        // Properties accessible from JavaScript
        public double Speed
        {
            get => _timeScale;
            set
            {
                _timeScale = Math.Clamp(value, 0.0, 10.0);
                _setTimeScaleCallback?.Invoke(_timeScale);
            }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;
                _setPausedCallback?.Invoke(_isPaused);
            }
        }

        public double Current => _getTimeCallback?.Invoke() ?? 0;

        // Methods accessible from JavaScript
        public void SetSpeed(double speed)
        {
            Speed = Math.Clamp(speed, 0.0, 10.0);
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void Toggle()
        {
            IsPaused = !IsPaused;
        }
    }
}
