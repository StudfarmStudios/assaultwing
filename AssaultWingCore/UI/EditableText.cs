using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Helpers;

namespace AW2.UI
{
    /// <summary>
    /// A short piece of text that is editable by the user. 
    /// Almost like a text field but with no graphical output.
    /// </summary>
    public class EditableText : IDisposable
    {
        private AssaultWingCore _game;
        private Action _changedCallback;
        private StringBuilder _content;
        private TimeSpan _temporaryActivationTimeout;
        private CharacterSet _allowedChars;

        public int MaxLength { get; set; }

        /// <summary>
        /// The current text content.
        /// </summary>
        public string Content { get { return _content.ToString(); } set { _content.Clear(); _content.Append(value); } }

        /// <summary>
        /// Are keypresses handled or not.
        /// </summary>
        public bool IsActive { get; set; }

        private static TimeSpan TemporaryActivationInterval { get { return AssaultWingCore.TargetElapsedTime + TimeSpan.FromMilliseconds(1); } }

        public EditableText(string content, int maxLength, CharacterSet allowedChars, AssaultWingCore game, Action changedCallback)
        {
            if (maxLength < 0) throw new ArgumentException("Maximum length cannot be negative");
            if (content.Length > maxLength) throw new ArgumentException("Initial content is longer than maximum length");
            MaxLength = maxLength;
            _allowedChars = allowedChars;
            _content = new StringBuilder(content, maxLength);
            _game = game;
            _changedCallback = changedCallback;
            WeakEventHandler<char>.Register(game.Window,
                RegisterKeyPressHandler,
                UnRegisterKeyPressHandler,
                this,
                KeyPressHandler);
        }

        private static void RegisterKeyPressHandler(Window window, Action<Object, char>  handler) {
            window.KeyPress += handler;
        }

        private static void UnRegisterKeyPressHandler(Window window, Action<Object, char> handler) {
            window.KeyPress -= handler;
        }

        private static void KeyPressHandler(EditableText self, object sender, char arg) {
            self.KeyPressHandler(sender, arg);
        }

        /// <summary>
        /// Override <see cref="IsActive"/> for a short while and be active.
        /// Works as if IsActive is set now to true and a bit later back to its actual value.
        /// </summary>
        public void ActivateTemporarily()
        {
            _temporaryActivationTimeout = _game.GameTime.TotalRealTime + TemporaryActivationInterval;
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
                case (char)Keys.Back:
                    if (_content.Length > 0)
                        _content.Remove(_content.Length - 1, 1);
                    break;
                default:
                    if (_content.Length < MaxLength && !char.IsControl(keyChar) && _allowedChars.Contains(keyChar))
                        _content.Append(keyChar);
                    break;
            }
        }

        private void KeyPressHandler(object sender, char keyChar)
        {
            if (!IsActive && _temporaryActivationTimeout <= _game.GameTime.TotalRealTime) return;
            InterpretKey(keyChar);
            _changedCallback();
        }
    }
}
