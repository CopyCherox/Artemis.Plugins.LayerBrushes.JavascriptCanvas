using ReactiveUI;
using System.Linq;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.ViewModels
{
    public class CodeEditorViewModel : ReactiveObject
    {
        private string _currentEditorCode = string.Empty;
        private string _savedEditorCode = string.Empty;
        private bool _hasUnsavedChanges = false;

        public CodeEditorViewModel()
        {
        }

        public string EditorCode
        {
            get => _currentEditorCode;
            set
            {
                if (_currentEditorCode != value)
                {
                    _currentEditorCode = value;
                    this.RaisePropertyChanged();
                    UpdateUnsavedChangesStatus();
                }
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
        }

        public string SavedEditorCode
        {
            get => _savedEditorCode;
            private set => _savedEditorCode = value;
        }

        public void LoadCode(string code)
        {
            _currentEditorCode = code;
            _savedEditorCode = code;
            this.RaisePropertyChanged(nameof(EditorCode));
            HasUnsavedChanges = false;
        }

        public void MarkAsSaved()
        {
            _savedEditorCode = _currentEditorCode;
            HasUnsavedChanges = false;
        }

        public void ResetToSaved()
        {
            if (_currentEditorCode != _savedEditorCode)
            {
                _currentEditorCode = _savedEditorCode;
                this.RaisePropertyChanged(nameof(EditorCode));
                HasUnsavedChanges = false;
            }
        }

        private void UpdateUnsavedChangesStatus()
        {
            HasUnsavedChanges = _currentEditorCode != _savedEditorCode;
        }

        public bool HasChanges()
        {
            return _currentEditorCode != _savedEditorCode;
        }

        public void UpdateCurrentCode(string code)
        {
            _currentEditorCode = code;
            this.RaisePropertyChanged(nameof(EditorCode));
            UpdateUnsavedChangesStatus();
        }
    }
}
