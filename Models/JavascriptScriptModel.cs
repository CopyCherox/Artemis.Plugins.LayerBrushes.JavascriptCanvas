using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    [DataContract]
    public class JavascriptScriptModel : INotifyPropertyChanged
    {
        private string _scriptName = "";
        private string _javascriptCode = "";
        private bool _isEnabled = false;
        private bool _isGlobal = false;  // ✅ NEW

        [DataMember]
        public string ScriptName
        {
            get => _scriptName;
            set
            {
                if (_scriptName != value)
                {
                    _scriptName = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataMember]
        public string JavaScriptCode
        {
            get => _javascriptCode;
            set
            {
                if (_javascriptCode != value)
                {
                    _javascriptCode = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataMember]
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        // ✅ NEW: Global script flag
        [DataMember]
        public bool IsGlobal
        {
            get => _isGlobal;
            set
            {
                if (_isGlobal != value)
                {
                    _isGlobal = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}