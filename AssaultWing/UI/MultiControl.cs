using System;
using System.Collections.Generic;
using System.Linq;

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

        public override bool Pulse { get { return controls.Any(control => control.Pulse); } }
        public override float Force { get { return controls.Max(control => control.Force); } }

        public MultiControl()
        {
            controls = new List<Control>();
        }

        /// <summary>
        /// Adds a previously created control as a subcontrol to the multicontrol.
        /// </summary>
        public void Add(Control control)
        {
            controls.Add(control);
        }

        /// <summary>
        /// Removes all previously added subcontrols from the multicontrol.
        /// </summary>
        /// Note: This method doesn't <see cref="Dispose"/> the subcontrols.
        public void Clear()
        {
            controls.Clear();
        }
    }
}
