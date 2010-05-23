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
        /// Floating bullet original position
        /// </summary>
        private Vector2 _originalPos;

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

            // Slow down the floating bullet if it hasn't stopped before
            if (!_bulletStopped)
            {
                Move *= 0.957f;
            }

            // When mine has nearly stopped start animating (this condition will succeed only once per floating bullet)
            if (!_bulletStopped && Move.Length() < 1)
            {
                _bulletStopped = true;
                _targetPos = Pos;
                _originalPos = Pos;
                _movementCurve = new MovementCurve(Pos);
                _targetCircle = new Circle(_originalPos, 15);
            }

            // Set movement vector to zero always when floating bullet has stopped
            if (_bulletStopped)
            {
                Move = Vector2.Zero;
            }

            // If floating bullet has stopped and current target position is same than current position randomize next target
            if (_bulletStopped && _targetPos == Pos)
            {
                _targetPos = Geometry.GetRandomLocation(_targetCircle);
                float animationLength = RandomHelper.GetRandomFloat(1.9f, 2.6f);
                _movementCurve.SetTarget(_targetPos, Arena.TotalTime, animationLength, MovementCurve.Curvature.SlowFastSlow);
            }

            // If floating bullet is stopped and current target positions is not the same than current position update the floating bullet position
            if (_bulletStopped && _targetPos != Pos)
            {
                Pos = _movementCurve.Evaluate(Arena.TotalTime);
            }
        }
    }
}
