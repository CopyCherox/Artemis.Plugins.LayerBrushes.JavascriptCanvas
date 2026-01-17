using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas
{
    public class JavascriptScriptModel : INotifyPropertyChanged
    {
        private string _scriptName = string.Empty;
        private string _javaScriptCode = string.Empty;
        private bool _isEnabled;

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

        public string JavaScriptCode
        {
            get => _javaScriptCode;
            set
            {
                if (_javaScriptCode != value)
                {
                    _javaScriptCode = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
