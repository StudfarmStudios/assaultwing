using System;
using Microsoft.Xna.Framework.Input;

namespace AW2.UI
{
    /// <summary>
    /// A key on the keyboard.
    /// </summary>
    public class KeyboardKey : Control
    {
        private Keys _key;

        public override bool Pulse { get { return NewState.Keyboard[_key] == KeyState.Down && OldState.Keyboard[_key] == KeyState.Up; } }
        public override float Force { get { return NewState.Keyboard[_key] == KeyState.Down ? 1f : 0f; } }

        /// <summary>
        /// Creates a control from a keyboard key.
        /// </summary>
        public KeyboardKey(Keys key)
        {
            _key = key;
        }
    }
}
