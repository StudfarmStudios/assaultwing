using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Input;
using AW2.Game;

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
        // These are never Release()d.
        static Control enterControl = new KeyboardKey(Keys.Enter);
        static Control escapeControl = new KeyboardKey(Keys.Escape);
        static Control yControl = new KeyboardKey(Keys.Y);
        static Control nControl = new KeyboardKey(Keys.N);
        static MultiControl proceedControl = new MultiControl();
        static MultiControl yesControl = new MultiControl();
        static MultiControl noControl = new MultiControl();

        /// <summary>
        /// Returns a new control that is handy for triggering the proceeding of a paused thing.
        /// </summary>
        /// The caller doesn't need to worry about Release()ing the returned control.
        public static Control GetProceedControl()
        {
            // At each call, we hand out the same copy of the control
            // and refresh the control according to the latest player controls.
            proceedControl.Clear();
            proceedControl.Add(enterControl);
            proceedControl.Add(escapeControl);
            foreach (var player in AssaultWing.Instance.DataEngine.Spectators)
                proceedControl.Add(player.Controls.fire1);
            return proceedControl;
        }

        /// <summary>
        /// Returns a new control that is handy for a positive answer.
        /// </summary>
        /// The caller doesn't need to worry about Release()ing the returned control.
        public static Control GetYesControl()
        {
            // At each call, we hand out the same copy of the control.
            yesControl.Clear();
            yesControl.Add(yControl);
            return yesControl;
        }

        /// <summary>
        /// Returns a new control that is handy for a negative answer.
        /// </summary>
        /// The caller doesn't need to worry about Release()ing the returned control.
        public static Control GetNoControl()
        {
            // At each call, we hand out the same copy of the control.
            noControl.Clear();
            noControl.Add(nControl);
            return noControl;
        }

        #endregion Static members

        Control control;
        Action callback;

        /// <summary>
        /// Creates a triggered callback.
        /// </summary>
        /// <param name="control">The triggering control.</param>
        /// <param name="callback">The callback.</param>
        public TriggeredCallback(Control control, Action callback)
        {
            if (control == null || callback == null)
                throw new ArgumentNullException("DialogAction got null arguments");
            this.control = control;
            this.callback = callback;
        }

        /// <summary>
        /// Updates the triggered callback, invoking the callback if the control triggers it.
        /// </summary>
        /// <returns><c>true</c> if the callback was invoked, or
        /// <c>false</c> otherwise.</returns>
        public bool Update()
        {
            if (control.Pulse)
            {
                callback();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Disposes of reserved resources.
        /// </summary>
        public void Dispose()
        {
            control.Release();
        }

        /// <summary>
        /// Destructor. Disposes of reserved resources.
        /// </summary>
        ~TriggeredCallback()
        {
            Dispose();
        }
    }
}
