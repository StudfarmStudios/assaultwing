using System;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A rocket that has its own means of propulsion.
    /// </summary>
    public class Rocket : Gob
    {
        #region Rocket fields

        /// <summary>
        /// Amount of damage to inflict on impact with a damageable gob.
        /// </summary>
        [TypeParameter]
        private float _impactDamage;

        /// <summary>
        /// Maximum force of thrust of the rocket, measured in Newtons.
        /// </summary>
        [TypeParameter]
        private float _thrustForce;

        /// <summary>
        /// Duration of thrust, measured in seconds.
        /// </summary>
        [TypeParameter]
        private float _thrustDuration;

        /// <summary>
        /// Maximum turning speed of the rocket, measured in radians per second.
        /// </summary>
        [TypeParameter]
        private float _turnSpeed;

        /// <summary>
        /// Time at which thursting ends, in game time.
        /// </summary>
        private TimeSpan _thrustEndTime;

        #endregion Rocket fields

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Rocket()
        {
            _impactDamage = 100;
            _thrustForce = 100;
            _thrustDuration = 2;
            _turnSpeed = 5;
        }

        public Rocket(CanonicalString typeName)
            : base(typeName)
        {
        }

        #region Methods related to gobs' functionality in the game world

        public override void Activate()
        {
            base.Activate();
            _thrustEndTime = Arena.TotalTime + TimeSpan.FromSeconds(_thrustDuration);
        }

        public override void Update()
        {
            if (Arena.TotalTime < _thrustEndTime)
                Thrust();
            else
                FallNoseFirst();

            base.Update();

            // Manage exhaust engines.
            if (Arena.TotalTime >= _thrustEndTime)
                SwitchEngineFlashAndBang(false);
        }

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
                theirArea.Owner.InflictDamage(_impactDamage, new DeathCause(theirArea.Owner, DeathCauseType.Damage, this));
            Die(new DeathCause());
        }

        #endregion Methods related to gobs' functionality in the game world

        private void FallNoseFirst()
        {
            float rotationGoal = AWMathHelper.Angle(Move);
            Rotation = AWMathHelper.InterpolateTowardsAngle(Rotation, rotationGoal,
                AssaultWing.Instance.PhysicsEngine.ApplyChange(_turnSpeed, AssaultWing.Instance.GameTime.ElapsedGameTime));
        }

        private void Thrust()
        {
            var forceVector = _thrustForce * AWMathHelper.GetUnitVector2(Rotation);
            AssaultWing.Instance.PhysicsEngine.ApplyForce(this, forceVector);
        }
    }
}
