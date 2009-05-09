using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Game
{
    /// <summary>
    /// A bunch of parameters of the gameplay.
    /// </summary>
    public class GameplayMode
    {
        /// <summary>
        /// The types of ship available for selection in the gameplay mode.
        /// </summary>
        public string[] ShipTypes { get; set; }

        /// <summary>
        /// The types of primary weapon available for selection in the gameplay mode.
        /// </summary>
        public string[] Weapon1Types { get; set; }

        /// <summary>
        /// The types of secondary weapon available for selection in the gameplay mode.
        /// </summary>
        public string[] Weapon2Types { get; set; }
    }
}
