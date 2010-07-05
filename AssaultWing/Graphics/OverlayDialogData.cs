using Microsoft.Xna.Framework;
using AW2.UI;

namespace AW2.Graphics
{
    /// <summary>
    /// Content and action data for an overlay dialog.
    /// </summary>
    /// <seealso cref="OverlayDialog"/>
    public abstract class OverlayDialogData : OverlayComponent
    {
        private TriggeredCallback[] _actions;

        /// <summary>
        /// Dimensions are meaningless because our alignments are Stretch.
        /// </summary>
        public override Point Dimensions { get { return new Point(1, 1); } }

        /// <summary>
        /// The triggered callbacks for the dialog.
        /// </summary>
        protected TriggeredCallback[] Actions { set { _actions = value; } }

        public OverlayDialogData(params TriggeredCallback[] actions)
            : base(null, HorizontalAlignment.Stretch, VerticalAlignment.Stretch)
        {
            _actions = actions;
        }

        /// <summary>
        /// Updates the overlay dialog contents and acts on triggered callbacks.
        /// </summary>
        public virtual void Update()
        {
            foreach (var action in _actions) action.Update();
        }
    }
}
