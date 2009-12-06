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
        /// If true, thurst the ship backward, otherwise thrust the ship forward.
        /// </summary>
        [TypeParameter]
        bool reverse;

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Thruster()
            : base()
        {
            reverse = true;
        }

        public Thruster(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            FireMode = FireModeType.Continuous;
        }

        public override void Fire(AW2.UI.ControlState triggerState)
        {
            if (!CanFire) return;
            if (triggerState.force > 0)
            {
                StartFiring();
                float direction = reverse ? owner.Rotation + MathHelper.Pi : owner.Rotation;
                owner.Thrust(triggerState.force, AssaultWing.Instance.GameTime.ElapsedGameTime, direction);
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
