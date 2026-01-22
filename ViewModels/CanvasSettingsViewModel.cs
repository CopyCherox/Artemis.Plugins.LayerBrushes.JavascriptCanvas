using ReactiveUI;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.ViewModels
{
    public class CanvasSettingsViewModel : ReactiveObject
    {
        private readonly JavascriptCanvasBrush _brush;
        private int _canvasWidth = 630;
        private int _canvasHeight = 250;
        private int _frameSkip = 2;

        public CanvasSettingsViewModel(JavascriptCanvasBrush brush)
        {
            _brush = brush;

            // Initialize from brush properties
            _frameSkip = brush.Properties.UpdateEveryNFrames?.CurrentValue ?? 2;

            // Subscribe to property changes from brush
            if (brush.Properties.UpdateEveryNFrames != null)
            {
                brush.Properties.UpdateEveryNFrames.CurrentValueSet += (sender, args) =>
                {
                    var newValue = brush.Properties.UpdateEveryNFrames.CurrentValue;
                    if (_frameSkip != newValue)
                    {
                        _frameSkip = newValue;
                        this.RaisePropertyChanged(nameof(FrameSkip));
                    }
                };
            }
        }

        public int CanvasWidth
        {
            get => _canvasWidth;
            set
            {
                if (_canvasWidth != value)
                {
                    _canvasWidth = value;
                    this.RaisePropertyChanged();
                    OnCanvasSizeChanged();
                }
            }
        }

        public int CanvasHeight
        {
            get => _canvasHeight;
            set
            {
                if (_canvasHeight != value)
                {
                    _canvasHeight = value;
                    this.RaisePropertyChanged();
                    OnCanvasSizeChanged();
                }
            }
        }

        public int FrameSkip
        {
            get => _frameSkip;
            set
            {
                if (_frameSkip != value)
                {
                    _frameSkip = value;
                    this.RaisePropertyChanged();

                    if (_brush?.Properties?.UpdateEveryNFrames != null)
                    {
                        _brush.Properties.UpdateEveryNFrames.SetCurrentValue(value);
                    }
                }
            }
        }

        public event System.EventHandler? CanvasSizeChanged;

        private void OnCanvasSizeChanged()
        {
            CanvasSizeChanged?.Invoke(this, System.EventArgs.Empty);
        }

        public void SetCanvasSize(int width, int height)
        {
            _canvasWidth = width;
            _canvasHeight = height;
            this.RaisePropertyChanged(nameof(CanvasWidth));
            this.RaisePropertyChanged(nameof(CanvasHeight));
            OnCanvasSizeChanged();
        }
    }
}
