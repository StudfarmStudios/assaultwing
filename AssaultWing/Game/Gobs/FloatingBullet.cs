using System;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Helpers;
using AW2.Helpers.Geometric;

namespace AW2.Game.Gobs
{
    public class FloatingBullet : Bullet
    {
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

        private Circle _targetCircle;
        private Vector2 _thrustForce;
        private float? _thrustSeconds; // if not null, thrust time to send from server to clients
        private TimeSpan _thrustEndGameTime;

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
            _gravitating = false;
        }

        public override void Update()
        {
            base.Update();
            if (_thrustEndGameTime < AssaultWing.Instance.DataEngine.ArenaTotalTime)
            {
                Move *= 0.957f;
                if (AssaultWing.Instance.NetworkMode != NetworkMode.Client && Move.LengthSquared() < 1 * 1)
                    RandomizeNewTargetPos();
            }
            else
            {
                AssaultWing.Instance.PhysicsEngine.ApplyForce(this, _thrustForce);
            }
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

        public override void Serialize(AW2.Net.NetworkBinaryWriter writer, AW2.Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                if (!_thrustSeconds.HasValue)
                    writer.Write((bool)false);
                else
                {
                    writer.Write((bool)true);
                    writer.Write((Half)_thrustSeconds.Value);
                    writer.WriteHalf(_thrustForce);
                    _thrustSeconds = null;
                }
            }
        }

        public override void Deserialize(AW2.Net.NetworkBinaryReader reader, AW2.Net.SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                bool movementChanged = reader.ReadBoolean();
                if (movementChanged)
                {
                    float thrustSeconds = reader.ReadHalf();
                    _thrustEndGameTime = AssaultWing.Instance.DataEngine.ArenaTotalTime + TimeSpan.FromSeconds(thrustSeconds)
                        - AssaultWing.Instance.TargetElapsedTime.Multiply(framesAgo);
                    _thrustForce = reader.ReadHalfVector2();
                }
            }
        }

        private void MoveTowards(Vector2 target, float force)
        {
            var forceVector = force * Vector2.Normalize(target - Pos);
            AssaultWing.Instance.PhysicsEngine.ApplyForce(this, forceVector);
            _targetCircle = null;
        }

        private void RandomizeNewTargetPos()
        {
            if (_targetCircle == null) _targetCircle = new Circle(Pos, 15);
            var targetPos = Geometry.GetRandomLocation(_targetCircle);
            _thrustForce = _hoverThrust * Vector2.Normalize(targetPos - Pos);
            _thrustSeconds = RandomHelper.GetRandomFloat(1.2f, 1.9f);
            _thrustEndGameTime = AssaultWing.Instance.DataEngine.ArenaTotalTime + TimeSpan.FromSeconds(_thrustSeconds.Value);
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server) ForceNetworkUpdate();
        }
    }
}
