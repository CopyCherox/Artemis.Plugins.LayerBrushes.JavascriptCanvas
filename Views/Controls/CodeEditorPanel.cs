using Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views.Syntax;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaEdit;
using ReactiveUI;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views.Controls
{
    public class CodeEditorPanel : DockPanel
    {
        private readonly TextEditor _codeEditor;
        private bool _isUpdatingEditor = false;

        public CodeEditorPanel()
        {
            Margin = new Thickness(0, 10, 0, 0);
            LastChildFill = true;

            // Title
            var editorTitle = new TextBlock
            {
                Text = "Javascript Code",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            DockPanel.SetDock(editorTitle, Dock.Top);
            Children.Add(editorTitle);

            // Apply button
            var applyBtn = CreateApplyButton();
            DockPanel.SetDock(applyBtn, Dock.Bottom);
            Children.Add(applyBtn);

            // Code editor
            _codeEditor = CreateCodeEditor();
            var editorBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Child = _codeEditor
            };
            Children.Add(editorBorder);

            // Setup data context change handler
            DataContextChanged += OnDataContextChanged;
        }

        private Button CreateApplyButton()
        {
            var applyBtn = new Button
            {
                Content = "Apply Changes to Layer Brush",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                Padding = new Thickness(20, 10),
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.Parse("#27AE60")),
                Foreground = Brushes.White
            };

            applyBtn[!Button.BackgroundProperty] = new Avalonia.Data.Binding("HasUnsavedChanges")
            {
                Converter = new UnsavedChangesColorConverter()
            };
            applyBtn[!Button.CommandProperty] = new Avalonia.Data.Binding("ApplyScriptCommand");

            return applyBtn;
        }

        private TextEditor CreateCodeEditor()
        {
            var editor = new TextEditor
            {
                FontFamily = new FontFamily("Consolas,Courier New,monospace"),
                FontSize = 13,
                Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
                Foreground = new SolidColorBrush(Color.Parse("#D4D4D4")),
                ShowLineNumbers = true,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                WordWrap = false
            };

            // Add syntax highlighting
            editor.TextArea.TextView.LineTransformers.Add(new JavaScriptColorizer());

            editor.TextChanged += (s, e) =>
            {
                if (_isUpdatingEditor) return;
                if (DataContext is ViewModels.JavascriptCanvasBrushConfigurationViewModel vm)
                {
                    vm.EditorCode = editor.Text ?? string.Empty;
                }
            };

            return editor;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is ViewModels.JavascriptCanvasBrushConfigurationViewModel vm)
            {
                vm.WhenAnyValue(x => x.EditorCode)
                    .Subscribe(code =>
                    {
                        if (!string.IsNullOrEmpty(code) && _codeEditor.Text != code)
                        {
                            _isUpdatingEditor = true;
                            _codeEditor.Text = code;
                            _isUpdatingEditor = false;
                        }
                    });
            }
        }
    }

    public class UnsavedChangesColorConverter : Avalonia.Data.Converters.IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool hasUnsaved && hasUnsaved)
            {
                return new SolidColorBrush(Color.Parse("#F39C12"));
            }
            return new SolidColorBrush(Color.Parse("#27AE60"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
