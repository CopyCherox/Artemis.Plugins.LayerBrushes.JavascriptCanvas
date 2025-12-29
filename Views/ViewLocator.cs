using Artemis.Plugins.LayerBrushes.JavascriptCanvas.ViewModels;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? data)
        {
            if (data is JavascriptCanvasBrushConfigurationViewModel)
            {
                return new JavascriptCanvasBrushConfigurationView();
            }

            return new TextBlock { Text = "View not found" };
        }

        public bool Match(object? data)
        {
            return data is JavascriptCanvasBrushConfigurationViewModel;
        }
    }
}