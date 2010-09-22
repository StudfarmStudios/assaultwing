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
        /// Width and height of the area to draw on, in pixels.
        /// </summary>
        Rectangle ClientBounds { get; }

        /// <summary>
        /// Minimum allowed width and height of the area to draw on, in pixels.
        /// </summary>
        Rectangle ClientBoundsMin { get; set; }
    }
}
