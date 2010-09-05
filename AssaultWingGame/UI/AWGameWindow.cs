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
        /// The title of the window.
        /// </summary>
        public string Title { get { return _window.Title; } set { _window.Title = value; } }

        [Obsolete("Consider using GraphicsDeviceService.Instance.GraphicsDevice.PresentationParameters.IsFullScreen instead")]
        public bool IsFullscreen { get { return GraphicsDeviceService.Instance.GraphicsDevice.PresentationParameters.IsFullScreen; } }

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
            // Set our window size and format preferences before switching.
            if (GraphicsDeviceService.Instance.GraphicsDevice.PresentationParameters.IsFullScreen)
            {
                GraphicsDeviceService.Instance.SetWindowed(_windowedSize.Width, _windowedSize.Height);
            }
            else
            {
                _windowedSize.Width = ClientBounds.Width;
                _windowedSize.Height = ClientBounds.Height;
                var displayMode = GraphicsDeviceService.Instance.GraphicsDevice.DisplayMode;
                GraphicsDeviceService.Instance.SetFullScreen(displayMode.Width, displayMode.Height);
            }
        }

        #endregion
    }
}
