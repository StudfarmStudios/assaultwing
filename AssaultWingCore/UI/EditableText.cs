using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Input;
using AW2.Core;

namespace AW2.UI
{
    /// <summary>
    /// A short piece of text that is editable by the user. 
    /// Almost like a text field but with no graphical output.
    /// </summary>
    public class EditableText : IDisposable
    {
        private static readonly TimeSpan TEMPORARY_ACTIVATION_INTERVAL = TimeSpan.FromSeconds(0.5);

        private AssaultWingCore _game;
        private Action _changedCallback;
        private StringBuilder _content;
        private TimeSpan _temporaryActivationTimeout;

        public int MaxLength { get; set; }

        /// <summary>
        /// The current text content.
        /// </summary>
        public string Content { get { return _content.ToString(); } }

        /// <summary>
        /// Are keypresses handled or not.
        /// </summary>
        public bool IsActive { get; set; }

        public EditableText(string content, int maxLength, AssaultWingCore game, Action changedCallback)
        {
            if (maxLength < 0) throw new ArgumentException("Maximum length cannot be negative");
            if (content.Length > maxLength) throw new ArgumentException("Initial content is longer than maximum length");
            MaxLength = maxLength;
            _content = new StringBuilder(content, maxLength);
            _game = game;
            _changedCallback = changedCallback;
            game.Window.KeyPress += KeyPressHandler; // FIXME !!! leaks memory if Dispose() is not called later
        }

        /// <summary>
        /// Override <see cref="IsActive"/> for a short while and be active.
        /// Works as if IsActive is set now to true and a bit later back to its actual value.
        /// </summary>
        public void ActivateTemporarily()
        {
            _temporaryActivationTimeout = _game.GameTime.TotalRealTime + TEMPORARY_ACTIVATION_INTERVAL;
        }

        public void Clear()
        {
            _content.Clear();
        }

        public void Dispose()
        {
            IsActive = false;
            _game.Window.KeyPress -= KeyPressHandler;
        }

        private void InterpretKey(char keyChar)
        {
            switch (keyChar)
            {
                case (char)8: // backspace
                    if (_content.Length > 0)
                        _content.Remove(_content.Length - 1, 1);
                    break;
                default:
                    // Only append chars that exist in our fonts.
                    if (_content.Length < MaxLength && keyChar >= 32 && keyChar <= 126)
                        _content.Append(keyChar);
                    break;
            }
        }

        private void KeyPressHandler(char keyChar)
        {
            if (!IsActive && _temporaryActivationTimeout <= _game.GameTime.TotalRealTime) return;
            InterpretKey(keyChar);
            _changedCallback();
        }
    }
}
