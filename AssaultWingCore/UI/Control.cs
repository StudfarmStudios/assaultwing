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
        private static int g_nextTimestamp;
        private static int g_globalStateTimestamp;
        private static InputState g_oldGlobalState;
        private static InputState g_newGlobalState;

        private int _localStateTimestamp;
        private InputState _oldLocalState;
        private InputState _newLocalState;

        protected InputState OldState
        {
            get
            {
                if (g_globalStateTimestamp < _localStateTimestamp) return _oldLocalState;
                _oldLocalState = _newLocalState = null;
                return g_oldGlobalState;
            }
        }

        protected InputState NewState
        {
            get
            {
                if (g_globalStateTimestamp < _localStateTimestamp) return _newLocalState;
                _oldLocalState = _newLocalState = null;
                return g_newGlobalState;
            }
        }

        public static void SetGlobalState(InputState oldState, InputState newState)
        {
            g_globalStateTimestamp = g_nextTimestamp++;
            g_oldGlobalState = oldState;
            g_newGlobalState = newState;
        }

        public virtual void SetLocalState(InputState oldState, InputState newState)
        {
            _localStateTimestamp = g_nextTimestamp++;
            _oldLocalState = oldState;
            _newLocalState = newState;
        }

        /// <summary>
        /// Returns if the control gave a pulse.
        /// </summary>
        public abstract bool Pulse { get; }

        /// <summary>
        /// Returns the amount of control force; a float between 0 and 1.
        /// </summary>
        public abstract float Force { get; }

        public bool HasSignal { get { return Pulse || Force > 0; } }

        /// <summary>
        /// Returns the state of the control.
        /// </summary>
        public ControlState State { get { return new ControlState(Force, Pulse); } }
    }
}
