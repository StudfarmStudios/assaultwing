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
        private bool _reverse;

        /// <summary>
        /// Thrust force factor, relative to the owning ship's thrust force.
        /// </summary>
        [TypeParameter]
        private float _thrustForceFactor;

        [TypeParameter]
        private float _collisionDamageToOthersMultiplier;

        [TypeParameter]
        private float _energyUsagePerAbsorbedDamageUnit;

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Thruster()
        {
            _reverse = true;
            _thrustForceFactor = 1;
            _collisionDamageToOthersMultiplier = 5;
            _energyUsagePerAbsorbedDamageUnit = 10;
        }

        public Thruster(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            base.Activate();
            Owner.CollisionDamageToOthersMultiplier *= _collisionDamageToOthersMultiplier;
            Owner.CollisionDamageFilter = FilterDamage;
        }

        public override FiringResult TryFire(AW2.UI.ControlState triggerState)
        {
            var thrustForce = triggerState.Force * _thrustForceFactor;
            var direction = _reverse ? Owner.Rotation + MathHelper.Pi : Owner.Rotation;
            Owner.Thrust(thrustForce, Owner.Game.GameTime.ElapsedGameTime, direction);
            return FiringResult.Void;
        }

        protected override void ShootImpl() { }
        protected override void CreateVisualsImpl() { }

        private float FilterDamage(float damageAmount)
        {
            var requiredCharge = damageAmount * _energyUsagePerAbsorbedDamageUnit;
            if (requiredCharge <= Charge)
            {
                Charge -= requiredCharge;
                return 0;
            }
            else
            {
                Charge = 0;
                return damageAmount - Charge / _energyUsagePerAbsorbedDamageUnit;
            }
        }
    }
}
