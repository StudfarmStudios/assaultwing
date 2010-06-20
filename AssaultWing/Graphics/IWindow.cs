using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Graphics
{
    /// <summary>
    /// A window where AssaultWing can draw itself.
    /// </summary>
    public interface IWindow
    {
        /// <summary>
        /// Title of the window, displayed to the end-user.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Does the window allow resizing by the end-user.
        /// </summary>
        bool AllowUserResizing { get; set; }

        bool IsFullscreen { get; }

        /// <summary>
        /// Width and height of the area to draw on, in pixels.
        /// </summary>
        Rectangle ClientBounds { get; }

        /// <summary>
        /// Minimum allowed width and height of the area to draw on, in pixels.
        /// </summary>
        Rectangle ClientBoundsMin { get; set; }

        /// <summary>
        /// The low-level handle to the window.
        /// </summary>
        IntPtr Handle { get; }

        /// <summary>
        /// Called when <see cref="ClientBounds"/> has changed.
        /// </summary>
        event EventHandler ClientSizeChanged;

        /// <summary>
        /// Toggles between fullscreen and windowed mode.
        /// </summary>
        void ToggleFullscreen();
    }
}
