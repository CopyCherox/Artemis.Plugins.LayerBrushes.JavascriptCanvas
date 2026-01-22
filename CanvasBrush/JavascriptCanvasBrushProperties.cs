using Artemis.Core;
using Artemis.Plugins.LayerBrushes.JavascriptCanvas;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class JavascriptCanvasBrushProperties : LayerPropertyGroup
    {
        [PropertyDescription(Name = "Brightness", MinInputValue = 0, MaxInputValue = 100)]
        public IntLayerProperty Brightness { get; set; } = null!;

        [PropertyDescription(Description = "Update canvas every N frames (higher = better performance, lower = smoother)", InputAffix = "frames")]
        public IntLayerProperty UpdateEveryNFrames { get; set; } = null!;

        [PropertyDescription(Name = "Enable Audio Reactivity")]
        public BoolLayerProperty? EnableAudio { get; set; }


        private ObservableCollection<JavascriptScriptModel>? _scriptsCache;

        public LayerProperty<string> EnabledScriptName { get; set; } = null!;

        public event EventHandler? ScriptsRefreshed;

        public ObservableCollection<JavascriptScriptModel> Scripts
        {
            get
            {
                if (_scriptsCache == null)
                {
                    System.Diagnostics.Debug.WriteLine($"📂 Loading scripts for Properties {this.GetHashCode()}");
                    _scriptsCache = LoadScriptsFromFolder();
                }
                return _scriptsCache;
            }
        }

        protected override void PopulateDefaults()
        {
            Brightness.DefaultValue = 100;
            UpdateEveryNFrames.DefaultValue = 2;
            EnabledScriptName.DefaultValue = "Moving Rainbow Wave";
        }

        protected override void EnableProperties()
        {
            EnabledScriptName.IsHidden = true;
            UpdateEveryNFrames.IsHidden = true;

            ScriptsFolderManager.ScriptsChanged += OnScriptsChangedExternally;
        }

        protected override void DisableProperties()
        {
            ScriptsFolderManager.ScriptsChanged -= OnScriptsChangedExternally;
        }

        private void OnScriptsChangedExternally(object? sender, System.EventArgs e)
        {
            var myHashCode = this.GetHashCode();
            System.Diagnostics.Debug.WriteLine($"🔔 OnScriptsChangedExternally called on Properties {myHashCode} - clearing cache");

            // ✅ Always clear cache and refresh - no ignore mechanism
            _scriptsCache = null;
            RefreshScripts();
        }

        private ObservableCollection<JavascriptScriptModel> LoadScriptsFromFolder()
        {
            var scripts = ScriptsFolderManager.LoadAllScripts();

            var enabledName = EnabledScriptName.CurrentValue;
            if (!string.IsNullOrEmpty(enabledName))
            {
                var scriptToEnable = scripts.FirstOrDefault(s => s.ScriptName == enabledName);
                if (scriptToEnable != null)
                {
                    scriptToEnable.IsEnabled = true;
                }
                else if (scripts.Count > 0)
                {
                    scripts[0].IsEnabled = true;
                }
            }
            else if (scripts.Count > 0)
            {
                scripts[0].IsEnabled = true;
            }

            return scripts;
        }

        public void SaveScripts()
        {
            if (_scriptsCache != null)
            {
                System.Diagnostics.Debug.WriteLine($"💾 Properties {this.GetHashCode()} saving {_scriptsCache.Count} scripts...");

                foreach (var script in _scriptsCache)
                {
                    ScriptsFolderManager.SaveScriptToFile(script.ScriptName, script.JavaScriptCode);
                }

                var enabledScript = _scriptsCache.FirstOrDefault(s => s.IsEnabled);
                if (enabledScript != null)
                {
                    EnabledScriptName.SetCurrentValue(enabledScript.ScriptName);
                }

                System.Diagnostics.Debug.WriteLine($"💾 Saved {_scriptsCache.Count} scripts to folder");
            }
        }

        public void RefreshScripts()
        {
            var subscriberCount = ScriptsRefreshed?.GetInvocationList()?.Length ?? 0;
            System.Diagnostics.Debug.WriteLine($"🔄 RefreshScripts called on Properties {this.GetHashCode()} - {subscriberCount} subscriber(s)");

            ScriptsRefreshed?.Invoke(this, EventArgs.Empty);

            System.Diagnostics.Debug.WriteLine($"✅ ScriptsRefreshed event invoked");
        }

        public int GetScriptsRefreshedSubscriberCount()
        {
            return ScriptsRefreshed?.GetInvocationList()?.Length ?? 0;
        }
    }
}
