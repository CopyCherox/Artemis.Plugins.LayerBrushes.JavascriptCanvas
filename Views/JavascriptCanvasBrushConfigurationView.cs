using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;
using TextMateSharp.Grammars;




namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views
{
    public class JavascriptCanvasBrushConfigurationView : UserControl
    {
        private readonly Image _previewImage;
        private readonly TextEditor _codeEditor;
        private readonly TextBox _scriptNameBox;
        private readonly ComboBox _scriptSelector;
        private readonly TextBlock _errorDisplay;
        private readonly NumericUpDown _canvasWidthInput;
        private readonly NumericUpDown _canvasHeightInput;
        private bool _isUpdatingEditor = false;

        public JavascriptCanvasBrushConfigurationView()
        {
            var mainGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,5,*"),
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*"),  // Last row uses "*" to fill space
                Margin = new Thickness(15)
            };




            // ========== TOP: Script Selection and Controls ==========
            var topPanel = new StackPanel { Spacing = 10 };

            // Title bar
            var titleBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };
            titleBar.Children.Add(new TextBlock
            {
                Text = "JavaScript Canvas Script Editor",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });
            topPanel.Children.Add(titleBar);

            // Script selector row
            var selectorPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

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

            selectorPanel.Children.Add(new TextBlock
            {
                Text = "Select Script:",
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            selectorPanel.Children.Add(_scriptSelector);
            selectorPanel.Children.Add(new Button
            {
                Content = "➕ Add Custom",
                [!Button.CommandProperty] = new Avalonia.Data.Binding("AddScriptCommand"),
                Padding = new Thickness(10, 5)
            });
            selectorPanel.Children.Add(new Button
            {
                Content = "🗑️ Delete",
                [!Button.CommandProperty] = new Avalonia.Data.Binding("DeleteScriptCommand"),
                Padding = new Thickness(10, 5)
            });
            topPanel.Children.Add(selectorPanel);

            // Script name (removed checkbox)
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

            var globalCheckbox = new CheckBox
            {
                Content = "🌐 Global Script",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 0, 0),
                [!CheckBox.IsCheckedProperty] = new Avalonia.Data.Binding("SelectedScript.IsGlobal")
            };
            ToolTip.SetTip(globalCheckbox, "When checked, this script will appear in all new brush instances automatically");
            namePanel.Children.Add(globalCheckbox);

            topPanel.Children.Add(namePanel);

            Grid.SetColumn(topPanel, 0);
            Grid.SetColumnSpan(topPanel, 3);
            Grid.SetRow(topPanel, 0);
            mainGrid.Children.Add(topPanel);

            // ========== CANVAS SIZE CONTROLS ==========
            var sizeControlPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.Parse("#34495E"))
            };

            sizeControlPanel.Children.Add(new TextBlock
            {
                Text = "Canvas Size:",
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 10, 15, 10)
            });

            sizeControlPanel.Children.Add(new TextBlock
            {
                Text = "Width:",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 10)
            });

            _canvasWidthInput = new NumericUpDown
            {
                Minimum = 100,
                Maximum = 2000,
                Increment = 50,
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 10),
                [!NumericUpDown.ValueProperty] = new Avalonia.Data.Binding("CanvasWidth")
            };
            sizeControlPanel.Children.Add(_canvasWidthInput);

            sizeControlPanel.Children.Add(new TextBlock
            {
                Text = "Height:",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 10, 0, 10)
            });

            _canvasHeightInput = new NumericUpDown
            {
                Minimum = 50,
                Maximum = 500,
                Increment = 25,
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 10),
                [!NumericUpDown.ValueProperty] = new Avalonia.Data.Binding("CanvasHeight")
            };
            sizeControlPanel.Children.Add(_canvasHeightInput);

            sizeControlPanel.Children.Add(new TextBlock
            {
                Text = "Presets:",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 10, 5, 10)
            });

            // Canvas size preset buttons
            AddCanvasSizePreset(sizeControlPanel, "Ultrawide", 1200, 100);
            AddCanvasSizePreset(sizeControlPanel, "Standard", 800, 150);
            AddCanvasSizePreset(sizeControlPanel, "Compact", 400, 100);
            AddCanvasSizePreset(sizeControlPanel, "Tall", 600, 300);

            // ✅ FRAME SKIP - "Skip every [number] frames" format
            sizeControlPanel.Children.Add(new TextBlock
            {
                Text = "Update every",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 10, 5, 10)
            });

            var frameSkipInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10,
                Increment = 1,
                Width = 100,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 10),

                [!NumericUpDown.ValueProperty] = new Avalonia.Data.Binding("FrameSkip")
            };
            sizeControlPanel.Children.Add(frameSkipInput);

            sizeControlPanel.Children.Add(new TextBlock
            {
                Text = "frames",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 10, 10, 10)
            });


            Grid.SetColumn(sizeControlPanel, 0);
            Grid.SetColumnSpan(sizeControlPanel, 3);
            Grid.SetRow(sizeControlPanel, 1);
            mainGrid.Children.Add(sizeControlPanel);



            // ========== ERROR DISPLAY ==========
            _errorDisplay = new SelectableTextBlock
            {
                Text = "",
                Foreground = Brushes.OrangeRed,
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5),
                [!TextBlock.TextProperty] = new Avalonia.Data.Binding("ErrorMessage")
            };
            Grid.SetColumn(_errorDisplay, 0);
            Grid.SetColumnSpan(_errorDisplay, 3);
            Grid.SetRow(_errorDisplay, 2);
            mainGrid.Children.Add(_errorDisplay);

            // ========== LEFT: Preview Canvas ==========
            // LEFT - Preview Canvas (using DockPanel to anchor help box to bottom)
            var previewPanel = new DockPanel
            {
                Margin = new Thickness(0, 10, 0, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
                LastChildFill = true
            };

            var previewTitle = new TextBlock
            {
                Text = "Live Canvas Preview",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            DockPanel.SetDock(previewTitle, Dock.Top);
            previewPanel.Children.Add(previewTitle);

            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Background = Brushes.Black,
                MinHeight = 150
            };

            _previewImage = new Image
            {
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            scrollViewer.Content = _previewImage;

            var previewBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Black,
                Child = scrollViewer
            };

            DockPanel.SetDock(previewBorder, Dock.Top);
            previewPanel.Children.Add(previewBorder);

            // API Help box - Colored headers (docked to bottom, fills remaining space)
            var apiTextBlock = new SelectableTextBlock
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap
            };

            // Add all the colored runs
            apiTextBlock.Inlines!.Add(new Run { Text = "📖 CANVAS API QUICK REFERENCE (Full CanvasRenderingContext2D)\n\n", Foreground = Brushes.White, FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "══════════════════════════════════════════════════════════════════════\n\n", Foreground = new SolidColorBrush(Color.Parse("#7F8C8D")) });

            apiTextBlock.Inlines.Add(new Run { Text = "DRAWING SHAPES\n", Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "  ctx.fillRect(x,y,w,h)\n  ctx.strokeRect(x,y,w,h)\n  ctx.clearRect(x,y,w,h)\n  ctx.fillCircle(x,y,r)\n  ctx.strokeCircle(x,y,r)\n  ctx.clear(r,g,b)\n\n", Foreground = new SolidColorBrush(Color.Parse("#A8E6CF")) });

            apiTextBlock.Inlines.Add(new Run { Text = "PATHS\n", Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "  ctx.beginPath()\n  ctx.closePath()\n  ctx.fill()\n  ctx.stroke()\n  ctx.clip()\n  ctx.moveTo(x,y)\n  ctx.lineTo(x,y)\n  ctx.drawLine(x1,y1,x2,y2)\n  ctx.arc(x,y,radius,startAngle,endAngle,counterclockwise)\n  ctx.arcTo(x1,y1,x2,y2,radius)\n  ctx.rect(x,y,w,h)\n  ctx.ellipse(x,y,radiusX,radiusY,rotation,startAngle,endAngle,ccw)\n  ctx.quadraticCurveTo(cpx,cpy,x,y)\n  ctx.bezierCurveTo(cp1x,cp1y,cp2x,cp2y,x,y)\n  ctx.isPointInPath(x,y)\n  ctx.isPointInStroke(x,y)\n\n", Foreground = new SolidColorBrush(Color.Parse("#A8E6CF")) });

            apiTextBlock.Inlines.Add(new Run { Text = "STYLES\n", Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "  ctx.fillStyle(r,g,b,a)\n  ctx.strokeStyle(r,g,b,a)\n  ctx.lineWidth(w)\n  ctx.lineCap('butt'|'round'|'square')\n  ctx.lineJoin('miter'|'round'|'bevel')\n  ctx.miterLimit(limit)\n  ctx.globalAlpha(0-1)\n  ctx.globalCompositeOperation(mode)\n\n", Foreground = new SolidColorBrush(Color.Parse("#A8E6CF")) });

            apiTextBlock.Inlines.Add(new Run { Text = "SHADOWS\n", Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "  ctx.shadowBlur(blur)\n  ctx.shadowColor(r,g,b,a)\n  ctx.shadowOffsetX(x)\n  ctx.shadowOffsetY(y)\n\n", Foreground = new SolidColorBrush(Color.Parse("#A8E6CF")) });

            apiTextBlock.Inlines.Add(new Run { Text = "GRADIENTS\n", Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "  grad = ctx.createLinearGradient(x0,y0,x1,y1)\n  grad = ctx.createRadialGradient(x0,y0,r0,x1,y1,r1)\n  grad.addColorStop(offset,r,g,b,a)    // offset: 0-1\n  ctx.fillStyleGradient(grad)\n  ctx.strokeStyleGradient(grad)\n\n", Foreground = new SolidColorBrush(Color.Parse("#A8E6CF")) });

            apiTextBlock.Inlines.Add(new Run { Text = "TRANSFORMATIONS\n", Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "  ctx.save()\n  ctx.restore()\n  ctx.resetTransform()\n  ctx.translate(x,y)\n  ctx.rotate(angle)\n  ctx.scale(x,y)\n  ctx.transform(a,b,c,d,e,f)\n  ctx.setTransform(a,b,c,d,e,f)\n\n", Foreground = new SolidColorBrush(Color.Parse("#A8E6CF")) });

            apiTextBlock.Inlines.Add(new Run { Text = "TEXT\n", Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "  ctx.font('24px Arial')\n  ctx.textAlign('start'|'center'|'end'|'left'|'right')\n  ctx.textBaseline('top'|'middle'|'bottom'|'alphabetic'|'hanging')\n  ctx.fillText(text,x,y)\n  ctx.strokeText(text,x,y)\n  metrics = ctx.measureText(text)      // returns {width}\n\n", Foreground = new SolidColorBrush(Color.Parse("#A8E6CF")) });

            apiTextBlock.Inlines.Add(new Run { Text = "COMPOSITING MODES (globalCompositeOperation)\n", Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "  'source-over' 'multiply' 'screen' 'overlay' 'darken' 'lighten'\n  'color-dodge' 'color-burn' 'hard-light' 'soft-light' 'difference'\n  'exclusion' 'hue' 'saturation' 'color' 'luminosity'\n\n", Foreground = new SolidColorBrush(Color.Parse("#A8E6CF")) });

            apiTextBlock.Inlines.Add(new Run { Text = "GLOBAL VARIABLES\n", Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "  width                                // canvas width\n  height                               // canvas height\n  time                                 // animation time in seconds\n\n", Foreground = new SolidColorBrush(Color.Parse("#A8E6CF")) });

            apiTextBlock.Inlines.Add(new Run { Text = "HELPERS\n", Foreground = new SolidColorBrush(Color.Parse("#FFD700")), FontWeight = FontWeight.Bold });
            apiTextBlock.Inlines.Add(new Run { Text = "  rgb = ctx.hslToRgb(h,s,l)           // h,s,l: 0-1 → {r,g,b}\n  hsl = ctx.rgbToHsl(r,g,b)           // r,g,b: 0-255 → {h,s,l}\n\n", Foreground = new SolidColorBrush(Color.Parse("#A8E6CF")) });

            apiTextBlock.Inlines.Add(new Run { Text = "══════════════════════════════════════════════════════════════════════", Foreground = new SolidColorBrush(Color.Parse("#7F8C8D")) });

            var helpBox = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#2C3E50")),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 10, 0, 0),
                Child = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    Content = apiTextBlock
                }
            };

            // Don't set dock - this makes it fill remaining space (LastChildFill = true)
            previewPanel.Children.Add(helpBox);

            Grid.SetColumn(previewPanel, 0);
            Grid.SetRow(previewPanel, 3);
            mainGrid.Children.Add(previewPanel);






            // ========== RIGHT: Code Editor ==========
            // RIGHT - Code Editor (Plugin-friendly, no syntax highlighting required)
            var editorPanel = new DockPanel
            {
                Margin = new Thickness(0, 10, 0, 0),
                LastChildFill = true
            };

            var editorTitle = new TextBlock
            {
                Text = "JavaScript Code",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            DockPanel.SetDock(editorTitle, Dock.Top);
            editorPanel.Children.Add(editorTitle);

            var applyBtn = new Button
            {
                Content = "✅ Apply Changes to Layer Brush",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                Padding = new Thickness(20, 10),
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.Parse("#27AE60")),
                Foreground = Brushes.White,
                [!Button.CommandProperty] = new Avalonia.Data.Binding("ApplyScriptCommand")
            };
            DockPanel.SetDock(applyBtn, Dock.Bottom);
            editorPanel.Children.Add(applyBtn);

            _codeEditor = new AvaloniaEdit.TextEditor
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

            // Add custom syntax highlighting
            _codeEditor.TextArea.TextView.LineTransformers.Add(new JavaScriptColorizer());

            _codeEditor.TextChanged += (s, e) =>
            {
                if (_isUpdatingEditor) return;
                if (DataContext is ViewModels.JavascriptCanvasBrushConfigurationViewModel vm)
                {
                    vm.EditorCode = _codeEditor.Text ?? string.Empty;
                }
            };


            var editorBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Child = _codeEditor
            };

            editorPanel.Children.Add(editorBorder);

            Grid.SetColumn(editorPanel, 2);
            Grid.SetRow(editorPanel, 3);
            mainGrid.Children.Add(editorPanel);








            Content = mainGrid;

            DataContextChanged += (s, e) =>
            {
                if (DataContext is ViewModels.JavascriptCanvasBrushConfigurationViewModel vm)
                {
                    vm.WhenAnyValue(x => x.PreviewBitmap)
                        .Subscribe(bitmap => UpdatePreviewImage(bitmap));

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
            };



        }



        private void AddCanvasSizePreset(WrapPanel panel, string name, int width, int height)
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
            panel.Children.Add(btn);
        }

        private void UpdatePreviewImage(SKBitmap? skBitmap)
        {
            if (skBitmap == null) return;

            try
            {
                // Direct pixel access using GetPixels()
                IntPtr pixelsPtr = skBitmap.GetPixels();
                if (pixelsPtr == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to get pixels from SKBitmap");
                    return;
                }

                int byteCount = skBitmap.Width * skBitmap.Height * 4;
                byte[] pixelBytes = new byte[byteCount];
                Marshal.Copy(pixelsPtr, pixelBytes, 0, byteCount);

                var avaBitmap = new WriteableBitmap(
                    new PixelSize(skBitmap.Width, skBitmap.Height),
                    new Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul
                );

                using (var buffer = avaBitmap.Lock())
                {
                    Marshal.Copy(pixelBytes, 0, buffer.Address, pixelBytes.Length);
                }

                _previewImage.Width = skBitmap.Width;
                _previewImage.Height = skBitmap.Height;
                _previewImage.Source = avaBitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preview update error: {ex.Message}");
            }
        }


    }


    // Add this class at the bottom of your HTMLCanvasBrushConfigurationView.cs file (outside the main class)
    public class JavaScriptColorizer : AvaloniaEdit.Rendering.DocumentColorizingTransformer
    {
        private static readonly string[] Keywords =
        {
        "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch",
        "case", "break", "continue", "return", "new", "this", "class", "extends",
        "true", "false", "null", "undefined", "typeof", "instanceof", "in", "of",
        "try", "catch", "finally", "throw", "async", "await", "yield", "import",
        "export", "default", "from", "as", "debugger", "delete", "void", "with"
    };

        protected override void ColorizeLine(AvaloniaEdit.Document.DocumentLine line)
        {
            var lineText = CurrentContext.Document.GetText(line);
            int lineStartOffset = line.Offset;

            // Color keywords
            foreach (var keyword in Keywords)
            {
                int index = 0;
                while ((index = lineText.IndexOf(keyword, index)) >= 0)
                {
                    // Check if it's a whole word
                    bool isWholeWord = (index == 0 || !char.IsLetterOrDigit(lineText[index - 1])) &&
                                       (index + keyword.Length >= lineText.Length || !char.IsLetterOrDigit(lineText[index + keyword.Length]));

                    if (isWholeWord)
                    {
                        ChangeLinePart(
                            lineStartOffset + index,
                            lineStartOffset + index + keyword.Length,
                            (AvaloniaEdit.Rendering.VisualLineElement element) =>
                            {
                                element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(Color.Parse("#569CD6"))); // Blue
                            });
                    }
                    index += keyword.Length;
                }
            }

            // Color strings
            for (int i = 0; i < lineText.Length; i++)
            {
                if (lineText[i] == '"' || lineText[i] == '\'' || lineText[i] == '`')
                {
                    char quote = lineText[i];
                    int start = i;
                    i++;
                    while (i < lineText.Length && lineText[i] != quote)
                    {
                        if (lineText[i] == '\\' && i + 1 < lineText.Length)
                            i++; // Skip escaped character
                        i++;
                    }
                    if (i < lineText.Length)
                    {
                        ChangeLinePart(
                            lineStartOffset + start,
                            lineStartOffset + i + 1,
                            (AvaloniaEdit.Rendering.VisualLineElement element) =>
                            {
                                element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(Color.Parse("#CE9178"))); // Orange
                            });
                    }
                }
            }

            // Color comments
            int commentIndex = lineText.IndexOf("//");
            if (commentIndex >= 0)
            {
                ChangeLinePart(
                    lineStartOffset + commentIndex,
                    lineStartOffset + lineText.Length,
                    (AvaloniaEdit.Rendering.VisualLineElement element) =>
                    {
                        element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(Color.Parse("#6A9955"))); // Green
                    });
            }

            // Color numbers
            for (int i = 0; i < lineText.Length; i++)
            {
                if (char.IsDigit(lineText[i]))
                {
                    int start = i;
                    while (i < lineText.Length && (char.IsDigit(lineText[i]) || lineText[i] == '.'))
                        i++;

                    ChangeLinePart(
                        lineStartOffset + start,
                        lineStartOffset + i,
                        (AvaloniaEdit.Rendering.VisualLineElement element) =>
                        {
                            element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(Color.Parse("#B5CEA8"))); // Light green
                        });
                }
            }
        }
    }

}