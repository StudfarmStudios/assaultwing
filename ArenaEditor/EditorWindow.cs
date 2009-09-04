using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AW2.Graphics;
using Microsoft.Xna.Framework;

namespace AW2
{
    /// <summary>
    /// An area where AssaultWing can draw itself.
    /// </summary>
    class EditorWindow : PictureBox, IWindow
    {
        #region IWindow Members

        public string Title { get; set; }

        public bool AllowUserResizing { get; set; }

        public Rectangle ClientBounds { get { return new Rectangle(Left, Top, Width, Height); } }

        public Rectangle ClientBoundsMin
        {
            get
            {
                return new Rectangle(0, 0, MinimumSize.Width, MinimumSize.Height);
            }
            set
            {
                MinimumSize = new System.Drawing.Size(value.Width, value.Height);
            }
        }

        public new IntPtr Handle { get { return base.Handle; } }

        public new event EventHandler ClientSizeChanged
        {
            add { SizeChanged += value; }
            remove { SizeChanged -= value; }
        }

        public new void Resize(int width, int height)
        {
            Size = new System.Drawing.Size(width, height);
        }

        #endregion
    }
}
