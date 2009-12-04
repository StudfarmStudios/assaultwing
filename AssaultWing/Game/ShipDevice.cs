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
    public abstract class ShipDevice : Clonable
    {
        public ShipDevice()
        {
        }

        public ShipDevice(CanonicalString typeName)
            : base(typeName)
        {
        }

        #region Public methods

        /// <summary>
        /// Called when the device is added to a game. Subclasses can initialize here things
        /// that couldn't be initialized in the constructor e.g. due to lack of data.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Fires (uses) the device.
        /// </summary>
        public abstract void Fire(AW2.UI.ControlState triggerState);

        /// <summary>
        /// Updates the device's state. This method is called regularly.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Releases all resources allocated by the device.
        /// </summary>
        public abstract void Dispose();

        #endregion Public methods
    }
}
