using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace AW2.UI
{
    /// <summary>
    /// A key on the keyboard.
    /// </summary>
    class KeyboardKey : Control
    {
        private Keys key;
        private bool pulse;
        private float force;

        /// <summary>
        /// Creates a control from a keyboard key.
        /// </summary>
        /// <param name="key">The key.</param>
        public KeyboardKey(Keys key)
            : base()
        {
            this.key = key;
            pulse = false;
            force = 0;
        }

        public override void SetState(ref InputState oldState, ref InputState newState)
        {
            pulse = newState.keyboard[key] == KeyState.Down && oldState.keyboard[key] == KeyState.Up;
            force = newState.keyboard[key] == KeyState.Down ? 1f : 0f;
        }

        /// <summary>
        /// Returns if the control gave a pulse.
        /// </summary>
        public override bool Pulse { get { return pulse; } }

        /// <summary>
        /// Returns the amount of control force; a float between 0 and 1.
        /// </summary>
        public override float Force { get { return force; } }
    }
}