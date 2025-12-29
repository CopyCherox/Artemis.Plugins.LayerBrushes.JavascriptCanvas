using Artemis.Core;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas;
using System.Collections.ObjectModel;
using System.Linq;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class JavascriptCanvasBrushProperties : LayerPropertyGroup
    {
        // Store scripts as a string (JSON serialized)
        public LayerProperty<string> ScriptsJson { get; set; } = null!;

        // Version tracking - increment this when you add new default scripts
        public IntLayerProperty ScriptsVersion { get; set; } = null!;

        [PropertyDescription(Name = "Brightness", MinInputValue = 0, MaxInputValue = 100)]
        public IntLayerProperty Brightness { get; set; } = null!;

        // Runtime collection (not saved directly)
        private ObservableCollection<JavascriptScriptModel>? _scriptsCache;

        [PropertyDescription(Description = "Update canvas every N frames (higher = better performance, lower = smoother)", InputAffix = "frames")]
        public IntLayerProperty UpdateEveryNFrames { get; set; } = null!;

        // UPDATE THIS NUMBER when you add new default scripts
        private const int CURRENT_SCRIPTS_VERSION = 2;

        public ObservableCollection<JavascriptScriptModel> Scripts
        {
            get
            {
                if (_scriptsCache == null)
                {
                    _scriptsCache = DeserializeScripts();
                }
                return _scriptsCache;
            }
        }

        protected override void PopulateDefaults()
        {
            Brightness.DefaultValue = 100;
            ScriptsVersion.DefaultValue = 0;
            UpdateEveryNFrames.DefaultValue = 2;

            // Initialize with default scripts JSON or update if version changed
            if (string.IsNullOrEmpty(ScriptsJson.DefaultValue) || ScriptsVersion.CurrentValue < CURRENT_SCRIPTS_VERSION)
            {
                var defaultScripts = new ObservableCollection<JavascriptScriptModel>
                {
                    new JavascriptScriptModel
                    {
                        ScriptName = "Moving Rainbow Wave",
                        JavaScriptCode  = @"// Moving rainbow wave
for (let x = 0; x < width; x++) {
    let hue = (x / width + time * 0.5) % 1.0;
    let rgb = ctx.hslToRgb(hue, 1.0, 0.5);
    ctx.fillStyle(rgb.r, rgb.g, rgb.b);
    ctx.fillRect(x, 0, 1, height);
}",
                        IsEnabled = true,
                        IsGlobal = false  // ✅ Default scripts are not global
                    },
                    new JavascriptScriptModel
                    {
                        ScriptName = "Breathing Pulse",
                        JavaScriptCode  = @"// Breathing pulse effect
let brightness = (Math.sin(time * 2) + 1) / 2;
let color = brightness * 255;
ctx.clear(color * 1, color * 0.4, 0);",
                        IsEnabled = false,
                        IsGlobal = false
                    },
                    new JavascriptScriptModel
                    {
                        ScriptName = "Moving Gradient",
                        JavaScriptCode  = @"// Moving gradient
for (let x = 0; x < width; x++) {
    let pos = (x / width + time * 0.3) % 1.0;
    let r = Math.floor(255 * pos);
    let g = Math.floor(128 * (1 - pos));
    let b = Math.floor(200 * Math.sin(pos * Math.PI));
    ctx.fillStyle(r, g, b);
    ctx.fillRect(x, 0, 1, height);
}",
                        IsEnabled = false,
                        IsGlobal = false
                    },
                    new JavascriptScriptModel
                    {
                        ScriptName = "Fire Effect",
                        JavaScriptCode  = @"// Fire effect
for (let x = 0; x < width; x++) {
    for (let y = 0; y < height; y++) {
        let yPos = y / height;
        let noise = Math.sin(x * 0.1 + time * 3) * 0.5 + 0.5;
        let intensity = (1 - yPos) * noise;
        let r = Math.floor(255 * intensity);
        let g = Math.floor(100 * intensity * 0.5);
        let b = 0;
        ctx.fillStyle(r, g, b);
        ctx.fillRect(x, y, 1, 1);
    }
}",
                        IsEnabled = false,
                        IsGlobal = false
                    },
                    new JavascriptScriptModel
                    {
                        ScriptName = "Scan Line",
                        JavaScriptCode  = @"// Moving scan line
ctx.clear(0, 0, 50);
let pos = (time * 0.5) % 1.0;
let x = Math.floor(pos * width);
ctx.fillStyle(0, 255, 255);
ctx.fillRect(x - 2, 0, 5, height);",
                        IsEnabled = false,
                        IsGlobal = false
                    }
                };

                // ✅ Merge with global scripts
                var globalScripts = GlobalScriptsManager.LoadGlobalScripts();
                foreach (var globalScript in globalScripts)
                {
                    // Check if global script already exists (by name)
                    if (!defaultScripts.Any(s => s.ScriptName == globalScript.ScriptName))
                    {
                        defaultScripts.Add(globalScript);
                    }
                }

                ScriptsJson.DefaultValue = SerializeScripts(defaultScripts);
                ScriptsVersion.SetCurrentValue(CURRENT_SCRIPTS_VERSION);

                // Force reload the cache
                _scriptsCache = null;
            }
        }

        protected override void EnableProperties()
        {
            ScriptsJson.IsHidden = true; // Hide the JSON property from UI
            ScriptsVersion.IsHidden = true; // Hide version from UI
            UpdateEveryNFrames.CurrentValue = 2;
            UpdateEveryNFrames.IsHidden = true;
        }

        protected override void DisableProperties()
        {
        }

        public void SaveScripts()
        {
            if (_scriptsCache != null)
            {
                ScriptsJson.SetCurrentValue(SerializeScripts(_scriptsCache));

                // ✅ Save global scripts separately
                GlobalScriptsManager.SaveGlobalScripts(_scriptsCache);
            }
        }

        private string SerializeScripts(ObservableCollection<JavascriptScriptModel> scripts)
        {
            return System.Text.Json.JsonSerializer.Serialize(scripts);
        }

        private ObservableCollection<JavascriptScriptModel> DeserializeScripts()
        {
            ObservableCollection<JavascriptScriptModel> scripts;

            try
            {
                var json = ScriptsJson.CurrentValue;
                if (!string.IsNullOrEmpty(json))
                {
                    scripts = System.Text.Json.JsonSerializer.Deserialize<ObservableCollection<JavascriptScriptModel>>(json) ?? new ObservableCollection<JavascriptScriptModel>();

                    if (scripts == null)
                    {
                        scripts = new ObservableCollection<JavascriptScriptModel>();
                    }
                }
                else
                {
                    scripts = new ObservableCollection<JavascriptScriptModel>();
                }
            }
            catch
            {
                scripts = new ObservableCollection<JavascriptScriptModel>();
            }

            // ✅ ALWAYS merge global scripts, even for existing brushes
            var globalScripts = GlobalScriptsManager.LoadGlobalScripts();
            foreach (var globalScript in globalScripts)
            {
                // Check if this global script already exists in the local collection
                var existingScript = scripts.FirstOrDefault(s =>
                    s.ScriptName == globalScript.ScriptName && s.IsGlobal);

                if (existingScript != null)
                {
                    // Update existing global script with latest version
                    existingScript.JavaScriptCode = globalScript.JavaScriptCode;
                    existingScript.IsGlobal = true;
                }
                else
                {
                    // Add new global script
                    scripts.Add(new JavascriptScriptModel
                    {
                        ScriptName = globalScript.ScriptName,
                        JavaScriptCode = globalScript.JavaScriptCode,
                        IsEnabled = false,
                        IsGlobal = true
                    });
                }
            }

            // ✅ Remove any scripts marked as global that no longer exist in global storage
            var scriptsToRemove = scripts.Where(s =>
                s.IsGlobal && !globalScripts.Any(g => g.ScriptName == s.ScriptName)).ToList();

            foreach (var scriptToRemove in scriptsToRemove)
            {
                scripts.Remove(scriptToRemove);
            }

            return scripts;
        }

        public void RefreshScripts()
        {
            _scriptsCache = null;
            // Trigger property change if needed
            OnPropertyChanged(nameof(Scripts));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            // This triggers UI updates
        }


    }



}