using Artemis.Core;
using Artemis.Core.Modules;
using System.Collections.Generic;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    [PluginFeature(Name = "Javascript Canvas Performance")]
    public class JavascriptCanvasModule : Module<JavascriptCanvasDataModel>
    {
        public override List<IModuleActivationRequirement> ActivationRequirements => new();

        public override void Enable()
        {
            // Module initialization
        }

        public override void Disable()
        {
            // Module cleanup
        }

        public override void Update(double deltaTime)
        {
            // This will be tracked in Performance tab
            // You can add performance monitoring logic here
        }
    }

    public class JavascriptCanvasDataModel : DataModel
    {
        public int ActiveBrushCount { get; set; }
        public double AverageRenderTime { get; set; }
    }
}
