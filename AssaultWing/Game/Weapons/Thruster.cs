using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Additional thruster for a ship. Can thrust the ship forward or backward.
    /// </summary>
    public class Thruster : ShipDevice
    {
        /// <summary>
        /// If true, thrust the ship backward, otherwise thrust the ship forward.
        /// </summary>
        [TypeParameter]
        private bool reverse;

        /// <summary>
        /// Thrust force factor, relative to the owning ship's thrust force.
        /// </summary>
        [TypeParameter]
        private float thrustForceFactor;

        /// <summary>
        /// Seconds of game time the thruster must be unused for the extra force charge
        /// to reset to its maximum. Should be greater than <see cref="extraForceChargeSecondsMaximum"/>.
        /// </summary>
        [TypeParameter]
        private float extraForceChargeDelay;

        /// <summary>
        /// Number of seconds in game time the thruster boosts with extra force,
        /// when the extra force has been charged.
        /// </summary>
        [TypeParameter]
        private float extraForceChargeSeconds;

        private TimeSpan _extraForceReady;
        private TimeSpan _extraForceEnd;

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Thruster()
        {
            reverse = true;
            thrustForceFactor = 1;
            extraForceChargeDelay = 3;
            extraForceChargeSeconds = 1;
        }

        public Thruster(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            FiringOperator = new FiringOperatorContinuous(this);
        }

        protected override bool PermissionToFire(bool canFire)
        {
            if (_extraForceReady <= Arena.TotalTime)
                _extraForceEnd = Arena.TotalTime + TimeSpan.FromSeconds(extraForceChargeSeconds);
            _extraForceReady = Arena.TotalTime + TimeSpan.FromSeconds(extraForceChargeDelay);
            return true;
        }

        protected override void ShootImpl()
        {
            var duration = owner.Game.GameTime.ElapsedGameTime;
            float direction = reverse ? owner.Rotation + MathHelper.Pi : owner.Rotation;
            float thrustForce = _extraForceEnd > Arena.TotalTime
                ? thrustForceFactor * 2
                : thrustForceFactor;
            owner.Thrust(thrustForce, duration, direction);
        }

        protected override void CreateVisualsImpl()
        {
        }
    }
}
