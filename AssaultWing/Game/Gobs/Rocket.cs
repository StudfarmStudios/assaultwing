using System;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Graphics;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
using AW2.Helpers.Geometric;

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
        private GobTrackerItem _targetTracker;

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
            if (_target != null && _target.Dead) _target = null;
            if (_lastFindTarget + FIND_TARGET_INTERVAL <= Arena.TotalTime)
            {
                _lastFindTarget = Arena.TotalTime;
                UpdateTarget();
            }
            if (Arena.TotalTime < _thrustEndTime)
            {
                if (_target != null)
                {
                    var predictedTargetPos = PredictPositionDecent(_target);
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
                theirArea.Owner.InflictDamage(_impactDamage, new DeathCause(theirArea.Owner, DeathCauseType.Damage, this));
            Die(new DeathCause());
        }

        public override void Dispose()
        {
            RemoveGobTrackers();
            base.Dispose();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        public override void Serialize(AW2.Net.NetworkBinaryWriter writer, AW2.Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                int targetID = _target != null ? _target.ID : Gob.INVALID_ID;
                writer.Write((int)targetID);
            }
        }

        public override void Deserialize(AW2.Net.NetworkBinaryReader reader, AW2.Net.SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                int targetID = reader.ReadInt32();
                if (targetID == Gob.INVALID_ID) _target = null;
                else
                {
                    _target = Arena.Gobs.FirstOrDefault(gob => gob.ID == targetID);
                    if (_target == null) Log.Write("WARNING: Couldn't find Rocket target by gob ID " + targetID);
                }
                UpdateGobTrackers();
            }
        }

        #endregion Methods related to serialisation

        #region Private methods

        private Vector2 PredictPositionDecent(Gob gob)
        {
            var trip = _target.Pos - Pos;
            float distance = trip.Length();
            var unitTowardsTarget = trip / distance;
            var targetEscapeMove = unitTowardsTarget * Vector2.Dot(_target.Move, unitTowardsTarget);
            var rocketChaseMove = unitTowardsTarget * _maxSpeed;
            var relativeMove = rocketChaseMove - targetEscapeMove;
            float secondsToCollisionEstimate = distance / relativeMove.Length();
            float lookAheadSeconds = MathHelper.Clamp(secondsToCollisionEstimate, 0, 1);
            return gob.Pos + gob.Move * lookAheadSeconds;
        }

        private Vector2 PredictPositionGood(Gob gob)
        {
            // Solve the prediction problem with 3D geometry where the Z axis represents time relative to now.
            // The rocket travelling at an estimated maximum speed to all directions spans a cone.
            // The target with its current known position and movement spans a 3D line.
            // The intersection of the cone and the line represents possible collision points in space and time.
            float rocketSpeedEstimate = _maxSpeed * 0.9f;
            var rocketStart = new Vector3(Pos, 0);
            var targetStart = new Vector3(_target.Pos, 0);
            var targetDelta = new Vector3(_target.Move, 1);
            float squareCosine = 1 / (1 * 1 + rocketSpeedEstimate * rocketSpeedEstimate);
            Vector3 intersectionData1, intersectionData2;
            var intersectionType = Geometry.Intersect(rocketStart, Vector3.UnitZ, squareCosine,
                targetStart, targetDelta, out intersectionData1, out intersectionData2);
            switch (intersectionType)
            {
                case Geometry.LineConeIntersectionType.Point:
                    return new Vector2(intersectionData1.X, intersectionData1.Y);
                case Geometry.LineConeIntersectionType.Ray:
                    return new Vector2(intersectionData1.X, intersectionData1.Y);
                case Geometry.LineConeIntersectionType.Segment:
                    if (intersectionData1.Z <= intersectionData2.Z)
                        return new Vector2(intersectionData1.X, intersectionData1.Y);
                    else
                        return new Vector2(intersectionData2.X, intersectionData2.Y);
                default:
                    return gob.Pos + 1 * gob.Move;
            }
        }

        private void RotateTowards(Vector2 direction, float rotationSpeed)
        {
            float rotationGoal = AWMathHelper.Angle(direction);
            Rotation = AWMathHelper.InterpolateTowardsAngle(Rotation, rotationGoal,
                AssaultWingCore.Instance.PhysicsEngine.ApplyChange(rotationSpeed, AssaultWingCore.Instance.GameTime.ElapsedGameTime));
        }

        private void Thrust()
        {
            var forceVector = _thrustForce * AWMathHelper.GetUnitVector2(Rotation);
            AssaultWingCore.Instance.PhysicsEngine.ApplyLimitedForce(this, forceVector, _maxSpeed,
                AssaultWingCore.Instance.GameTime.ElapsedGameTime);
        }

        private Gob FindBestTarget()
        {
            var targets =
                from gob in Arena.Gobs.GameplayLayer.Gobs
                where gob.IsDamageable && !gob.Disabled && gob.Owner != Owner
                    && AWMathHelper.AbsoluteAngleDifference((gob.Pos - Pos).Angle(), Rotation) <= _findTargetAngle
                let distanceSquared = Vector2.DistanceSquared(gob.Pos, Pos)
                where distanceSquared <= _findTargetRange * _findTargetRange
                orderby distanceSquared ascending
                select gob;
            return targets.FirstOrDefault();
        }

        private void UpdateTarget()
        {
            if (AssaultWingCore.Instance.NetworkMode == AW2.Core.NetworkMode.Client) return;
            var oldTarget = _target;
            var newBestTarget = FindBestTarget();
            _target = newBestTarget ?? _target;
            UpdateGobTrackers();
            if (AssaultWingCore.Instance.NetworkMode == AW2.Core.NetworkMode.Server && _target != oldTarget)
                ForceNetworkUpdate();
        }

        private void UpdateGobTrackers()
        {
            if (_targetTracker == null && _target == null) return;
            if (_targetTracker != null && _target == _targetTracker.Gob) return;
            RemoveGobTrackers();
            CreateGobTrackers();
        }

        private void RemoveGobTrackers()
        {
            if (_targetTracker == null) return;
            Owner.RemoveGobTrackerItem(_targetTracker);
            if (_targetTracker.Gob != null && _targetTracker.Gob.Owner != null)
                _targetTracker.Gob.Owner.RemoveGobTrackerItem(_targetTracker);
            _targetTracker = null;
        }

        private void CreateGobTrackers()
        {
            if (_targetTracker != null) throw new ApplicationException("Rocket is creating a gob tracker although it has one already");
            if (_target == null) return;
            _targetTracker = new GobTrackerItem(_target, this, GobTrackerItem.ROCKET_TARGET_TEXTURE, false, true, true, true, Owner.PlayerColor);
            Owner.AddGobTrackerItem(_targetTracker);
            if (_target.Owner != null) _target.Owner.AddGobTrackerItem(_targetTracker);
        }

        #endregion Private methods
    }
}
