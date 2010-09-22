using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using AW2.Graphics;

namespace AW2
{
    /// <summary>
    /// An area in ArenaEditor where the game view is drawn.
    /// </summary>
    public class EditorWindow : PictureBox, IWindow
    {
        public Rectangle ClientBounds { get { return new Rectangle(Left, Top, Width, Height); } }

        public Rectangle ClientBoundsMin
        {
            get { return new Rectangle(0, 0, MinimumSize.Width, MinimumSize.Height); }
            set { MinimumSize = new System.Drawing.Size(value.Width, value.Height); }
        }
    }
}
