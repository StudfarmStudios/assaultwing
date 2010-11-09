﻿using System;

namespace AW2.UI
{
    /// <summary>
    /// State of a control.
    /// </summary>
    public struct ControlState
    {
        /// <summary>
        /// Amount of force of the control, between 0 (no force) and 1 (full force).
        /// </summary>
        public float Force;

        /// <summary>
        /// Did the control give a pulse.
        /// </summary>
        public bool Pulse;

        /// <summary>
        /// Creates a new control state.
        /// </summary>
        /// <param name="force">Amount of force of the control.</param>
        /// <param name="pulse">Did the control give a pulse.</param>
        public ControlState(float force, bool pulse)
        {
            Force = force;
            Pulse = pulse;
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Pulse, Force);
        }
    }
}
