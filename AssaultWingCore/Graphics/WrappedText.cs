using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Graphics
{
    /// <summary>
    /// A piece of text that wraps nicely into lines of desired width.
    /// </summary>
    public class WrappedText
    {
        private string _text;
        private Func<string, float> _getStringWidth;
        private float _enWidth;

        public WrappedText(string text, Func<string, float> getStringWidth)
        {
            _text = text;
            _getStringWidth = getStringWidth;
            _enWidth = getStringWidth("N");
        }

        public string[] WrapToWidth(float width)
        {
            var lines = new List<string>();
            for (int startIndex = GetNextWordStart(-1); startIndex < _text.Length; )
            {
                var endIndex = GetPreviousWordEnd(startIndex, startIndex + (int)(width / _enWidth)); // exclusive index
                if (endIndex == startIndex) endIndex = GetNextWordEnd(endIndex);
                var line = _text.Substring(startIndex, endIndex - startIndex);
                var guessOverflow = _getStringWidth(line) - width;
                while (endIndex < _text.Length)
                {
                    var newEndIndex = guessOverflow > 0
                        ? GetPreviousWordEnd(startIndex, endIndex)
                        : GetNextWordEnd(endIndex);
                    if (newEndIndex == startIndex) break;
                    var newLine = _text.Substring(startIndex, newEndIndex - startIndex);
                    var newGuessOverflow = _getStringWidth(newLine) - width;
                    if (guessOverflow <= 0 && newGuessOverflow > 0) break;
                    line = newLine;
                    endIndex = newEndIndex;
                    guessOverflow = newGuessOverflow;
                    if (guessOverflow > 0 && newGuessOverflow <= 0) break;
                }
                lines.Add(line);
                startIndex = GetNextWordStart(endIndex);
            }
            return lines.ToArray();
        }

        private int GetPreviousWordEnd(int startIndex, int i)
        {
            i = Math.Min(i, _text.Length);
            while (i > startIndex && !char.IsWhiteSpace(_text[i - 1])) i--;
            while (i > startIndex && char.IsWhiteSpace(_text[i - 1])) i--;
            return i;
        }

        private int GetNextWordEnd(int i)
        {
            while (i < _text.Length && char.IsWhiteSpace(_text[i])) i++;
            while (i < _text.Length && !char.IsWhiteSpace(_text[i])) i++;
            return i;
        }

        private int GetNextWordStart(int i)
        {
            if (i < 0)
                i = 0;
            else
                while (i < _text.Length && !char.IsWhiteSpace(_text[i])) i++;
            while (i < _text.Length && char.IsWhiteSpace(_text[i])) i++;
            return i;
        }
    }
}
