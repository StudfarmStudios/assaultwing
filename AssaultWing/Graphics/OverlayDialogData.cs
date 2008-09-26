using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.UI;

namespace AW2.Graphics
{
    /// <summary>
    /// Content and action data for an overlay dialog.
    /// </summary>
    /// <seealso cref="OverlayDialog"/>
    public abstract class OverlayDialogData : OverlayComponent
    {
        TriggeredCallback[] actions;

        /// <summary>
        /// The triggered callbacks for the dialog.
        /// </summary>
        protected TriggeredCallback[] Actions { set { actions = value; } }

        /// <summary>
        /// Creates content and callbacks for an overlay dialog.
        /// </summary>
        public OverlayDialogData(params TriggeredCallback[] actions)
            : base(HorizontalAlignment.Center, VerticalAlignment.Center)
        {
            this.actions = actions;
        }

        /// <summary>
        /// Updates the overlay dialog contents and acts on triggered callbacks.
        /// </summary>
        public virtual void Update()
        {
            foreach (TriggeredCallback action in actions)
                action.Update();
        }

        /// <summary>
        /// The dimensions of the component in pixels.
        /// </summary>
        /// The return value field <c>Point.X</c> is the width of the component,
        /// and the field <c>Point.Y</c> is the height of the component,
        public override Point Dimensions
        {
            get
            {
                // By default we keep the viewport as it is.
                // This is okay because there is only one centered
                // overlay component in an overlay dialog at a time.
                GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
                return new Point(gfx.Viewport.Width, gfx.Viewport.Height);
            }
        }
    }
}
