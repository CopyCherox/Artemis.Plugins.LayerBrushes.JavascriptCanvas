using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.ViewModels
{
    public class ScriptSelectorViewModel : ReactiveObject
    {
        private readonly JavascriptCanvasBrush _brush;
        private ObservableCollection<JavascriptScriptModel> _scripts;
        private JavascriptScriptModel? _selectedScript;

        public ScriptSelectorViewModel(JavascriptCanvasBrush brush)
        {
            _brush = brush;
            _scripts = brush.Properties.Scripts;

            // Subscribe to script collection changes
            _brush.Properties.ScriptsRefreshed += OnScriptsRefreshed;

            // Select the enabled script or the first one
            _selectedScript = _scripts.FirstOrDefault(s => s.IsEnabled) ?? _scripts.FirstOrDefault();

            // Subscribe to property changes on all scripts
            foreach (var script in _scripts)
            {
                script.PropertyChanged += OnScriptPropertyChanged;
            }
        }

        public ObservableCollection<JavascriptScriptModel> Scripts
        {
            get => _scripts;
            private set => this.RaiseAndSetIfChanged(ref _scripts, value);
        }

        public JavascriptScriptModel? SelectedScript
        {
            get => _selectedScript;
            set
            {
                if (_selectedScript != value)
                {
                    _selectedScript = value;
                    this.RaisePropertyChanged();
                    OnSelectedScriptChanged();
                }
            }
        }

        public event EventHandler<JavascriptScriptModel?>? SelectedScriptChanged;

        private void OnSelectedScriptChanged()
        {
            SelectedScriptChanged?.Invoke(this, _selectedScript);
        }

        public void AddScript(JavascriptScriptModel script)
        {
            script.PropertyChanged += OnScriptPropertyChanged;
            _scripts.Add(script);
            this.RaisePropertyChanged(nameof(Scripts));
        }

        public void RemoveScript(JavascriptScriptModel script)
        {
            script.PropertyChanged -= OnScriptPropertyChanged;
            _scripts.Remove(script);
            this.RaisePropertyChanged(nameof(Scripts));
        }

        public void SelectScriptByName(string scriptName)
        {
            var script = _scripts.FirstOrDefault(s => s.ScriptName == scriptName);
            if (script != null)
            {
                SelectedScript = script;
            }
        }

        public void SelectNextScript()
        {
            if (_selectedScript == null || _scripts.Count == 0) return;

            int currentIndex = _scripts.IndexOf(_selectedScript);
            int nextIndex = (currentIndex + 1) % _scripts.Count;
            SelectedScript = _scripts[nextIndex];
        }

        public void SelectPreviousScript()
        {
            if (_selectedScript == null || _scripts.Count == 0) return;

            int currentIndex = _scripts.IndexOf(_selectedScript);
            int previousIndex = currentIndex - 1;
            if (previousIndex < 0) previousIndex = _scripts.Count - 1;
            SelectedScript = _scripts[previousIndex];
        }

        public void EnableScript(JavascriptScriptModel script)
        {
            foreach (var s in _scripts)
            {
                s.IsEnabled = (s == script);
            }
        }

        private void OnScriptsRefreshed(object? sender, EventArgs e)
        {
            var previouslySelectedName = _selectedScript?.ScriptName;

            // Unsubscribe from old scripts
            foreach (var script in _scripts)
            {
                script.PropertyChanged -= OnScriptPropertyChanged;
            }

            // Reload scripts
            Scripts = _brush.Properties.Scripts;

            // Subscribe to new scripts
            foreach (var script in _scripts)
            {
                script.PropertyChanged += OnScriptPropertyChanged;
            }

            // Restore selection by name
            if (!string.IsNullOrEmpty(previouslySelectedName))
            {
                SelectedScript = _scripts.FirstOrDefault(s => s.ScriptName == previouslySelectedName)
                    ?? _scripts.FirstOrDefault(s => s.IsEnabled)
                    ?? _scripts.FirstOrDefault();
            }
            else
            {
                SelectedScript = _scripts.FirstOrDefault(s => s.IsEnabled) ?? _scripts.FirstOrDefault();
            }
        }

        private void OnScriptPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Propagate property changes if needed
            if (sender == _selectedScript)
            {
                this.RaisePropertyChanged(nameof(SelectedScript));
            }
        }

        public void Dispose()
        {
            _brush.Properties.ScriptsRefreshed -= OnScriptsRefreshed;

            foreach (var script in _scripts)
            {
                script.PropertyChanged -= OnScriptPropertyChanged;
            }
        }
    }
}
