using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Net;
using AW2.Net.Messages;
using Microsoft.Xna.Framework;

namespace AW2.UI
{
    /// <summary>
    /// A control whose state is received from an external entity
    /// instead of the conventional input devices.
    /// </summary>
    public class RemoteControl : Control
    {
        bool pulse;
        float force;

        /// <summary>
        /// Time of last control state update, in game time.
        /// </summary>
        TimeSpan lastStateUpdate;

        /// <summary>
        /// Creates a remote control.
        /// </summary>
        public RemoteControl()
            : base()
        {
            pulse = false;
            force = 0;
            lastStateUpdate = new TimeSpan(-1);
        }

        /// <summary>
        /// Sets the control's state based on the current and previous state of all inputs.
        /// </summary>
        /// <param name="oldState">The old state of all inputs.</param>
        /// <param name="newState">The current state of all inputs.</param>
        public override void SetState(ref InputState oldState, ref InputState newState)
        {
            // The conventional input devices are not of interest to us.
        }

        /// <summary>
        /// Did the control give a pulse.
        /// </summary>
        public override bool Pulse { get { return pulse; } }

        /// <summary>
        /// The amount of control force; a float between 0 and 1.
        /// </summary>
        public override float Force { get { return force; } }

        /// <summary>
        /// Sets the control's state.
        /// </summary>
        /// <param name="force">The force of the control.</param>
        /// <param name="pulse">Did the control give a pulse.</param>
        public void SetControlState(float force, bool pulse)
        {
            // Take the maximum of all state updates during one frame.
            // This gives priority to hard action.
            TimeSpan now = AssaultWing.Instance.GameTime.TotalGameTime;
            if (lastStateUpdate < now)
            {
                this.pulse = pulse;
                this.force = MathHelper.Clamp(force, 0, 1);
            }
            else
            {
                this.pulse |= pulse;
                this.force = MathHelper.Clamp(force, this.force, 1);
            }
            lastStateUpdate = now;
        }
    }
}