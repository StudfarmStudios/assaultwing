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
        public string Title { get; set; }

        public bool AllowUserResizing { get; set; }

        public bool IsFullscreen { get { return false; } }

        public Rectangle ClientBounds { get { return new Rectangle(Left, Top, Width, Height); } }

        public Rectangle ClientBoundsMin
        {
            get { return new Rectangle(0, 0, MinimumSize.Width, MinimumSize.Height); }
            set { MinimumSize = new System.Drawing.Size(value.Width, value.Height); }
        }

        public new IntPtr Handle { get { return base.Handle; } }

        public new event EventHandler ClientSizeChanged
        {
            add { SizeChanged += value; }
            remove { SizeChanged -= value; }
        }

        public void ToggleFullscreen()
        {
            // silently ignored
        }
    }
}
