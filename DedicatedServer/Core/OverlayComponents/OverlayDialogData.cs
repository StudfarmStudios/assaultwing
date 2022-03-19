using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Graphics;
using AW2.Menu;
using AW2.UI;

namespace AW2.Core.OverlayComponents
{
    /// <summary>
    /// Content and action data for an overlay dialog.
    /// </summary>
    /// <seealso cref="OverlayDialog"/>
    public abstract class OverlayDialogData : OverlayComponent
    {
        private static readonly TimeSpan ACTION_WARMUP_TIME = TimeSpan.FromSeconds(0.1);
        private TriggeredCallback[] _actions;
        private TimeSpan _firstUpdate;

        public AssaultWing Game { get { return Menu.Game; } }
        public MenuEngineImpl Menu { get; private set; }

        /// <summary>
        /// Dimensions are meaningless because our alignments are Stretch.
        /// </summary>
        public override Point Dimensions { get { return new Point(1, 1); } }

        public IEnumerable<Control> Controls { get { return _actions.Select(action => action.Control); } }

        /// <summary>
        /// If non-null, then the <see cref="OverlayDialogData"/> will replace all
        /// previous <see cref="OverlayDialogData"/>s with the same <see cref="GroupName"/>.
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// The triggered callbacks for the dialog.
        /// </summary>
        protected TriggeredCallback[] Actions { set { _actions = value; } }

        private bool IsCheckingActions { get { return _firstUpdate != TimeSpan.Zero && _firstUpdate + ACTION_WARMUP_TIME < Game.GameTime.TotalRealTime; } }

        public OverlayDialogData(MenuEngineImpl menu, params TriggeredCallback[] actions)
            : base(null, HorizontalAlignment.Stretch, VerticalAlignment.Stretch)
        {
            Menu = menu;
            _actions = actions;
        }

        /// <summary>
        /// Updates the overlay dialog contents and acts on triggered callbacks.
        /// </summary>
        public override void Update()
        {
            if (_firstUpdate == TimeSpan.Zero) _firstUpdate = Game.GameTime.TotalRealTime;
            if (IsCheckingActions && _actions.Any(action => action.Update())) Hide();
        }

        public void Hide()
        {
            Game.HideDialog();
        }
    }
}
