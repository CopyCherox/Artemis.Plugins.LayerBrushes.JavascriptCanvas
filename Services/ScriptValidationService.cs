using System.Collections.ObjectModel;
using System.Linq;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services
{
    public class ScriptValidationService
    {
        private readonly ObservableCollection<JavascriptScriptModel> _scripts;

        public ScriptValidationService(ObservableCollection<JavascriptScriptModel> scripts)
        {
            _scripts = scripts;
        }

        public ValidationResult ValidateScriptName(string scriptName, JavascriptScriptModel? currentScript = null)
        {
            // Check for empty or whitespace
            if (string.IsNullOrWhiteSpace(scriptName))
            {
                return new ValidationResult(false, "Script name cannot be empty.");
            }

            // Check for duplicate names (excluding current script)
            var isDuplicate = _scripts.Any(s => s != currentScript && s.ScriptName == scriptName);
            if (isDuplicate)
            {
                return new ValidationResult(false, $"A script named '{scriptName}' already exists.");
            }

            // Check for invalid characters
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (scriptName.IndexOfAny(invalidChars) >= 0)
            {
                return new ValidationResult(false, "Script name contains invalid characters.");
            }

            // Check length
            if (scriptName.Length > 200)
            {
                return new ValidationResult(false, "Script name is too long (max 200 characters).");
            }

            return new ValidationResult(true, string.Empty);
        }

        public string EnsureUniqueScriptName(string baseName)
        {
            if (!_scripts.Any(s => s.ScriptName == baseName))
                return baseName;

            int counter = 1;
            string newName;
            do
            {
                newName = $"{baseName} ({counter})";
                counter++;
            }
            while (_scripts.Any(s => s.ScriptName == newName));

            return newName;
        }

        public bool IsScriptNameAvailable(string scriptName, JavascriptScriptModel? excludeScript = null)
        {
            return !_scripts.Any(s => s != excludeScript && s.ScriptName == scriptName);
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        public ValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new ValidationResult(true, string.Empty);
        public static ValidationResult Failure(string errorMessage) => new ValidationResult(false, errorMessage);
    }
}
