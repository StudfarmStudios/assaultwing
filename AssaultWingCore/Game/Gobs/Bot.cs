using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// Semi-intellectual armed flying gob.
    /// </summary>
    /// <remarks>
    /// Locks on to an enemy for a few seconds, moves toward it, and looks for nearby targets meanwhile.
    /// A nearby target may replace the locked target.
    /// </remarks>
    public class Bot : Gob
    {
        private static readonly TimeSpan MOVE_TARGET_UPDATE_INTERVAL = TimeSpan.FromSeconds(11);
        private static readonly TimeSpan AIM_TARGET_UPDATE_INTERVAL = TimeSpan.FromSeconds(1.1);

        [TypeParameter]
        private float _rotationSpeed; // radians/second
        [TypeParameter]
        private float _aimRange;
        [TypeParameter]
        private float _shootRange;
        [TypeParameter]
        private float _optimalTargetDistance;
        [TypeParameter]
        private float _thrustForce;
        [TypeParameter]
        private float _maxSpeed; // TODO: Extract the whole thruster thingy (implemented in Bot, Ship, Rocket); fields, force application, pengs.
        [TypeParameter]
        private CanonicalString _weaponName;

        private Weapon _weapon;
        private LazyProxy<int, Gob> _targetProxy;
        private PIDController _thrustController;
        private TimeSpan _nextMoveTargetUpdate;
        private TimeSpan _nextAimTargetUpdate;

        public new BotPlayer Owner { get { return (BotPlayer)base.Owner; } set { base.Owner = value; } }
        private Gob Target { get { return _targetProxy != null ? _targetProxy.GetValue() : null; } set { _targetProxy = value; } }

        /// <summary>
        /// Only for deserialization.
        /// </summary>
        public Bot()
        {
            _rotationSpeed = MathHelper.TwoPi / 10;
            _aimRange = 700;
            _shootRange = 500;
            _optimalTargetDistance = 400;
            _thrustForce = 50000;
            _maxSpeed = 150;
            _weaponName = (CanonicalString)"dummyweapontype";
        }

        public Bot(CanonicalString typeName)
            : base(typeName)
        {
            Gravitating = false;
        }

        public override void Activate()
        {
            base.Activate();
            _weapon = Weapon.Create(_weaponName);
            _weapon.AttachTo(this, ShipDevice.OwnerHandleType.PrimaryWeapon);
            Game.DataEngine.Devices.Add(_weapon);
            _thrustController = new PIDController(() => _optimalTargetDistance, () => Target == null ? 0 : Vector2.Distance(Target.Pos, Pos))
            {
                ProportionalGain = 2,
                IntegralGain = 0,
                DerivativeGain = 0,
                OutputMaxAmplitude = 200,
            };
        }

        public override void Update()
        {
            base.Update();
            UpdateMoveTarget();
            UpdateAimTarget();
            MoveAround();
            Aim();
            Shoot();
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                base.Serialize(writer, mode);
                if (mode.HasFlag(SerializationModeFlags.VaryingData))
                {
                    int targetID = Target != null ? Target.ID : Gob.INVALID_ID;
                    writer.Write((short)targetID);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if (Owner != null) Owner.SeizeBot(this);
            if (mode.HasFlag(SerializationModeFlags.VaryingData))
            {
                int targetID = reader.ReadInt16();
                _targetProxy = new LazyProxy<int, Gob>(FindGob);
                _targetProxy.SetData(targetID);
            }
        }

        private void UpdateMoveTarget()
        {
            if (Game.NetworkMode == Core.NetworkMode.Client) return;
            if (Arena.TotalTime < _nextMoveTargetUpdate) return;
            _nextMoveTargetUpdate = Arena.TotalTime + MOVE_TARGET_UPDATE_INTERVAL;
            var newTarget = TargetSelection.ChooseTarget(Game.DataEngine.Minions, this, Rotation, float.MaxValue, TargetSelection.SectorType.FullCircle, float.MaxValue, 1);
            var oldTarget = Target;
            Target = newTarget;
            if (Game.NetworkMode == Core.NetworkMode.Server && oldTarget != newTarget) ForcedNetworkUpdate = true;
        }

        private void UpdateAimTarget()
        {
            // TODO !!! Extract functionality that is common with UpdateMoveTarget().
            if (Game.NetworkMode == Core.NetworkMode.Client) return;
            if (Arena.TotalTime < _nextAimTargetUpdate) return;
            _nextAimTargetUpdate = Arena.TotalTime + AIM_TARGET_UPDATE_INTERVAL;
            var newTarget = TargetSelection.ChooseTarget(Game.DataEngine.Minions, this, Rotation, _aimRange, TargetSelection.SectorType.FullCircle, float.MaxValue);
            // If no short range target found, then continue with long range target.
            if (newTarget == null) return;
            var oldTarget = Target;
            Target = newTarget;
            if (Game.NetworkMode == Core.NetworkMode.Server && oldTarget != newTarget) ForcedNetworkUpdate = true;
        }

        private void MoveAround()
        {
            if (Target == null || Target.IsHidden) return;
            var trip = Target.Pos - Pos;
            _thrustController.Compute();
            var force = -_thrustForce * _thrustController.Output / _thrustController.OutputMaxAmplitude * Vector2.Normalize(trip);
            Game.PhysicsEngine.ApplyLimitedForce(this, force, _maxSpeed, Game.GameTime.ElapsedGameTime);
        }

        private void Aim()
        {
            if (Target == null || Target.IsHidden) return;
            var rotationStep = Game.PhysicsEngine.ApplyChange(_rotationSpeed, Game.GameTime.ElapsedGameTime);
            Rotation = AWMathHelper.InterpolateTowardsAngle(Rotation, (Target.Pos - Pos).Angle(), rotationStep);
        }

        private void Shoot()
        {
            if (Game.NetworkMode == Core.NetworkMode.Client) return;
            if (Target == null) return;
            if (Vector2.DistanceSquared(Target.Pos, Pos) > _shootRange * _shootRange) return;
            _weapon.TryFire(new UI.ControlState(1, true));
        }
    }
}
