using System;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Graphics;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A rocket that has its own means of propulsion and targetting capabilities.
    /// </summary>
    public class Rocket : Gob
    {
        #region Rocket fields

        private static readonly TimeSpan FIND_TARGET_INTERVAL = TimeSpan.FromSeconds(0.5);

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
        /// Rocket's maximum speed reachable by thrust, measured in meters per second.
        /// </summary>
        [TypeParameter]
        private float _maxSpeed;

        /// <summary>
        /// Maximum turning speed of the rocket when falling without thrusting, measured in radians per second.
        /// </summary>
        [TypeParameter]
        private float _fallTurnSpeed;

        /// <summary>
        /// Maximum turning speed of the rocket when thrusting towards a target, measured in radians per second.
        /// </summary>
        [TypeParameter]
        private float _targetTurnSpeed;

        /// <summary>
        /// Range in which to look for targets, in meters.
        /// </summary>
        [TypeParameter]
        private float _findTargetRange;

        /// <summary>
        /// Half central angle of the sector in which to look for targets, in radians.
        /// </summary>
        [TypeParameter]
        private float _findTargetAngle;

        /// <summary>
        /// Time at which thursting ends, in game time.
        /// </summary>
        private TimeSpan _thrustEndTime;

        private Gob _target;
        private TimeSpan _lastFindTarget;

        #endregion Rocket fields

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Rocket()
        {
            _impactDamage = 100;
            _thrustForce = 100;
            _thrustDuration = 2;
            _maxSpeed = 400;
            _fallTurnSpeed = 5;
            _targetTurnSpeed = 5;
            _findTargetRange = 800;
            _findTargetAngle = MathHelper.Pi / 6;
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
            if (_target == null && _lastFindTarget + FIND_TARGET_INTERVAL <= Arena.TotalTime) FindTarget();
            if (Arena.TotalTime < _thrustEndTime)
            {
                if (_target != null) RotateTowards(_target.Pos - Pos, _targetTurnSpeed);
                Thrust();
            }
            else
            {
                RotateTowards(Move, _fallTurnSpeed);                
                if (_targetTracker != null)
                    RemoveTargetTrackers();
            }

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
            RemoveTargetTrackers();
        }

        private void RemoveTargetTrackers()
        {
            if (_targetTracker != null)
            {
                Owner.RemoveGobTrackerItem(_targetTracker);
                if (_targetTracker.Gob != null)
                    _targetTracker.Gob.Owner.RemoveGobTrackerItem(_targetTracker);
                _targetTracker = null;
            }
        }

        #endregion Methods related to gobs' functionality in the game world

        private void RotateTowards(Vector2 direction, float rotationSpeed)
        {
            float rotationGoal = AWMathHelper.Angle(direction);
            Rotation = AWMathHelper.InterpolateTowardsAngle(Rotation, rotationGoal,
                AssaultWing.Instance.PhysicsEngine.ApplyChange(rotationSpeed, AssaultWing.Instance.GameTime.ElapsedGameTime));
        }

        private void Thrust()
        {
            var forceVector = _thrustForce * AWMathHelper.GetUnitVector2(Rotation);
            AssaultWing.Instance.PhysicsEngine.ApplyLimitedForce(this, forceVector, _maxSpeed,
                AssaultWing.Instance.GameTime.ElapsedGameTime);
        }

        private GobTrackerItem _targetTracker;

        private void FindTarget()
        {
            _lastFindTarget = Arena.TotalTime;
            var targets =
                from gob in Arena.Gobs.GameplayLayer.Gobs
                where gob.IsDamageable && !gob.Disabled && gob.Owner != Owner
                    && AWMathHelper.AbsoluteAngleDifference((gob.Pos - Pos).Angle(), Rotation) <= _findTargetAngle
                let distanceSquared = Vector2.DistanceSquared(gob.Pos, Pos)
                where distanceSquared <= _findTargetRange * _findTargetRange
                orderby distanceSquared ascending
                select gob;
            _target = targets.FirstOrDefault();

            // If the target has changed remove the GobTrackerItem from the list and set
            // the _targetTracker to null so that a new one will be created.
            if (_target != null && _targetTracker != null && _target != _targetTracker.Gob)
            {
                RemoveTargetTrackers();
            }

            // Create a target GobTrackerItem
            if (_target != null && _target is Ship && _targetTracker == null)
            {
                _targetTracker = new GobTrackerItem(_target, this, GobTrackerItem.ROCKET_TARGET_TEXTURE, false, true, true, true, Owner.PlayerColor);
                Owner.AddGobTrackerItem(_targetTracker);
                _target.Owner.AddGobTrackerItem(_targetTracker);
            }
        }
    }
}
