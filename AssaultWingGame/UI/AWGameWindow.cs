using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using AW2.Core;

namespace AW2.UI
{
    /// <summary>
    /// A wrapper for <see cref="Microsoft.Xna.Framework.GameWindow"/>.
    /// </summary>
    public class AWGameWindow : AW2.Graphics.IWindow
    {
        private GameForm _window;
        private Form _windowForm;
        private Rectangle _windowedSize;

        public AWGameWindow(GameForm window)
        {
            _window = window;
            _windowForm = (Form)Form.FromHandle(window.Handle);
        }

        #region IWindow Members

        /// <summary>
        /// Dimensions of the area the game can draw on.
        /// </summary>
        public Rectangle ClientBounds { get { return _window.ClientBounds; } }

        /// <summary>
        /// Minimum dimensions of the area the game can draw on.
        /// </summary>
        public Rectangle ClientBoundsMin
        {
            get
            {
                var min = _windowForm.MinimumSize;
                return new Rectangle(0, 0, min.Width, min.Height);
            }
            set
            {
                _windowForm.MinimumSize = new System.Drawing.Size(value.Width, value.Height);
            }
        }

        #endregion
    }
}
