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
        protected static InputState OldState { get; private set; }
        protected static InputState NewState { get; private set; }

        /// <summary>
        /// Creates a new control and registers it to receive regular updates from UIEngine.
        /// </summary>
        public Control()
        {
            g_registeredControls.Add(this);
        }

        public static void SetState(ref InputState oldState, ref InputState newState)
        {
            OldState = oldState;
            NewState = newState;
        }

        public static void ForEachControl(Action<Control> action)
        {
            foreach (var control in g_registeredControls) action(control);
        }

        public void Dispose()
        {
            g_registeredControls.Remove(this);
        }

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
