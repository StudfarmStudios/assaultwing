using System;
using Microsoft.Xna.Framework.Input;

namespace AW2.UI
{
    /// <summary>
    /// A callback that can be triggered by a control.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Update"/> regularly to check for the triggering condition.
    /// Some controls that are commonly used in triggered callbacks
    /// are provided by static methods.
    /// </remarks>
    public class TriggeredCallback
    {
        public static readonly Control PROCEED_CONTROL = new MultiControl
        {
            new KeyboardKey(Keys.Enter),
            new KeyboardKey(Keys.Escape),
            new GamePadButton(0, GamePadButtonType.A),
            new GamePadButton(0, GamePadButtonType.B),
        };
        public static readonly Control YES_CONTROL = new MultiControl
        {
            new KeyboardKey(Keys.Y),
            new GamePadButton(0, GamePadButtonType.A),
        };
        public static readonly Control NO_CONTROL = new MultiControl
        {
            new KeyboardKey(Keys.N),
            new GamePadButton(0, GamePadButtonType.B),
        };

        public Control Control { get; private set; }
        private Action _callback;

        public TriggeredCallback(Control control, Action callback)
        {
            if (control == null) throw new ArgumentNullException("control");
            if (callback == null) throw new ArgumentNullException("callback");
            Control = control;
            _callback = callback;
        }

        /// <summary>
        /// Updates the triggered callback, invoking the callback if the control triggers it.
        /// Returns true if the callback was invoked, or false otherwise.
        /// </summary>
        public bool Update()
        {
            if (!Control.Pulse) return false;
            _callback();
            return true;
        }
    }
}
