using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;

namespace AW2.Game
{
    /// <summary>
    /// A device that a ship can use. 
    /// </summary>
    /// <seealso cref="Weapon"/>
    public class ShipDevice : Clonable
    {
        public ShipDevice()
        {
        }

        public ShipDevice(CanonicalString typeName)
            : base(typeName)
        {
        }
    }
}
