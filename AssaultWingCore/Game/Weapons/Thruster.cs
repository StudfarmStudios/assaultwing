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

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Thruster()
        {
            _reverse = true;
            _thrustForceFactor = 1;
        }

        public Thruster(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            FiringOperator = new FiringOperatorContinuous(this);
        }

        protected override void ShootImpl()
        {
            var duration = Owner.Game.GameTime.ElapsedGameTime;
            float direction = _reverse ? Owner.Rotation + MathHelper.Pi : Owner.Rotation;
            Owner.Thrust(_thrustForceFactor, duration, direction);
        }

        protected override void CreateVisualsImpl()
        {
        }
    }
}
