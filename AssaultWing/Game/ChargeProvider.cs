using System;

namespace AW2.Game
{
    /// <summary>
    /// Provider of charge-related data for a ship device. Produced separately
    /// for each ship device by the ship.
    /// </summary>
    public class ChargeProvider
    {
        /// <summary>
        /// Maximum amount of charge.
        /// </summary>
        public Func<float> ChargeMax { get; private set; }

        /// <summary>
        /// Speed of charging, measured in charge units per second.
        /// </summary>
        public Func<float> ChargeSpeed { get; private set; }

        public ChargeProvider(Func<float> getChargeMax, Func<float> getChargeSpeed)
        {
            ChargeMax = getChargeMax;
            ChargeSpeed = getChargeSpeed;
        }
    }
}
