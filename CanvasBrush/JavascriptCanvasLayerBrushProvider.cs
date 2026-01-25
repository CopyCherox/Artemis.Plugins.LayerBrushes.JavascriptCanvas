using Artemis.Core;
using Artemis.Core.LayerBrushes;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class JavascriptCanvasBrushProvider : LayerBrushProvider
    {
        public override void Enable()
        {
            // Just register the brush - Artemis will look for the ViewModel/View automatically
            RegisterLayerBrushDescriptor<JavascriptCanvasBrush>(
                "Javascript Canvas",
                "Execute Javascript for LED effects",
                "Code"
            );
        }

        public override void Disable()
        {
        }
    }
}