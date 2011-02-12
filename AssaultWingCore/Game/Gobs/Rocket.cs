using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.GobUtils;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;

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
        /// Time at which thursting ends, in game time.
        /// </summary>
        private TimeSpan _thrustEndTime;

        private TimeSpan _lastFindTarget;
        private GobTrackerItem _targetTracker;

        #endregion Rocket fields

        private Gob Target { get { return _targetProxy != null ? _targetProxy.GetValue() : null; } set { _targetProxy = value; } }
        private LazyProxy<int, Gob> _targetProxy;

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
            if (Target != null && Target.Dead) Target = null;
            if (_lastFindTarget + FIND_TARGET_INTERVAL <= Arena.TotalTime)
            {
                _lastFindTarget = Arena.TotalTime;
                UpdateTarget();
            }
            if (Arena.TotalTime < _thrustEndTime)
            {
                if (Target != null)
                {
                    var predictedTargetPos = PredictPositionDecent(Target);
                    RotateTowards(predictedTargetPos - Pos, _targetTurnSpeed);
                }
                Thrust();
            }
            else
            {
                RotateTowards(Move, _fallTurnSpeed);
                if (_targetTracker != null)
                    RemoveGobTrackers();
            }

            base.Update();

            // Manage exhaust engines.
            if (Arena.TotalTime >= _thrustEndTime)
                SwitchEngineFlashAndBang(false);
        }

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
                theirArea.Owner.InflictDamage(_impactDamage, new DamageInfo(this));
            Die();
        }

        public override void Dispose()
        {
            RemoveGobTrackers();
            base.Dispose();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                int targetID = Target != null ? Target.ID : Gob.INVALID_ID;
                writer.Write((int)targetID);
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                int targetID = reader.ReadInt32();
                _targetProxy = new LazyProxy<int, Gob>(FindGob);
                _targetProxy.SetData(targetID);
                UpdateGobTrackers();
            }
        }

        #endregion Methods related to serialisation

        #region Private methods

        private Vector2 PredictPositionDecent(Gob gob)
        {
            var trip = Target.Pos - Pos;
            float distance = trip.Length();
            var unitTowardsTarget = trip / distance;
            var targetEscapeMove = unitTowardsTarget * Vector2.Dot(Target.Move, unitTowardsTarget);
            var rocketChaseMove = unitTowardsTarget * _maxSpeed;
            var relativeMove = rocketChaseMove - targetEscapeMove;
            float secondsToCollisionEstimate = distance / relativeMove.Length();
            float lookAheadSeconds = MathHelper.Clamp(secondsToCollisionEstimate, 0, 1);
            return gob.Pos + gob.Move * lookAheadSeconds;
        }

        private void RotateTowards(Vector2 direction, float rotationSpeed)
        {
            float rotationGoal = AWMathHelper.Angle(direction);
            Rotation = AWMathHelper.InterpolateTowardsAngle(Rotation, rotationGoal,
                Game.PhysicsEngine.ApplyChange(rotationSpeed, Game.GameTime.ElapsedGameTime));
        }

        private void Thrust()
        {
            var forceVector = _thrustForce * AWMathHelper.GetUnitVector2(Rotation);
            Game.PhysicsEngine.ApplyLimitedForce(this, forceVector, _maxSpeed,
                Game.GameTime.ElapsedGameTime);
        }

        private void UpdateTarget()
        {
            var oldTarget = Target;
            var potentialTargets =
                from player in Game.DataEngine.Players
                where player.Ship != null
                select player.Ship;
            var newBestTarget = TargetSelection.ChooseTarget(potentialTargets, this, _findTargetRange);
            if (newBestTarget != null &&
                (newBestTarget.Owner == null || newBestTarget.Owner == Owner) &&
                RandomHelper.GetRandomFloat() < 0.9)
                newBestTarget = null;
            if (newBestTarget != null) Target = newBestTarget;
            UpdateGobTrackers();
            if (Game.NetworkMode == AW2.Core.NetworkMode.Server && Target != oldTarget)
                ForcedNetworkUpdate = true;
        }

        private void UpdateGobTrackers()
        {
            if (_targetTracker == null && Target == null) return;
            if (_targetTracker != null && Target == _targetTracker.Gob) return;
            RemoveGobTrackers();
            CreateGobTrackers();
        }

        private void RemoveGobTrackers()
        {
            if (_targetTracker == null) return;
            if (Owner != null) Owner.GobTrackerItems.Remove(_targetTracker);
            if (_targetTracker.Gob != null && _targetTracker.Gob.Owner != null)
                _targetTracker.Gob.Owner.GobTrackerItems.Remove(_targetTracker);
            _targetTracker = null;
        }

        private void CreateGobTrackers()
        {
            if (_targetTracker != null) throw new ApplicationException("Rocket is creating a gob tracker although it has one already");
            if (Target == null) return;
            _targetTracker = new GobTrackerItem(Target, this, GobTrackerItem.ROCKET_TARGET_TEXTURE)
            {
                StickToBorders = false,
                ShowWhileTargetOnScreen = true,
            };
            if (Owner != null) Owner.GobTrackerItems.Add(_targetTracker);
            if (Target.Owner != null) Target.Owner.GobTrackerItems.Add(_targetTracker);
        }

        #endregion Private methods
    }
}
