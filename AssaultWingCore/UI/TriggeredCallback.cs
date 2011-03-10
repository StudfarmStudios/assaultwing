using System;
using Microsoft.Xna.Framework.Input;
using AW2.Core;

namespace AW2.UI
{
    /// <summary>
    /// A callback that can be triggered by a control.
    /// </summary>
    /// Call <c>Update</c> regularly to check for the triggering condition.
    /// Some controls that are commonly used in triggered callbacks
    /// are provided by static methods.
    public class TriggeredCallback
    {
        #region Static members

        // These static controls are for public use through the public static methods.
        private static Control g_enterControl = new KeyboardKey(Keys.Enter);
        private static Control g_escapeControl = new KeyboardKey(Keys.Escape);
        private static Control g_yControl = new KeyboardKey(Keys.Y);
        private static Control g_nControl = new KeyboardKey(Keys.N);
        private static MultiControl g_proceedControl = new MultiControl();
        private static MultiControl g_yesControl = new MultiControl();
        private static MultiControl g_noControl = new MultiControl();

        /// <summary>
        /// Returns a new control that is handy for triggering the proceeding of a paused thing.
        /// The caller doesn't need to worry about Release()ing the returned control.
        /// </summary>
        public static Control GetProceedControl()
        {
            // At each call, we hand out the same copy of the control
            // and refresh the control according to the latest player controls.
            g_proceedControl.Clear();
            g_proceedControl.Add(g_enterControl);
            g_proceedControl.Add(g_escapeControl);
            foreach (var player in AssaultWingCore.Instance.DataEngine.Spectators)
                g_proceedControl.Add(player.Controls.Fire1);
            return g_proceedControl;
        }

        /// <summary>
        /// Returns a new control that is handy for a positive answer.
        /// The caller doesn't need to worry about Release()ing the returned control.
        /// </summary>
        public static Control GetYesControl()
        {
            // At each call, we hand out the same copy of the control.
            g_yesControl.Clear();
            g_yesControl.Add(g_yControl);
            return g_yesControl;
        }

        /// <summary>
        /// Returns a new control that is handy for a negative answer.
        /// The caller doesn't need to worry about Release()ing the returned control.
        /// </summary>
        public static Control GetNoControl()
        {
            // At each call, we hand out the same copy of the control.
            g_noControl.Clear();
            g_noControl.Add(g_nControl);
            return g_noControl;
        }

        #endregion Static members

        private Control _control;
        private Action _callback;

        public TriggeredCallback(Control control, Action callback)
        {
            if (control == null || callback == null)
                throw new ArgumentNullException("DialogAction got null arguments");
            _control = control;
            _callback = callback;
        }

        /// <summary>
        /// Updates the triggered callback, invoking the callback if the control triggers it.
        /// Returns true if the callback was invoked, or false otherwise.
        /// </summary>
        public bool Update()
        {
            if (!_control.Pulse) return false;
            _callback();
            return true;
        }
    }
}
