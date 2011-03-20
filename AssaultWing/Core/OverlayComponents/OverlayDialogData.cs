using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Graphics;
using AW2.UI;

namespace AW2.Core.OverlayComponents
{
    /// <summary>
    /// Content and action data for an overlay dialog.
    /// </summary>
    /// <seealso cref="OverlayDialog"/>
    public abstract class OverlayDialogData : OverlayComponent
    {
        private TriggeredCallback[] _actions;

        public AssaultWing Game { get; private set; }

        /// <summary>
        /// Dimensions are meaningless because our alignments are Stretch.
        /// </summary>
        public override Point Dimensions { get { return new Point(1, 1); } }

        public IEnumerable<Control> Controls { get { return _actions.Select(action => action.Control); } }

        /// <summary>
        /// The triggered callbacks for the dialog.
        /// </summary>
        protected TriggeredCallback[] Actions { set { _actions = value; } }

        public OverlayDialogData(AssaultWing game, params TriggeredCallback[] actions)
            : base(null, HorizontalAlignment.Stretch, VerticalAlignment.Stretch)
        {
            Game = game;
            _actions = actions;
        }

        /// <summary>
        /// Updates the overlay dialog contents and acts on triggered callbacks.
        /// </summary>
        public virtual void Update()
        {
            if (_actions.Any(action => action.Update())) Hide();
        }

        public void Hide()
        {
            Game.HideDialog();
        }
    }
}
