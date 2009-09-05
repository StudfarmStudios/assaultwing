using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Game
{
    /// <summary>
    /// Information about an arena.
    /// </summary>
    public class ArenaInfo
    {
        /// <summary>
        /// Name of the arena.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name of the file where the arena is serialised.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Dimensions of the arena.
        /// </summary>
        public Vector2 Dimensions { get; set; }

        /// <summary>
        /// Name of the preview texture of the arena.
        /// </summary>
        public string PreviewName { get; set; }
    }
}
