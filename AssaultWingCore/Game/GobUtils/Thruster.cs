using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Propels a gob forward.
    /// </summary>
    [LimitedSerialization]
    public class Thruster
    {
        /// <summary>
        /// Maximum force of thrust, measured in Newtons.
        /// </summary>
        [TypeParameter]
        private float _maxForce;

        /// <summary>
        /// Maximum speed reachable by thrust, measured in meters per second.
        /// </summary>
        [TypeParameter]
        private float _maxSpeed;

        public Gob Owner { get; set; }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Thruster()
        {
            _maxForce = 50000;
            _maxSpeed = 200;
        }

        /// <param name="proportionalThrust">Proportional amount of thrust, between -1 (full thrust backward)
        /// and 1 (full thrust forward).</param>
        /// <param name="direction">Direction of thrust in radians.</param>
        public void Thrust(float proportionalThrust, float direction)
        {
            ThrustImpl(proportionalThrust, AWMathHelper.GetUnitVector2(direction));
        }

        /// <param name="proportionalThrust">Proportional amount of thrust, between -1 (full thrust backward)
        /// and 1 (full thrust forward).</param>
        /// <param name="direction">Direction of thrust. Amplitude is irrelevant.</param>
        public void Thrust(float proportionalThrust, Vector2 direction)
        {
            ThrustImpl(proportionalThrust, Vector2.Normalize(direction));
        }

        private void ThrustImpl(float proportionalThrust, Vector2 unitDirection)
        {
            if (proportionalThrust < -1 || proportionalThrust > 1) throw new ArgumentOutOfRangeException("proportionalThrust");
            if (Owner == null) throw new InvalidOperationException("No owner to thrust");
            var force = _maxForce * proportionalThrust * unitDirection;
            Owner.Game.PhysicsEngine.ApplyLimitedForce(Owner, force, _maxSpeed, Owner.Game.GameTime.ElapsedGameTime);
        }
    }
}
