using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;

namespace AW2
{
    /// <summary>
    /// A wrapper for <see cref="Microsoft.Xna.Framework.GameWindow"/>.
    /// </summary>
    public class AWGameWindow : AW2.Graphics.IWindow
    {
        private GameWindow _window;
        private Form _windowForm;
        private GraphicsDeviceManager _graphics;
        private Rectangle _windowedSize;

        public AWGameWindow(GameWindow window, GraphicsDeviceManager graphics)
        {
            _window = window;
            _windowForm = (Form)Form.FromHandle(window.Handle);
            _graphics = graphics;
        }

        #region IWindow Members

        /// <summary>
        /// The title of the window.
        /// </summary>
        public string Title { get { return _window.Title; } set { _window.Title = value; } }

        /// <summary>
        /// Can the user resize the window.
        /// </summary>
        public bool AllowUserResizing
        {
            get { return _window.AllowUserResizing; }
            set
            {
                // This piece of code may get called from another thread. Therefore
                // we cannot set window.AllowUserResizing = false; without Invoke().
                _windowForm.Invoke(new Action(() => { _window.AllowUserResizing = value; }));
            }
        }

        public bool IsFullscreen { get { return _graphics.IsFullScreen; } }

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

        /// <summary>
        /// Low-level handle to the area the game can draw on.
        /// </summary>
        public IntPtr Handle { get { return _window.Handle; } }

        /// <summary>
        /// Called when the dimensions of the drawable area has changed.
        /// </summary>
        public event EventHandler ClientSizeChanged
        {
            add { _window.ClientSizeChanged += value; }
            remove { _window.ClientSizeChanged -= value; }
        }

        /// <summary>
        /// Toggles between fullscreen and windowed mode.
        /// </summary>
        public void ToggleFullscreen()
        {
            lock (_graphics)
            {
                // Set our window size and format preferences before switching.
                if (_graphics.IsFullScreen)
                {
                    _graphics.PreferredBackBufferWidth = _windowedSize.Width;
                    _graphics.PreferredBackBufferHeight = _windowedSize.Height;
                }
                else
                {
                    _windowedSize.Width = ClientBounds.Width;
                    _windowedSize.Height = ClientBounds.Height;
                    var displayMode = _graphics.GraphicsDevice.DisplayMode;
                    _graphics.PreferredBackBufferWidth = displayMode.Width;
                    _graphics.PreferredBackBufferHeight = displayMode.Height;
                }
                _graphics.ToggleFullScreen();
            }
        }

        #endregion
    }
}
