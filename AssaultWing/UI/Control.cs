using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.UI
{
    /// <summary>
    /// Represents a one-direction control on an input device.
    /// </summary>
    /// The control can be read either as a pulse control or a force control.
    /// A pulse control only signals an instant action.
    /// A force control signals constant action with an amount of force.
    /// It is important to call the base constructor in all subclass constructors;
    /// failure to do this will result in the control not working.
    /// When a control is no longer used, it must be released with Release() to
    /// stop it from being updated.
    public abstract class Control
    {
        private static List<Control> g_registeredControls = new List<Control>();

        /// <summary>
        /// Creates a new control and registers it to receive regular updates from UIEngine.
        /// </summary>
        public Control()
        {
            g_registeredControls.Add(this);
        }

        public static void ForEachControl(Action<Control> action)
        {
            foreach (var control in g_registeredControls) action(control);
        }

        public void Dispose()
        {
            // FIXME !!! This may get called on a separate thread while iterating over 'controls' -> uncaught exception
            g_registeredControls.Remove(this);
        }

        /// <summary>
        /// Sets the control's state based on the current and previous state of all inputs.
        /// </summary>
        /// <param name="oldState">The old state of all inputs.</param>
        /// <param name="newState">The current state of all inputs.</param>
        public abstract void SetState(ref InputState oldState, ref InputState newState);

        /// <summary>
        /// Returns if the control gave a pulse.
        /// </summary>
        public abstract bool Pulse { get; }

        /// <summary>
        /// Returns the amount of control force; a float between 0 and 1.
        /// </summary>
        public abstract float Force { get; }

        /// <summary>
        /// Returns the state of the control.
        /// </summary>
        public ControlState State { get { return new ControlState(Force, Pulse); } }
    }
}
