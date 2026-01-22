using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services
{
    public class DialogService
    {
        public async Task<bool> ShowDeleteConfirmation(Window owner, string scriptName)
        {
            var dialog = new Window
            {
                Title = "Confirm Delete",
                Width = 400,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.Parse("#2D2D2D"))
            };

            var mainPanel = new StackPanel
            {
                Margin = new Thickness(30, 20, 30, 20),
                Spacing = 25,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            mainPanel.Children.Add(new TextBlock
            {
                Text = $"Are you sure you want to delete '{scriptName}'?",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var buttonGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,15,Auto,*"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 120,
                Height = 32,
                Padding = new Thickness(10, 5),
                Background = new SolidColorBrush(Color.Parse("#95A5A6")),
                Foreground = Brushes.White
            };

            var deleteButton = new Button
            {
                Content = "Yes, Delete",
                Width = 120,
                Height = 32,
                Padding = new Thickness(10, 5),
                Background = new SolidColorBrush(Color.Parse("#E74C3C")),
                Foreground = Brushes.White
            };

            cancelButton.Click += (s, e) => dialog.Close(false);
            deleteButton.Click += (s, e) => dialog.Close(true);

            Grid.SetColumn(cancelButton, 1);
            Grid.SetColumn(deleteButton, 3);
            buttonGrid.Children.Add(cancelButton);
            buttonGrid.Children.Add(deleteButton);

            mainPanel.Children.Add(buttonGrid);
            dialog.Content = mainPanel;

            var result = await dialog.ShowDialog<bool?>(owner);
            return result == true;
        }

        public async Task ShowImportSummary(Window owner, int successCount, int totalCount, int errorCount)
        {
            var summaryDialog = new Window
            {
                Title = "Import Complete",
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = new SolidColorBrush(Color.Parse("#2D2D2D"))
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 20
            };

            var message = $"Imported {successCount} of {totalCount} script(s).";
            if (errorCount > 0)
                message += $"\n\n{errorCount} script(s) failed to import.";

            panel.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White
            });

            var okButton = new Button
            {
                Content = "OK",
                Width = 100,
                Padding = new Thickness(10, 5),
                Background = new SolidColorBrush(Color.Parse("#3498DB")),
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            okButton.Click += (s, e) => summaryDialog.Close();
            panel.Children.Add(okButton);
            summaryDialog.Content = panel;

            await summaryDialog.ShowDialog(owner);
        }
    }
}
