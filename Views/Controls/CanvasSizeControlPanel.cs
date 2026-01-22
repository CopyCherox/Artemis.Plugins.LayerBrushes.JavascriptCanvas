using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views.Controls
{
    public class CanvasSizeControlPanel : WrapPanel
    {
        private readonly NumericUpDown _canvasWidthInput;
        private readonly NumericUpDown _canvasHeightInput;

        public CanvasSizeControlPanel()
        {
            Orientation = Orientation.Horizontal;
            Margin = new Thickness(0, 10, 0, 10);
            Background = new SolidColorBrush(Color.Parse("#34495E"));

            AddLabel("Canvas Size:", bold: true, leftMargin: 10, rightMargin: 15);

            // Width controls
            AddLabel("Width:");
            _canvasWidthInput = AddNumericInput(100, 2000, 50, "CanvasWidth");

            // Height controls
            AddLabel("Height:", leftMargin: 15);
            _canvasHeightInput = AddNumericInput(50, 500, 25, "CanvasHeight");

            // Presets
            AddLabel("Presets:", bold: true, leftMargin: 20, rightMargin: 5);
            AddPresetButton("Ultrawide", 1200, 200);
            AddPresetButton("Standard", 630, 250);
            AddPresetButton("Compact", 400, 100);
            AddPresetButton("Tall", 630, 400);

            // Frame skip
            AddLabel("Update every", leftMargin: 20, rightMargin: 5);
            var frameSkipInput = AddNumericInput(1, 10, 1, "FrameSkip", width: 100);
            AddLabel("frames", leftMargin: 5, rightMargin: 10);
        }

        private void AddLabel(string text, bool bold = false, int leftMargin = 0, int rightMargin = 0)
        {
            Children.Add(new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(leftMargin, 10, rightMargin, 10)
            });
        }

        private NumericUpDown AddNumericInput(decimal min, decimal max, decimal increment,
            string bindingPath, int width = 120)
        {
            var input = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Increment = increment,
                Width = width,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 10),
                [!NumericUpDown.ValueProperty] = new Avalonia.Data.Binding(bindingPath)
            };
            Children.Add(input);
            return input;
        }

        private void AddPresetButton(string name, int width, int height)
        {
            var btn = new Button
            {
                Content = name,
                Margin = new Thickness(5, 10),
                Padding = new Thickness(10, 5),
                Background = new SolidColorBrush(Color.Parse("#3498DB")),
                Foreground = Brushes.White
            };

            btn.Click += (s, e) =>
            {
                if (DataContext is ViewModels.JavascriptCanvasBrushConfigurationViewModel vm)
                {
                    vm.CanvasWidth = width;
                    vm.CanvasHeight = height;
                }
            };

            Children.Add(btn);
        }
    }
}
