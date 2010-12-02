using System;
using Microsoft.Xna.Framework;
using AW2.Core;
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

        private Vector2? _hoverAroundPos;
        private Vector2 _thrustForce;

        private int HoverThrustCycleFrame { get { return Arena.FrameNumber % (int)(Game.TargetFPS * HOVER_THRUST_INTERVAL); } }
        private bool IsHoverThrusting { get { return HoverThrustCycleFrame < Game.TargetFPS * HOVER_THRUST_INTERVAL / 2; } }
        private bool IsChangingHoverThrustTargetPos { get { return HoverThrustCycleFrame == 0; } }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public FloatingBullet()
        {
            _hoverThrust = 10000;
            _attractionForce = 50000;
            _spreadingForce = 10000;
        }

        public FloatingBullet(CanonicalString typeName)
            : base(typeName)
        {
            Gravitating = false;
        }

        public override void Update()
        {
            base.Update();
            Move *= 0.97f;
            if (IsChangingHoverThrustTargetPos) SetNewTargetPos();
            if (IsHoverThrusting) Game.PhysicsEngine.ApplyForce(this, _thrustForce);
        }

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            switch (myArea.Name)
            {
                case "Magnet":
                    if (theirArea.Owner.Owner != Owner)
                        MoveTowards(theirArea.Owner.Pos, _attractionForce);
                    break;
                case "Spread":
                    if (theirArea.Owner.Owner == Owner)
                        MoveTowards(theirArea.Owner.Pos, -_spreadingForce);
                    break;
                default:
                    base.Collide(myArea, theirArea, stuck);
                    break;
            }
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                if (_hoverAroundPos.HasValue)
                    writer.WriteHalf(_hoverAroundPos.Value);
                else
                    writer.WriteHalf(new Vector2(float.NaN));
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.VaryingData) != 0)
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
            Game.PhysicsEngine.ApplyForce(this, forceVector);
            _hoverAroundPos = null;
        }

        private void SetNewTargetPos()
        {
            if (!_hoverAroundPos.HasValue) _hoverAroundPos = Pos;
            float targetAngle = MathHelper.TwoPi / (2.5f * Game.TargetFPS * HOVER_THRUST_INTERVAL) * (ID * 7 + Arena.FrameNumber);
            var targetPos = _hoverAroundPos.Value + 50 * AWMathHelper.GetUnitVector2(targetAngle);
            _thrustForce = _hoverThrust * Vector2.Normalize(targetPos - Pos);
        }
    }
}
