using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Views.Syntax
{
    public class JavaScriptColorizer : DocumentColorizingTransformer
    {
        private static readonly string[] Keywords =
        {
            "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch",
            "case", "break", "continue", "return", "new", "this", "class", "extends",
            "true", "false", "null", "undefined", "typeof", "instanceof", "in", "of",
            "try", "catch", "finally", "throw", "async", "await", "yield", "import",
            "export", "default", "from", "as", "debugger", "delete", "void", "with"
        };

        protected override void ColorizeLine(DocumentLine line)
        {
            var lineText = CurrentContext.Document.GetText(line);
            int lineStartOffset = line.Offset;

            ColorizeKeywords(lineText, lineStartOffset);
            ColorizeStrings(lineText, lineStartOffset);
            ColorizeComments(lineText, lineStartOffset);
            ColorizeNumbers(lineText, lineStartOffset);
        }

        private void ColorizeKeywords(string lineText, int lineStartOffset)
        {
            foreach (var keyword in Keywords)
            {
                int index = 0;
                while ((index = lineText.IndexOf(keyword, index)) >= 0)
                {
                    bool isWholeWord = (index == 0 || !char.IsLetterOrDigit(lineText[index - 1])) &&
                        (index + keyword.Length >= lineText.Length || !char.IsLetterOrDigit(lineText[index + keyword.Length]));

                    if (isWholeWord)
                    {
                        ChangeLinePart(
                            lineStartOffset + index,
                            lineStartOffset + index + keyword.Length,
                            (VisualLineElement element) =>
                            {
                                element.TextRunProperties.SetForegroundBrush(
                                    new SolidColorBrush(Color.Parse("#569CD6")));
                            });
                    }
                    index += keyword.Length;
                }
            }
        }

        private void ColorizeStrings(string lineText, int lineStartOffset)
        {
            for (int i = 0; i < lineText.Length; i++)
            {
                if (lineText[i] == '"' || lineText[i] == '\'' || lineText[i] == '`')
                {
                    char quote = lineText[i];
                    int start = i;
                    i++;
                    while (i < lineText.Length && lineText[i] != quote)
                    {
                        if (lineText[i] == '\\' && i + 1 < lineText.Length)
                            i++;
                        i++;
                    }
                    if (i < lineText.Length)
                    {
                        ChangeLinePart(
                            lineStartOffset + start,
                            lineStartOffset + i + 1,
                            (VisualLineElement element) =>
                            {
                                element.TextRunProperties.SetForegroundBrush(
                                    new SolidColorBrush(Color.Parse("#CE9178")));
                            });
                    }
                }
            }
        }

        private void ColorizeComments(string lineText, int lineStartOffset)
        {
            int commentIndex = lineText.IndexOf("//");
            if (commentIndex >= 0)
            {
                ChangeLinePart(
                    lineStartOffset + commentIndex,
                    lineStartOffset + lineText.Length,
                    (VisualLineElement element) =>
                    {
                        element.TextRunProperties.SetForegroundBrush(
                            new SolidColorBrush(Color.Parse("#6A9955")));
                    });
            }
        }

        private void ColorizeNumbers(string lineText, int lineStartOffset)
        {
            for (int i = 0; i < lineText.Length; i++)
            {
                if (char.IsDigit(lineText[i]))
                {
                    int start = i;
                    while (i < lineText.Length && (char.IsDigit(lineText[i]) || lineText[i] == '.'))
                        i++;
                    ChangeLinePart(
                        lineStartOffset + start,
                        lineStartOffset + i,
                        (VisualLineElement element) =>
                        {
                            element.TextRunProperties.SetForegroundBrush(
                                new SolidColorBrush(Color.Parse("#B5CEA8")));
                        });
                }
            }
        }
    }
}
