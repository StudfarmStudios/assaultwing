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
    public class MultiControl : Control, IEnumerable<Control>
    {
        private List<Control> _controls;

        public override bool Pulse { get { return _controls.Any(control => control.Pulse); } }
        public override float Force { get { return _controls.Max(control => control.Force); } }

        public MultiControl()
        {
            _controls = new List<Control>();
        }

        public override void SetLocalState(InputState oldState, InputState newState)
        {
            base.SetLocalState(oldState, newState);
            foreach (var control in _controls) control.SetLocalState(oldState, newState);
        }

        public void Add(Control control)
        {
            _controls.Add(control);
        }

        public override string ToString()
        {
            return "[" + string.Join("|", _controls.Select(x => x.ToString())) + "]";
        }

        public IEnumerator<Control> GetEnumerator()
        {
            return _controls.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _controls.GetEnumerator();
        }
    }
}
