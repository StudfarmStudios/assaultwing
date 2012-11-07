using System;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Collisions;
using AW2.Game.GobUtils;
using AW2.Game.Players;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
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
        private const float TURN_LIMIT = MathHelper.PiOver4;

        /// <summary>
        /// Amount of damage to inflict on impact with a damageable gob.
        /// </summary>
        [TypeParameter]
        private float _impactDamage;

        [TypeParameter]
        private Thruster _thruster;

        /// <summary>
        /// Duration of thrust, measured in seconds.
        /// </summary>
        [TypeParameter]
        private float _thrustDuration;

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

        private TargetSelector _targetSelector;
        private TimeSpan _nextFindTarget;
        private GobTrackerItem _targetTracker;
        private LazyProxy<int, Gob> _targetProxy;

        #endregion Rocket fields

        public float TargetTurnSpeed { get { return _targetTurnSpeed; } }
        public bool IsThrusting { get { return Arena.TotalTime < _thrustEndTime; } }
        public Player PlayerOwner { get { return Owner as Player; } }
        private Gob Target { get { return _targetProxy != null ? _targetProxy.GetValue() : null; } set { _targetProxy = value; } }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Rocket()
        {
            _impactDamage = 100;
            _thruster = new Thruster();
            _thrustDuration = 2;
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
            _thruster.Activate(this);
            _targetSelector = new TargetSelector(_findTargetRange) { AngleWeight = 5 };
            _thrustEndTime = Arena.TotalTime + TimeSpan.FromSeconds(_thrustDuration);
            // Avoid choosing the initial target on the first frame. This helps the case
            // where the rocket was shot from the owner's position (when the owner doesn't
            // have weapon barrels marked in its 3D model). Otherwise the rocket will
            // target the owner because it's so close.
            _nextFindTarget = Arena.TotalTime + TimeSpan.FromSeconds(0.1);
        }

        public override void Update()
        {
            CheckLoseTarget();
            if (IsThrusting)
            {
                UpdateTarget();
                if (Target != null)
                {
                    var predictedTargetPos = PredictPositionDecent(Target);
                    RotateTowards(predictedTargetPos - Pos, _targetTurnSpeed);
                }
                _thruster.Thrust(1);
            }
            else
            {
                RotateTowards(Move, _fallTurnSpeed);
                if (_targetTracker != null)
                    RemoveGobTrackers();
            }
            base.Update();
            _thruster.Update();
            UpdateGobTrackers();
        }

        public override bool CollideIrreversible(CollisionArea myArea, CollisionArea theirArea)
        {
            if (!theirArea.Type.IsPhysical()) return false;
            if (theirArea.Owner.IsDamageable)
            {
                theirArea.Owner.InflictDamage(_impactDamage, new DamageInfo(this));
                Game.Stats.SendHit(this, theirArea.Owner);
            }
            Die();
            return true;
        }

        public override void Dispose()
        {
            RemoveGobTrackers();
            _thruster.Dispose();
            base.Dispose();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                base.Serialize(writer, mode);
                if ((mode & SerializationModeFlags.VaryingDataFromServer) != 0)
                {
                    int targetID = Target != null ? Target.ID : Gob.INVALID_ID;
                    writer.Write((short)targetID);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.VaryingDataFromServer) != 0)
            {
                int targetID = reader.ReadInt16();
                _targetProxy = new LazyProxy<int, Gob>(FindGob);
                _targetProxy.SetData(targetID);
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
            var rocketChaseMove = unitTowardsTarget * _thruster.MaxSpeed;
            var relativeMove = rocketChaseMove - targetEscapeMove;
            float secondsToCollisionEstimate = distance / relativeMove.Length();
            float lookAheadSeconds = MathHelper.Clamp(secondsToCollisionEstimate, 0, 1);
            return gob.Pos + gob.Move * lookAheadSeconds;
        }

        private void RotateTowards(Vector2 direction, float rotationSpeed)
        {
            // Rotation is limited by rocket movement so that the rocket can only turn
            // in a small angle from the direction it is currently moving towards.
            var rotationGoal = direction.Angle();
            var moveDirection = Move.Angle();
            var rotationLimitedByMove = rotationGoal.ClampAngle(moveDirection - TURN_LIMIT, moveDirection + TURN_LIMIT);
            var elapsedSeconds = (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
            Rotation = AWMathHelper.InterpolateTowardsAngle(Rotation, rotationLimitedByMove, rotationSpeed * elapsedSeconds);
        }

        private void CheckLoseTarget()
        {
            if (Target == null) return;
            if (Target.Dead || Target.IsHidden) Target = null;
        }

        private void UpdateTarget()
        {
            if (_nextFindTarget > Arena.TotalTime) return;
            _nextFindTarget = Arena.TotalTime + FIND_TARGET_INTERVAL;
            var oldTarget = Target;
            var newBestTarget = _targetSelector.ChooseTarget(Game.DataEngine.Minions, this, Rotation);
            if (newBestTarget != null &&
                (newBestTarget.Owner == null || IsFriend(newBestTarget)) &&
                RandomHelper.GetRandomFloat() < 0.9)
                newBestTarget = null;
            if (newBestTarget != null) Target = newBestTarget;
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
            if (PlayerOwner != null) PlayerOwner.GobTrackerItems.Remove(_targetTracker);
            if (_targetTracker.Gob != null)
            {
                var targetPlayerOwner = _targetTracker.Gob.Owner as Player;
                if (targetPlayerOwner != null) targetPlayerOwner.GobTrackerItems.Remove(_targetTracker);
            }
            _targetTracker = null;
        }

        private void CreateGobTrackers()
        {
            if (_targetTracker != null) throw new ApplicationException("Rocket is creating a gob tracker although it has one already");
            if (Target == null) return;
            _targetTracker = new GobTrackerItem(Target, this, "gui_tracker_rockettarget")
            {
                StickToBorders = false,
                ShowWhileTargetOnScreen = true,
            };
            if (PlayerOwner != null) PlayerOwner.GobTrackerItems.Add(_targetTracker);
            var targetPlayerOwner = Target.Owner as Player;
            if (targetPlayerOwner != null) targetPlayerOwner.GobTrackerItems.Add(_targetTracker);
        }

        #endregion Private methods
    }
}
