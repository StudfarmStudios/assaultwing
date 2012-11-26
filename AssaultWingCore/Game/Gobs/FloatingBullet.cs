using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Collisions;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    public class FloatingBullet : Bullet
    {
        private const float HOVER_THRUST_INTERVAL = 2f; // in seconds game time

        /// <summary>
        /// Amplitude of bullet thrust when hovering around, measured in Newtons.
        /// </summary>
        [TypeParameter]
        private float _hoverThrust;

        /// <summary>
        /// Amplitude of attraction force towards nearby enemy targets.
        /// </summary>
        [TypeParameter]
        private float _attractionForce;

        /// <summary>
        /// Amplitude of repulsion force away from nearby friendly mines.
        /// </summary>
        [TypeParameter]
        private float _spreadingForce;

        /// <summary>
        /// Damping factor of linear movement.
        /// </summary>
        [TypeParameter]
        private float _movementDamping;

        /// <summary>
        /// Damping factor of angular movement.
        /// </summary>
        [TypeParameter]
        private float _rotationDamping;

        [TypeParameter]
        private string _hitSound;
        [TypeParameter, ShallowCopy]
        private Curve _enemyDistanceToAlpha;

        private Vector2? _hoverAroundPos;
        private Vector2 _thrustForce;
        private CollisionArea _magnetArea;
        private CollisionArea _spreadArea;

        public override bool IsDamageable { get { return true; } }
        private int HoverThrustCycleFrame { get { return Arena.FrameNumber % (int)(AssaultWingCore.TargetFPS * HOVER_THRUST_INTERVAL); } }
        private bool IsHoverThrusting { get { return HoverThrustCycleFrame < AssaultWingCore.TargetFPS * HOVER_THRUST_INTERVAL / 2; } }
        private bool IsChangingHoverThrustTargetPos { get { return HoverThrustCycleFrame == 0; } }

        private bool IsAvoidable(Gob gob) { return IsFriend(gob) && gob is FloatingBullet; }
        private bool IsReachable(Gob gob) { return !IsFriend(gob) && !gob.IsHidden && Arena.PotentialTargets.Contains(gob); }
        private bool IsExplodable(CollisionArea area)
        {
            if (IsFriend(area.Owner)) return false;
            if (!area.Owner.IsDamageable) return false;
            if (!area.Type.IsPhysical()) return false;
            if (area.Owner.MaxDamageLevel <= 100 && area.Owner.MoveType == GobUtils.MoveType.Dynamic) return false;
            return true;
        }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public FloatingBullet()
        {
            _hoverThrust = 10000;
            _attractionForce = 50000;
            _spreadingForce = 10000;
            _movementDamping = 0.95f;
            _rotationDamping = 0.97f;
            _hitSound = "";
            _enemyDistanceToAlpha = new Curve();
            _enemyDistanceToAlpha.PreLoop = CurveLoopType.Constant;
            _enemyDistanceToAlpha.PostLoop = CurveLoopType.Constant;
            _enemyDistanceToAlpha.Keys.Add(new CurveKey(0, 1));
            _enemyDistanceToAlpha.Keys.Add(new CurveKey(200, 0));
            _enemyDistanceToAlpha.ComputeTangents(CurveTangent.Linear);
        }

        public FloatingBullet(CanonicalString typeName)
            : base(typeName)
        {
            Gravitating = false;
        }

        public override void Activate()
        {
            base.Activate();
            IsHiding = true;
            Body.LinearDamping = _movementDamping;
            Body.AngularDamping = _rotationDamping;
            _magnetArea = CollisionAreas.First(area => area.Name == "Magnet");
            _spreadArea = CollisionAreas.First(area => area.Name == "Spread");
        }

        public override void Update()
        {
            base.Update();
            if (IsChangingHoverThrustTargetPos) SetNewTargetPos();
            if (IsHoverThrusting) PhysicsHelper.ApplyForce(this, _thrustForce);
            // FIXME !!! Computing alpha and magnet from Arena.PotentialTargets each frame may be too slow.
            // !!! Instead, choose a target, like, twice a second and compute alpha and magnet by that.
            Alpha = Arena.PotentialTargets
                .Where(gob => !IsFriend(gob) && !gob.IsHidden)
                .Select(gob => Vector2.Distance(Pos, gob.Pos))
                .Select(_enemyDistanceToAlpha.Evaluate)
                .DefaultIfEmpty(0)
                .Max();
            foreach (var gob in PhysicsHelper.GetContacting(_magnetArea).Select(area => area.Owner))
                if (IsReachable(gob)) MoveTowards(gob.Pos, _attractionForce);
            foreach (var gob in PhysicsHelper.GetContacting(_spreadArea).Select(area => area.Owner))
                if (IsAvoidable(gob)) MoveTowards(gob.Pos, -_spreadingForce);
        }

        public override bool CollideIrreversible(CollisionArea myArea, CollisionArea theirArea)
        {
            if (myArea.Name == "Magnet" || myArea.Name == "Spread") return false;
            if (!IsExplodable(theirArea)) return false;
            var hasHitSound = _hitSound != "";
            if (hasHitSound) Game.SoundEngine.PlaySound(_hitSound, this);
            var baseResult = base.CollideIrreversible(myArea, theirArea);
            return hasHitSound || baseResult;
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.Serialize(writer, mode);
                if ((mode & SerializationModeFlags.VaryingDataFromServer) != 0)
                {
                    if (_hoverAroundPos.HasValue)
                        writer.WriteHalf(_hoverAroundPos.Value);
                    else
                        writer.WriteHalf(new Vector2(float.NaN));
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.VaryingDataFromServer) != 0)
            {
                var maybeHoverAroundPos = reader.ReadHalfVector2();
                if (float.IsNaN(maybeHoverAroundPos.X))
                    _hoverAroundPos = null;
                else
                    _hoverAroundPos = maybeHoverAroundPos;
            }
        }

        private void MoveTowards(Vector2 target, float force)
        {
            var forceVector = force * Vector2.Normalize(target - Pos);
            PhysicsHelper.ApplyForce(this, forceVector);
            _hoverAroundPos = null;
        }

        private void SetNewTargetPos()
        {
            if (!_hoverAroundPos.HasValue) _hoverAroundPos = Pos;
            float targetAngle = MathHelper.TwoPi / (2.5f * AssaultWingCore.TargetFPS * HOVER_THRUST_INTERVAL) * (ID * 7 + Arena.FrameNumber);
            var targetPos = _hoverAroundPos.Value + 50 * AWMathHelper.GetUnitVector2(targetAngle);
            _thrustForce = _hoverThrust * Vector2.Normalize(targetPos - Pos);
        }
    }
}
