using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using Microsoft.Xna.Framework;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Additional thruster for a ship. Can thrust the ship forward or backward.
    /// </summary>
    class Thruster : ShipDevice
    {
        /// <summary>
        /// If true, thrust the ship backward, otherwise thrust the ship forward.
        /// </summary>
        [TypeParameter]
        bool reverse;

        /// <summary>
        /// Thrust force factor, relative to the owning ship's thrust force.
        /// </summary>
        [TypeParameter]
        float thrustForceFactor;

        /// <summary>
        /// Seconds of game time the thruster must be unused for the extra force charge
        /// to reset to its maximum. Should be greater than <see cref="extraForceChargeSecondsMaximum"/>.
        /// </summary>
        [TypeParameter]
        float extraForceChargeDelay;

        /// <summary>
        /// Number of seconds in game time the thruster boosts with extra force,
        /// when the extra force has been charged.
        /// </summary>
        [TypeParameter]
        float extraForceChargeSeconds;

        TimeSpan _extraForceReady;
        TimeSpan _extraForceEnd;

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Thruster()
            : base()
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
            FireMode = FireModeType.Continuous;
        }

        protected override void FireImpl(AW2.UI.ControlState triggerState)
        {
            if (!CanFire) return;
            if (triggerState.force > 0)
            {
                if (_extraForceReady <= AssaultWing.Instance.GameTime.TotalArenaTime)
                    _extraForceEnd = AssaultWing.Instance.GameTime.TotalArenaTime + TimeSpan.FromSeconds(extraForceChargeSeconds);
                _extraForceReady = AssaultWing.Instance.GameTime.TotalArenaTime + TimeSpan.FromSeconds(extraForceChargeDelay);
                StartFiring();
                var duration = AssaultWing.Instance.GameTime.ElapsedGameTime;
                float direction = reverse ? owner.Rotation + MathHelper.Pi : owner.Rotation;
                float forceFactor = triggerState.force * thrustForceFactor;
                if (_extraForceEnd > AssaultWing.Instance.GameTime.TotalArenaTime) forceFactor *= 2;
                owner.Thrust(forceFactor, duration, direction);
                DoneFiring();
            }
        }

        public override void Update()
        {
        }

        public override void Dispose()
        {
        }
    }
}
