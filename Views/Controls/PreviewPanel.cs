using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views.Controls
{
    public class PreviewPanel : DockPanel
    {
        private readonly Image _previewImage;

        public PreviewPanel()
        {
            Margin = new Thickness(0, 10, 0, 0);
            VerticalAlignment = VerticalAlignment.Stretch;
            LastChildFill = true;

            var previewTitle = new TextBlock
            {
                Text = "Live Canvas Preview",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            DockPanel.SetDock(previewTitle, Dock.Top);
            Children.Add(previewTitle);

            _previewImage = new Image
            {
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Background = Brushes.Black,
                MinHeight = 150,
                Content = _previewImage
            };

            var previewBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Black,
                Child = scrollViewer
            };
            DockPanel.SetDock(previewBorder, Dock.Top);
            Children.Add(previewBorder);

            var helpBox = new ApiReferenceControl();
            Children.Add(helpBox);

            DataContextChanged += (s, e) =>
            {
                if (DataContext is ViewModels.JavascriptCanvasBrushConfigurationViewModel vm)
                {
                    vm.WhenAnyValue(x => x.PreviewBitmap)
                        .Subscribe(bitmap => UpdatePreviewImage(bitmap));
                }
            };
        }

        private void UpdatePreviewImage(SKBitmap? skBitmap)
        {
            if (skBitmap == null) return;

            try
            {
                IntPtr pixelsPtr = skBitmap.GetPixels();
                if (pixelsPtr == IntPtr.Zero)
                    return;

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
            catch (Exception)
            {
                // Silently handle preview errors
            }
        }
    }
}