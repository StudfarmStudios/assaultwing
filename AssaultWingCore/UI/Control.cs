namespace AW2.UI
{
    /// <summary>
    /// Represents a one-direction control on an input device.
    /// </summary>
    /// <remarks>
    /// The control can be read either as a pulse control or a force control.
    /// A pulse control only signals an instant action.
    /// A force control signals constant action with an amount of force.
    /// </remarks>
    public abstract class Control
    {
        protected static InputState OldState { get; private set; }
        protected static InputState NewState { get; private set; }

        public static void SetState(ref InputState oldState, ref InputState newState)
        {
            OldState = oldState;
            NewState = newState;
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
