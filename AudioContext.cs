using Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class AudioContext
    {
        private readonly AudioReactivityService _audioService;

        public AudioContext(AudioReactivityService audioService)
        {
            _audioService = audioService;
        }

        public float Bass => _audioService?.Bass ?? 0;
        public float Midrange => _audioService?.Midrange ?? 0;
        public float Treble => _audioService?.Treble ?? 0;
        public float Volume => _audioService?.Volume ?? 0;
        public bool IsEnabled => _audioService?.IsEnabled ?? false;

        public float GetBand(int index)
        {
            if (_audioService == null || index < 0 || index >= _audioService.FrequencyBands.Length)
                return 0;
            return _audioService.FrequencyBands[index];
        }

        public float GetRange(int startBand, int endBand)
        {
            if (_audioService == null)
                return 0;

            startBand = Math.Clamp(startBand, 0, _audioService.FrequencyBands.Length - 1);
            endBand = Math.Clamp(endBand, 0, _audioService.FrequencyBands.Length - 1);

            if (startBand > endBand) return 0;

            float sum = 0;
            for (int i = startBand; i <= endBand; i++)
            {
                sum += _audioService.FrequencyBands[i];
            }
            return sum / (endBand - startBand + 1);
        }
    }
}