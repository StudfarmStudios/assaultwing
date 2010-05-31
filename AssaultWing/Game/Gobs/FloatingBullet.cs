using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Geometric;

namespace AW2.Game.Gobs
{
    public class FloatingBullet : Bullet
    {
        /// <summary>
        /// MovementCurve for animating mine
        /// </summary>
        private MovementCurve _movementCurve;

        /// <summary>
        /// Targetpoint for animation
        /// </summary>
        private Vector2 _targetPos;

        /// <summary>
        /// Flag if floating bullet has stopped once
        /// </summary>
        private bool _bulletStopped;

        /// <summary>
        /// Circle representing radius of randomization
        /// </summary>
        private Circle _targetCircle;

        /// This constructor is only for serialisation.
        public FloatingBullet()
        {
        }

        public FloatingBullet(CanonicalString typeName)
            : base(typeName)
        {
            _gravitating = false;
        }

        public override void Update()
        {
            base.Update();
            if (_bulletStopped)
                UpdateStoppedBullet();
            else
                UpdateMovingBullet();
        }

        public override void Serialize(AW2.Net.NetworkBinaryWriter writer, AW2.Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((bool)_bulletStopped);
                writer.WriteHalf(_targetPos);
            }
        }

        public override void Deserialize(AW2.Net.NetworkBinaryReader reader, AW2.Net.SerializationModeFlags mode, TimeSpan messageAge)
        {
            var oldPos = Pos; // HACK to avoid mine jitter on client
            base.Deserialize(reader, mode, messageAge);
            if (_bulletStopped) Pos = oldPos;
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                bool oldBulletStopped = _bulletStopped;
                _bulletStopped = reader.ReadBoolean();
                var newTargetPos = reader.ReadHalfVector2();
                if ((!oldBulletStopped && _bulletStopped) || (oldBulletStopped && newTargetPos != _targetPos))
                    SetNewTargetPos(newTargetPos);
            }
        }

        private void UpdateStoppedBullet()
        {
            Move = Vector2.Zero;
            if (_targetPos != Pos || AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                // Update the floating bullet position
                Pos = _movementCurve.Evaluate(Arena.TotalTime);
            }
            else
            {
                // Randomize next target position
                SetNewTargetPos(Geometry.GetRandomLocation(_targetCircle));
            }
        }

        private void UpdateMovingBullet()
        {
            // Slow down the floating bullet
            Move *= 0.957f;

            // When mine has nearly stopped start animating (this condition will succeed only once per floating bullet)
            if (Move.Length() < 1 && AssaultWing.Instance.NetworkMode != NetworkMode.Client)
            {
                _bulletStopped = true;
                _targetPos = Pos;
                _targetCircle = new Circle(Pos, 15);
                SetNewTargetPos(Geometry.GetRandomLocation(_targetCircle));
            }
        }

        private void SetNewTargetPos(Vector2 targetPos)
        {
            _targetPos = targetPos;
            float animationLength = RandomHelper.GetRandomFloat(1.9f, 2.6f);
            if (_movementCurve == null) _movementCurve = new MovementCurve(Pos);
            _movementCurve.SetTarget(_targetPos, Arena.TotalTime, animationLength, MovementCurve.Curvature.SlowFastSlow);
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server) ForceNetworkUpdate();
        }
    }
}
