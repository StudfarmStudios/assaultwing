using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.GobUtils;
using AW2.Graphics.Content;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;

using Point = AW2.Helpers.Geometric.Point;

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
        private const float FAN_ANGLE_SPEED_MAX = 30;
        private static IGeomPrimitive[] g_wallNavPrimitives =
        { // HACK: These primitives assume that gob scale is 0.047.
            new Circle(new Vector2(1200, 0), 200),
            new Circle(new Vector2(1200, 0).Rotate(MathHelper.TwoPi / 5), 200),
            new Circle(new Vector2(1200, 0).Rotate(MathHelper.TwoPi / 5 * 2), 200),
            new Circle(new Vector2(1200, 0).Rotate(MathHelper.TwoPi / 5 * 3), 200),
            new Circle(new Vector2(1200, 0).Rotate(MathHelper.TwoPi / 5 * 4), 200),
        };

        [TypeParameter]
        private float _rotationSpeed; // radians/second
        [TypeParameter]
        private float _aimRange;
        [TypeParameter]
        private float _shootRange;
        [TypeParameter]
        private float _optimalTargetDistance;
        [TypeParameter]
        private Thruster _thruster;
        [TypeParameter]
        private CoughEngine _coughEngine;
        [TypeParameter]
        private CanonicalString _weaponName;

        private Weapon _weapon;
        private LazyProxy<int, Gob> _targetProxy;
        private PIDController _thrustController;
        private List<TimedAction> _timedActions;
        private TargetSelector _aimTargetSelector;
        private TargetSelector _moveTargetSelector;
        private float _fanAngle; // in radians
        private float _fanAngleSpeed; // in radians/second
        private CollisionArea[] _navAreas;

        public CanonicalString WeaponName { get { return _weaponName; } }
        public new BotPlayer Owner { get { return (BotPlayer)base.Owner; } set { base.Owner = value; } }
        private Gob Target
        {
            get { return _targetProxy != null ? _targetProxy.GetValue() : null; }
            set
            {
                if (Game.NetworkMode == Core.NetworkMode.Server && Target != value) ForcedNetworkUpdate = true;
                _targetProxy = value;
            }
        }

        /// <summary>
        /// Only for deserialization.
        /// </summary>
        public Bot()
        {
            _rotationSpeed = MathHelper.TwoPi / 10;
            _aimRange = 700;
            _shootRange = 500;
            _optimalTargetDistance = 400;
            _thruster = new Thruster();
            _coughEngine = new CoughEngine();
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
            _thruster.Activate(this);
            _coughEngine.Activate(this);
            _weapon = Weapon.Create(_weaponName);
            _weapon.AttachTo(this, ShipDevice.OwnerHandleType.PrimaryWeapon);
            Game.DataEngine.Devices.Add(_weapon);
            _navAreas = g_wallNavPrimitives
                .Select(prim => new CollisionArea("", prim, this, CollisionAreaType.Receptor,
                    CollisionAreaType.Physical & ~CollisionAreaType.PhysicalMovable,
                    CollisionAreaType.None, CollisionMaterialType.Regular))
                .ToArray();
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
            }
        }

        public override void Update()
        {
            base.Update();
            foreach (var act in _timedActions) act.Update(Arena.TotalTime);
            MoveAround();
            Aim();
            Shoot();
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
            if (Owner != null) Owner.SeizeBot(this);
            if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
            {
                int targetID = reader.ReadInt16();
                _targetProxy = new LazyProxy<int, Gob>(Arena.FindGob);
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
            Target = _moveTargetSelector.ChooseTarget(Game.DataEngine.Minions, this, Rotation);
        }

        private void UpdateAimTarget()
        {
            // If no short range target found, then continue with long range target.
            Target = _aimTargetSelector.ChooseTarget(Game.DataEngine.Minions, this, Rotation) ?? Target;
        }

        private void MoveAround()
        {
            throw new NotImplementedException("Rewrite using Farseer raycasting");
#if false // !!!
            if (Target == null || Target.IsHidden) return;
            var trip = Vector2.Normalize(Target.Pos - Pos);
            _thrustController.Compute();
            var proportionalThrust = -_thrustController.Output / _thrustController.OutputMaxAmplitude;

            // Avoid walls
            var navDirs =_navAreas
                .Where(area => Arena.GetOverlappingGobs(area, area.CollidesAgainst).Any())
                .Select(area => Vector2.Normalize(Pos - area.Area.BoundingBox.Center));
            if (navDirs.Any())
            {
                trip *= proportionalThrust * 0.5f; // Avoiding walls has higher priority than reaching shooting distance.
                proportionalThrust = 1;
                foreach (var dir in navDirs) trip += dir;
            }

            _thruster.Thrust(proportionalThrust, trip);
#endif
        }

        private void Aim()
        {
            if (Target == null || Target.IsHidden) return;
            var rotationStep = Game.PhysicsEngine.ApplyChange(_rotationSpeed, Game.GameTime.ElapsedGameTime);
            var oldRotation = Rotation;
            Rotation = AWMathHelper.InterpolateTowardsAngle(Rotation, (Target.Pos - Pos).Angle(), rotationStep);
            var rotationDelta = AWMathHelper.GetAbsoluteMinimalEqualAngle(Rotation - oldRotation);
            _fanAngleSpeed = MathHelper.Clamp(_fanAngleSpeed + rotationDelta * 10, -FAN_ANGLE_SPEED_MAX, FAN_ANGLE_SPEED_MAX);
        }

        private void Shoot()
        {
            if (Game.NetworkMode == Core.NetworkMode.Client) return;
            if (Target == null) return;
            if (Vector2.DistanceSquared(Target.Pos, Pos) > _shootRange * _shootRange) return;
            _weapon.TryFire(new UI.ControlState(1, true));
        }

        private void MoveFan()
        {
            _fanAngle += _fanAngleSpeed * (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
            _fanAngleSpeed *= 0.9873f; // slow down to 10 % in 180 updates (i.e. 3 seconds)
        }
    }
}
