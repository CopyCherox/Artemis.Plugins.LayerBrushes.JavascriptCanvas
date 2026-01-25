using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views.Controls
{
    public class ScriptSelectorPanel : StackPanel
    {
        private ComboBox _scriptSelector;
        private TextBox _scriptNameBox;

        public ScriptSelectorPanel()
        {
            Spacing = 10;

            // Title bar
            var titleBar = CreateTitleBar();
            Children.Add(titleBar);

            // Script selector row
            var selectorPanel = CreateSelectorPanel();
            Children.Add(selectorPanel);

            // Script name row
            var namePanel = CreateNamePanel();
            Children.Add(namePanel);

            _scriptNameBox = new TextBox();
            _scriptSelector = new ComboBox();
        }

        private StackPanel CreateTitleBar()
        {
            var titleBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            titleBar.Children.Add(new TextBlock
            {
                Text = "Javascript Canvas Script Editor",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });

            titleBar.Children.Add(new Border
            {
                Width = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch
            });

            var unsavedIndicator = new TextBlock
            {
                Text = "● Unsaved Changes",
                Foreground = new SolidColorBrush(Color.Parse("#F39C12")),
                FontWeight = FontWeight.Bold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0)
            };
            unsavedIndicator[!TextBlock.IsVisibleProperty] = new Avalonia.Data.Binding("HasUnsavedChanges");
            titleBar.Children.Add(unsavedIndicator);

            return titleBar;
        }

        private StackPanel CreateSelectorPanel()
        {
            var selectorPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            selectorPanel.Children.Add(new TextBlock
            {
                Text = "Select Script:",
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });

            _scriptSelector = new ComboBox
            {
                MinWidth = 300,
                VerticalAlignment = VerticalAlignment.Center,
                [!ComboBox.ItemsSourceProperty] = new Avalonia.Data.Binding("Scripts"),
                [!ComboBox.SelectedItemProperty] = new Avalonia.Data.Binding("SelectedScript")
            };
            _scriptSelector.ItemTemplate = new FuncDataTemplate<object>((value, namescope) =>
            {
                return new TextBlock { [!TextBlock.TextProperty] = new Avalonia.Data.Binding("ScriptName") };
            });
            selectorPanel.Children.Add(_scriptSelector);

            selectorPanel.Children.Add(CreateButton("➕ Add Custom", "AddScriptCommand", null));
            selectorPanel.Children.Add(CreateButton("🗑️ Delete", "DeleteScriptCommand", null));
            selectorPanel.Children.Add(CreateButton("Export...", "ExportScriptCommand", "#16A085"));
            selectorPanel.Children.Add(CreateButton("Import...", "ImportScriptCommand", "#8E44AD"));

            return selectorPanel;
        }

        private StackPanel CreateNamePanel()
        {
            var namePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            namePanel.Children.Add(new TextBlock
            {
                Text = "Script Name:",
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });

            _scriptNameBox = new TextBox
            {
                MinWidth = 250,
                [!TextBox.TextProperty] = new Avalonia.Data.Binding("SelectedScript.ScriptName")
            };
            namePanel.Children.Add(_scriptNameBox);

            return namePanel;
        }

        private Button CreateButton(string content, string commandBinding, string? backgroundColor)
        {
            var button = new Button
            {
                Content = content,
                [!Button.CommandProperty] = new Avalonia.Data.Binding(commandBinding),
                Padding = new Thickness(10, 5)
            };

            if (backgroundColor != null)
            {
                button.Background = new SolidColorBrush(Color.Parse(backgroundColor));
                button.Foreground = Brushes.White;
            }

            return button;
        }
    }
}
