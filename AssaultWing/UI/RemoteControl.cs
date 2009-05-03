using System;
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
        /// Access only through <see cref="LastStateUpdate"/>.
        /// </summary>
        TimeSpan lastStateUpdate;

        /// <summary>
        /// Access only through <see cref="LastPulseRead"/>.
        /// </summary>
        TimeSpan lastPulseRead;

        /// <summary>
        /// Time of last control state update, in game time.
        /// </summary>
        TimeSpan LastStateUpdate
        {
            get { return lastStateUpdate; }
            set
            {
                lastStateUpdate = value;
                // Keep LastStateUpdate and LastPulseRead strictly ordered.
                if (value == lastPulseRead) lastStateUpdate += new TimeSpan(1);
            }
        }

        /// <summary>
        /// Time of last pulse value read time, in game time.
        /// </summary>
        TimeSpan LastPulseRead
        {
            get { return lastPulseRead; }
            set
            {
                lastPulseRead = value;
                // Keep LastStateUpdate and LastPulseRead strictly ordered.
                if (value == lastStateUpdate) lastPulseRead += new TimeSpan(1);
            }
        }

        /// <summary>
        /// Creates a remote control.
        /// </summary>
        public RemoteControl()
            : base()
        {
            pulse = false;
            force = 0;
            LastStateUpdate = new TimeSpan(-1);
            LastPulseRead = new TimeSpan(-1);
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
        public override bool Pulse
        {
            get
            {
                // Give one pulse only on one frame.
                TimeSpan now = AssaultWing.Instance.GameTime.TotalGameTime;
                if (LastStateUpdate < LastPulseRead && LastPulseRead < now)
                    pulse = false;
                LastPulseRead = now;
                return pulse;
            }
        }

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
            if (LastStateUpdate < now)
            {
                this.pulse = pulse;
                this.force = MathHelper.Clamp(force, 0, 1);
            }
            else
            {
                this.pulse |= pulse;
                this.force = MathHelper.Clamp(force, this.force, 1);
            }
            LastStateUpdate = now;
        }
    }
}
