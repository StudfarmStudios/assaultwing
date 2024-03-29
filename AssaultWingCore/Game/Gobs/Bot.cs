using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Collisions;
using AW2.Game.GobUtils;
using AW2.Game.Players;
using AW2.Graphics.Content;
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
        private static readonly TimeSpan TRY_FIRE_INTERVAL = TimeSpan.FromSeconds(0.9);
        private static readonly CollisionAreaType[] WALL_TYPES = new[] { CollisionAreaType.Static };
        private static readonly CollisionAreaType[] OBSTACLE_TYPES = new[] { CollisionAreaType.Static };
        private const float FAN_ANGLE_SPEED_MAX = 30;
        private const float FAN_ANGLE_SPEED_FADEOUT = 0.9873f; // slow down to 10 % in 180 updates (i.e. 3 seconds)
        private const int WALL_SCAN_DIRS = 16;
        private const int WALL_SCAN_DIR_ROTATION = 7;
        private const float WALL_SCAN_RANGE = 100;
        private const float WALL_THRUST_FADEOUT = 0.91f;
        private const float WALL_THRUST_CUTOFF = 0.5f;

        private static readonly Vector2[] g_wallScanUnits;

        [TypeParameter]
        private float _rotationSpeed; // radians/second
        [TypeParameter]
        private float _aimRange;
        [TypeParameter]
        private float _shootRange;
        [TypeParameter]
        private float _shootAimAngleErrorMax;
        [TypeParameter]
        private bool _shootWithoutLineOfSight;
        [TypeParameter]
        private float _optimalTargetDistance;
        [TypeParameter]
        private Thruster _thruster;
        [TypeParameter]
        private float _movementDamping;
        [TypeParameter]
        private CoughEngine _coughEngine;
        [TypeParameter]
        private CanonicalString _weaponName;

        private ShipDevice _weapon;
        private LazyProxy<int, Gob> _targetProxy;
        private PIDController _thrustController;
        private List<TimedAction> _timedActions;
        private TargetSelector _aimTargetSelector;
        private TargetSelector _moveTargetSelector;
        private float _fanAngle; // in radians
        private float _fanAngleSpeed; // in radians/second
        private int _wallScanDir;
        private float _wallThrust;
        private Vector2 _wallTripAccumulator;

        public override bool IsDamageable { get { return true; } }
        public CanonicalString WeaponName { get { return _weaponName; } }
        private BotPlayer BotOwner { get { return Owner as BotPlayer; } }
        private Gob Target
        {
            get { return _targetProxy != null ? _targetProxy.GetValue() : null; }
            set
            {
                if (Game.NetworkMode == Core.NetworkMode.Server && Target != value) ForcedNetworkUpdate = true;
                _targetProxy = value;
            }
        }

        static Bot()
        {
            g_wallScanUnits = Enumerable.Range(0, WALL_SCAN_DIRS)
                .Select(dir => Vector2.UnitX.Rotate(dir * MathHelper.TwoPi / WALL_SCAN_DIRS))
                .ToArray();
        }

        /// <summary>
        /// Only for deserialization.
        /// </summary>
        public Bot()
        {
            _rotationSpeed = MathHelper.TwoPi / 10;
            _aimRange = 700;
            _shootRange = 500;
            _shootAimAngleErrorMax = 0.20f;
            _shootWithoutLineOfSight = false;
            _optimalTargetDistance = 400;
            _thruster = new Thruster();
            _movementDamping = 0;
            _coughEngine = new CoughEngine();
            _weaponName = (CanonicalString)"dummyweapontype";
        }

        public Bot(CanonicalString typeName)
            : base(typeName)
        {
            DampAngularVelocity = true;
            Gravitating = false;
            IsModelBonesMoving = true;
        }

        public override void Activate()
        {
            base.Activate();
            _thruster.Activate(this);
            _coughEngine.Activate(this);
            _weapon = ShipDevice.Create(Game, _weaponName);
            _weapon.AttachTo(this, ShipDevice.OwnerHandleType.PrimaryWeapon);
            Game.PostFrameLogicEngine.DoOnce += () => Game.DataEngine.Devices.Add(_weapon);
            _thrustController = new PIDController(() => _optimalTargetDistance, () => Target == null ? 0 : Vector2.Distance(Target.Pos, Pos))
            {
                ProportionalGain = 2,
                IntegralGain = 0,
                DerivativeGain = 0,
                OutputMaxAmplitude = 200,
            };
            _aimTargetSelector = new TargetSelector(_aimRange)
            {
                MaxAngle = MathHelper.Pi,
                FriendlyWeight = float.MaxValue,
                AngleWeight = 0.5f,
            };
            _moveTargetSelector = new TargetSelector(float.MaxValue)
            {
                MaxAngle = MathHelper.Pi,
                FriendlyWeight = float.MaxValue,
                AngleWeight = 0,
            };
            _timedActions = new List<TimedAction>();
            if (Game.NetworkMode != Core.NetworkMode.Client)
            {
                _timedActions.Add(new TimedAction(MOVE_TARGET_UPDATE_INTERVAL, UpdateMoveTarget));
                _timedActions.Add(new TimedAction(AIM_TARGET_UPDATE_INTERVAL, UpdateAimTarget));
                _timedActions.Add(new TimedAction(TRY_FIRE_INTERVAL, Shoot));
            }
            Body.LinearDamping = _movementDamping;
        }

        public override void Update()
        {
            base.Update();
            if (Target != null && Target.Dead) Target = null;
            foreach (var act in _timedActions) act.Update(Arena.TotalTime);
            MoveAround();
            Aim();
            _thruster.Update();
            _coughEngine.Update();
            MoveFan();
        }

        public override void Dispose()
        {
            _thruster.Dispose();
            base.Dispose();
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                base.Serialize(writer, mode);
                if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
                {
                    int targetID = Target != null ? Target.ID : Gob.INVALID_ID;
                    writer.Write((short)targetID);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if (BotOwner != null) BotOwner.SeizeBot(this);
            if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
            {
                int targetID = reader.ReadInt16();
                _targetProxy = new LazyProxy<int, Gob>(FindGob);
                _targetProxy.SetData(targetID);
            }
        }

        protected override void CopyAbsoluteBoneTransformsTo(ModelGeometry skeleton, Matrix[] transforms)
        {
            if (transforms == null) throw new ArgumentNullException("Null transformation matrix array");
            if (transforms.Length < skeleton.Bones.Length) throw new ArgumentException("Too short transformation matrix array");
            foreach (var bone in skeleton.Bones)
            {
                if (bone.Parent == null)
                    transforms[bone.Index] = bone.Transform;
                else
                {
                    if (bone.Parent.Index >= bone.Index) throw new Exception("Unexpected situation: bone parent doesn't precede the bone itself");
                    var extraTransform = bone.Name.StartsWith("fan")
                        ? Matrix.CreateRotationZ(_fanAngle)
                        : Matrix.Identity;
                    transforms[bone.Index] = extraTransform * bone.Transform * transforms[bone.Parent.Index];
                }
            }
        }

        private void UpdateMoveTarget()
        {
            // Keep any chosen aim target.
            Target = Target ?? _moveTargetSelector.ChooseTarget(Arena.PotentialTargets, this, Rotation);
        }

        private void UpdateAimTarget()
        {
            // If no short range target found, then continue with long range target.
            Target = _aimTargetSelector.ChooseTarget(Arena.PotentialTargets, this, Rotation, IsInLineOfSight) ?? Target;
        }

        private void MoveAround()
        {
            var targetThrust = GetTargetThrust();
            var wallThrust = GetWallThrust();
            var combinedThrust = CombineThrusts(targetThrust, wallThrust);
            _thruster.Thrust(MathHelper.Clamp(combinedThrust.Length(), 0, 1), combinedThrust);
        }

        private Vector2 CombineThrusts(Vector2 targetThrust, Vector2 wallThrust)
        {
            // Avoiding walls has higher priority than reaching optimal shooting distance.
            return wallThrust == Vector2.Zero
                ? targetThrust
                : wallThrust + targetThrust.ProjectOnto(wallThrust.Rotate90());
        }

        /// <summary>
        /// Returns thrust direction vector with amplitude between 0 and 1 denoting thrust force.
        /// </summary>
        private Vector2 GetTargetThrust()
        {
            if (Target == null || Target.IsHidden || Target.Dead) return Vector2.Zero;
            var trip = (Target.Pos - Pos).NormalizeOrZero();
            _thrustController.Compute();
            var proportionalThrust = -_thrustController.Output / _thrustController.OutputMaxAmplitude;
            return proportionalThrust * trip;
        }

        /// <summary>
        /// Returns thrust direction vector with amplitude between 0 and 1 denoting thrust force.
        /// </summary>
        private Vector2 GetWallThrust()
        {
            _wallScanDir = (_wallScanDir + WALL_SCAN_DIR_ROTATION) % WALL_SCAN_DIRS;
            var distance = Arena.GetDistanceToClosest(Pos, Pos + WALL_SCAN_RANGE * g_wallScanUnits[_wallScanDir],
                area => area.Owner.MoveType != MoveType.Dynamic && area.Type.IsPhysical(), WALL_TYPES);
            _wallTripAccumulator *= WALL_THRUST_FADEOUT;
            _wallThrust *= WALL_THRUST_FADEOUT;
            if (distance.HasValue)
            {
                _wallThrust = 1;
                _wallTripAccumulator -= g_wallScanUnits[_wallScanDir];
            }
            if (_wallThrust <= WALL_THRUST_CUTOFF) _wallThrust = 0;
            return _wallThrust * _wallTripAccumulator.NormalizeOrZero();
        }

        private void Aim()
        {
            if (Target == null || Target.IsHidden) return;
            RotationSpeed = AWMathHelper.GetAngleSpeedTowards(Rotation, (Target.Pos - Pos).Angle(), _rotationSpeed, AW2.Core.AssaultWingCore.TargetElapsedTime);
            var rotationDelta = RotationSpeed * (float)AW2.Core.AssaultWingCore.TargetElapsedTime.TotalSeconds;
            _fanAngleSpeed = MathHelper.Clamp(_fanAngleSpeed + rotationDelta * 10, -FAN_ANGLE_SPEED_MAX, FAN_ANGLE_SPEED_MAX);
        }

        private void Shoot()
        {
            if (Game.NetworkMode == Core.NetworkMode.Client) return;
            if (Target == null) return;
            var aimErrorAngle = AWMathHelper.AbsoluteAngleDifference(Rotation, (Target.Pos - Pos).Angle());
            if (aimErrorAngle > _shootAimAngleErrorMax) return;
            if (IsInLineOfSight(Target)) _weapon.TryFire(new UI.ControlState(1, true));
        }

        private bool IsInLineOfSight(Gob target)
        {
            var targetDistanceSquared = Vector2.DistanceSquared(target.Pos, Pos);
            if (targetDistanceSquared > _shootRange * _shootRange) return false;
            if (_shootWithoutLineOfSight) return true;
            var obstacleDistance = Arena.GetDistanceToClosest(Pos, target.Pos, area => area.Owner.MoveType != GobUtils.MoveType.Dynamic, OBSTACLE_TYPES);
            return !obstacleDistance.HasValue || obstacleDistance * obstacleDistance > targetDistanceSquared;
        }

        private void MoveFan()
        {
            _fanAngle += _fanAngleSpeed * (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
            _fanAngleSpeed *= FAN_ANGLE_SPEED_FADEOUT;
        }
    }
}
