using System;
using System.Collections.Generic;
using System.Text;

namespace AW2.UI
{
    /// <summary>
    /// A control combined of various subcontrols.
    /// The subcontrol with the strongest signal will determine the
    /// state of the multicontrol.
    /// </summary>
    /// Useful for grouping several alternative controls into one
    /// bunch that is easier to handle.
    /// Note: A multicontrol isn't responsible for Release()ing its subcontrols.
    public class MultiControl : Control
    {
        List<Control> controls;

        /// <summary>
        /// Creates a multicontrol.
        /// </summary>
        public MultiControl()
            : base()
        {
            controls = new List<Control>();
        }

        /// <summary>
        /// Adds a previously created control as a subcontrol to the multicontrol.
        /// </summary>
        /// <param name="control">The control to add.</param>
        public void Add(Control control)
        {
            controls.Add(control);
        }

        /// <summary>
        /// Removes all previously added subcontrols from the multicontrol.
        /// </summary>
        /// Note: This method doesn't Release() the subcontrols.
        public void Clear()
        {
            controls.Clear();
        }

        /// <summary>
        /// Sets the control's state based on the current and previous state of all inputs.
        /// </summary>
        /// <param name="oldState">The old state of all inputs.</param>
        /// <param name="newState">The current state of all inputs.</param>
        public override void SetState(ref InputState oldState, ref InputState newState)
        {
            // The subcontrols will be updated separately, and that's enough.
        }

        /// <summary>
        /// Returns if the control gave a pulse.
        /// </summary>
        public override bool Pulse
        {
            get
            {
                foreach (Control control in controls)
                    if (control.Pulse) return true;
                return false;
            }
        }

        /// <summary>
        /// Returns the amount of control force; a float between 0 and 1.
        /// </summary>
        public override float Force { get {
            float force = 0;
            foreach (Control control in controls)
                force = Math.Max(force, control.Force);
            return force; 
        } }
    }
}
