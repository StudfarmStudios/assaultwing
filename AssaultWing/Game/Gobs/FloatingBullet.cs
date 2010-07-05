﻿using System;
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
        /// Amplitude of attraction force towards targets that overlap the "Magnet" receptor.
        /// </summary>
        [TypeParameter]
        private float _attractionForce;

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
            if (myArea.Name == "Magnet")
            {
                if (theirArea.Owner.Owner != Owner)
                {
                    var forceVector = _attractionForce * Vector2.Normalize(theirArea.Owner.Pos - Pos);
                    AssaultWing.Instance.PhysicsEngine.ApplyForce(this, forceVector);
                    _targetCircle = null;
                }
            }
            else
                base.Collide(myArea, theirArea, stuck);
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

        public override void Deserialize(AW2.Net.NetworkBinaryReader reader, AW2.Net.SerializationModeFlags mode, TimeSpan messageAge)
        {
            base.Deserialize(reader, mode, messageAge);
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                bool movementChanged = reader.ReadBoolean();
                if (movementChanged)
                {
                    float thrustSeconds = reader.ReadHalf();
                    _thrustEndGameTime = AssaultWing.Instance.DataEngine.ArenaTotalTime + TimeSpan.FromSeconds(thrustSeconds) - messageAge;
                    _thrustForce = reader.ReadHalfVector2();
                }
            }
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
