using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using AW2.Core;

namespace AW2.UI
{
    /// <summary>
    /// A short piece of text that is editable by the user. 
    /// Almost like a text field but with no graphical output.
    /// </summary>
    public class EditableText
    {
        [Flags]
        public enum Keysets
        {
            None = 0x00,
            Numbers = 0x01,
            Letters = 0x02,
            Space = 0x04,
            Dot = 0x08,
            All = Numbers | Letters | Space | Dot,
            PlayerNameSet = Letters | Space,
            IPAddressSet = Numbers | Dot,
        };

        private static readonly TimeSpan REPEAT_DELAY = TimeSpan.FromSeconds(0.4);
        private static readonly TimeSpan REPEAT_INTERVAL = TimeSpan.FromSeconds(0.05);

        /// <summary>
        /// Last key pressed, or <c>null</c> if
        /// no key pressed yet or the pressed key has been released.
        /// </summary>
        private Keys? _lastPressedKey;

        private TimeSpan _nextKeyRepeat;

        public Keysets AllowedKeysets { get; set; }
        public int MaxLength { get; set; }

        /// <summary>
        /// Position of the caret in the text field, as a zero-based index from
        /// the beginning of the text.
        /// </summary>
        public int CaretPosition { get; private set; }

        /// <summary>
        /// The current text content.
        /// </summary>
        public string Content { get; private set; }

        private bool TimeToRepeatKey { get { return _nextKeyRepeat != TimeSpan.Zero && _nextKeyRepeat <= AssaultWingCore.Instance.GameTime.TotalRealTime; } }

        public EditableText(string content, int maxLength, Keysets allowedKeysets)
        {
            if (maxLength < 0) throw new ArgumentException("Maximum length cannot be negative");
            if (content.Length > maxLength) throw new ArgumentException("Initial content is longer than maximum length");
            AllowedKeysets = allowedKeysets;
            MaxLength = maxLength;
            Content = content;
            CaretPosition = content.Length;
        }

        /// <summary>
        /// Updates the text according to user input.
        /// </summary>
        /// <param name="changedCallback">Action to perform if 
        /// the text contents were changed.</param>
        public void Update(Action changedCallback)
        {
            var state = Keyboard.GetState();

            // If a key has been pressed, do nothing until it is released.
            IEnumerable<Keys> pressedKeys = null;
            if (!_lastPressedKey.HasValue)
            {
                _nextKeyRepeat = AssaultWingCore.Instance.GameTime.TotalRealTime + REPEAT_DELAY;
                pressedKeys = state.GetPressedKeys();
            }
            else
            {
                if (state.IsKeyUp(_lastPressedKey.Value))
                    _lastPressedKey = null;
                else if (TimeToRepeatKey)
                {
                    _nextKeyRepeat = AssaultWingCore.Instance.GameTime.TotalRealTime + REPEAT_INTERVAL;
                    pressedKeys = new Keys[] { _lastPressedKey.Value };
                }
            }

            if (pressedKeys != null)
            {
                foreach (var key in pressedKeys) InterpretKey(key);
                changedCallback();
            }
        }

        public void Clear()
        {
            Content = "";
            CaretPosition = 0;
        }

        private void InterpretKey(Keys key)
        {
            switch (key)
            {
                case Keys.Left: --CaretPosition; break;
                case Keys.Right: ++CaretPosition; break;
                case Keys.Home: CaretPosition = 0; break;
                case Keys.End: CaretPosition = Content.Length; break;
                case Keys.Back:
                    if (CaretPosition > 0)
                    {
                        --CaretPosition;
                        Content = Content.Remove(CaretPosition, 1);
                    }
                    break;
                case Keys.Delete:
                    if (CaretPosition < Content.Length)
                        Content = Content.Remove(CaretPosition, 1);
                    break;
                default:
                    InterpretTextInput(key);
                    break;
            }
            CaretPosition = Math.Min(CaretPosition, Content.Length);
            CaretPosition = Math.Max(CaretPosition, 0);
            _lastPressedKey = key;
        }

        private void InterpretTextInput(Keys key)
        {
            char? chr = null;
            if ((AllowedKeysets & Keysets.Numbers) != 0)
            {
                if (key >= Keys.D0 && key <= Keys.D9)
                    chr = (char)('0' + key - Keys.D0);
            }
            if ((AllowedKeysets & Keysets.Letters) != 0)
            {
                if (key >= Keys.A && key <= Keys.Z)
                    chr = (char)('a' + key - Keys.A);
            }
            if ((AllowedKeysets & Keysets.Space) != 0)
            {
                if (key == Keys.Space) chr = ' ';
            }
            if ((AllowedKeysets & Keysets.Dot) != 0)
            {
                if (key == Keys.OemPeriod) chr = '.';
            }
            if (chr.HasValue && Content.Length < MaxLength)
            {
                Content = Content.Insert(CaretPosition, chr.Value.ToString());
                ++CaretPosition;
            }
        }
    }
}
