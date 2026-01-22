using Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicData;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views
{
    public class JavascriptCanvasBrushConfigurationView : UserControl
    {
        public JavascriptCanvasBrushConfigurationView()
        {
            var mainGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,5,*"),
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*"),
                Margin = new Thickness(15)
            };

            // Top section - Script selector
            var topPanel = new ScriptSelectorPanel();
            Grid.SetColumn(topPanel, 0);
            Grid.SetColumnSpan(topPanel, 3);
            Grid.SetRow(topPanel, 0);
            mainGrid.Children.Add(topPanel);

            // Canvas size controls
            var sizeControlPanel = new CanvasSizeControlPanel();
            Grid.SetColumn(sizeControlPanel, 0);
            Grid.SetColumnSpan(sizeControlPanel, 3);
            Grid.SetRow(sizeControlPanel, 1);
            mainGrid.Children.Add(sizeControlPanel);

            // Error display
            var errorDisplay = new SelectableTextBlock
            {
                Text = "",
                Foreground = Avalonia.Media.Brushes.OrangeRed,
                FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5),
                [!TextBlock.TextProperty] = new Avalonia.Data.Binding("ErrorMessage")
            };
            Grid.SetColumn(errorDisplay, 0);
            Grid.SetColumnSpan(errorDisplay, 3);
            Grid.SetRow(errorDisplay, 2);
            mainGrid.Children.Add(errorDisplay);

            // Left - Preview panel
            var previewPanel = new PreviewPanel();
            Grid.SetColumn(previewPanel, 0);
            Grid.SetRow(previewPanel, 3);
            mainGrid.Children.Add(previewPanel);

            // Right - Code editor
            var editorPanel = new CodeEditorPanel();
            Grid.SetColumn(editorPanel, 2);
            Grid.SetRow(editorPanel, 3);
            mainGrid.Children.Add(editorPanel);

            Content = mainGrid;
        }
    }
}
