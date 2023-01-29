using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Collisions;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Helpers.Geometric;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Flow of medium radially from a point. Medium flow affects the movement of gobs.
    /// </summary>
    [LimitedSerialization]
    public class RadialFlow
    {
        /// <summary>
        /// Speed of medium flow away from the center, measured in
        /// meters per second as a function of the distance from the
        /// center.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private Curve _flowSpeed;

        /// <summary>
        /// Time, in seconds of game time, of how long there is a medium flow away
        /// from the center.
        /// </summary>
        [TypeParameter]
        private float _flowTime;

        /// <summary>
        /// Magnitude of drag. 0 means no drag and 1 means
        /// absolute drag where gobs cannot escape the medium flow.
        /// Practical values are very small, under 0.1.
        /// </summary>
        [TypeParameter]
        private float _dragMagnitude;

        /// <summary>
        /// Area of effect of the medium flow.
        /// </summary>
        private CollisionArea _collisionArea;

        /// <summary>
        /// Time of medium flow end, in game time.
        /// </summary>
        private TimeSpan _flowEndTime;

        private Gob _radiator;

        public bool IsActive { get { return _collisionArea != null; } }
        private bool IsTimeUp { get { return Now >= _flowEndTime; } }
        private TimeSpan Now { get { return _radiator.Arena.TotalTime; } }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public RadialFlow()
        {
            _flowSpeed = new Curve();
            _flowSpeed.PreLoop = CurveLoopType.Constant;
            _flowSpeed.PostLoop = CurveLoopType.Constant;
            _flowSpeed.Keys.Add(new CurveKey(0, 6000, 0, 0, CurveContinuity.Smooth));
            _flowSpeed.Keys.Add(new CurveKey(300, 0, -1.5f, -1.5f, CurveContinuity.Smooth));
            _flowTime = 0.5f;
            _dragMagnitude = 0.003f;
        }

        public void Activate(Gob radiator)
        {
            if (IsActive) throw new ApplicationException("Cannot activate an active RadialFlow");
            _radiator = radiator;
            _flowEndTime = Now + TimeSpan.FromSeconds(_flowTime);
            var areaArea = new Circle(Vector2.Zero, _flowSpeed.Keys.Last().Position) { Density = 0 };
            _collisionArea = new CollisionArea("Flow", areaArea, radiator,
                CollisionAreaType.Flow, CollisionMaterialType.Regular);
        }

        public void Deactivate()
        {
            if (!IsActive) throw new ApplicationException("Cannot deactivate an inactive RadialFlow");
            _collisionArea.Destroy();
            _collisionArea = null;
        }

        public void Update()
        {
            if (IsActive)
            {
                foreach (var area in PhysicsHelper.GetContacting(_collisionArea)) ApplyTo(area.Owner);
                if (IsTimeUp) Deactivate();
            }
        }

        private void ApplyTo(Gob gob)
        {
            if (gob.MoveType != MoveType.Dynamic) return;
            var difference = gob.Pos - _radiator.Pos;
            var differenceUnit = difference.NormalizeOrZero();
            var flow = differenceUnit * _flowSpeed.Evaluate(difference.Length());
            var moveBoost = _radiator.Move.NormalizeOrZero() * Vector2.Dot(_radiator.Move, differenceUnit);
            PhysicsHelper.ApplyDrag(gob, flow + moveBoost, _dragMagnitude);

            // HACK to blow rockets away
            var rocket = gob as Gobs.Rocket;
            if (rocket != null)
            {
                var turnStep = (1 + rocket.TargetTurnSpeed) * (float)_radiator.Game.GameTime.ElapsedGameTime.TotalSeconds;
                rocket.Rotation = AWMathHelper.InterpolateTowardsAngle(rocket.Rotation, difference.Angle(), turnStep);
            }
        }
    }
}
