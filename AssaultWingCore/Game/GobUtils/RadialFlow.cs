using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers.Serialization;

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
        [TypeParameter]
        private CollisionArea _collisionArea;

        /// <summary>
        /// Time of medium flow end, in game time.
        /// </summary>
        private TimeSpan _flowEndTime;

        private Gob _radiator;

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
            _collisionArea = new CollisionArea();
        }

        public void Activate(Gob radiator, TimeSpan now)
        {
            _radiator = radiator;
            _collisionArea.Owner = radiator;
            radiator.TransformUnmovableCollisionAreas(new[] { _collisionArea });
            _flowEndTime = now + TimeSpan.FromSeconds(_flowTime);
        }

        public void Update()
        {
            foreach (var gob in _radiator.Arena.GetOverlappingGobs(_collisionArea, _collisionArea.CollidesAgainst))
                Apply(gob);
        }

        public bool IsFinished(TimeSpan now)
        {
            return now >= _flowEndTime;
        }

        private void Apply(Gob gob)
        {
            var difference = gob.Pos - _radiator.Pos;
            var differenceLength = difference.Length();
            var differenceUnit = differenceLength > 0 ? difference / differenceLength : Vector2.Zero;
            var flow = differenceUnit * _flowSpeed.Evaluate(differenceLength);
            var moveBoost = _radiator.Move == Vector2.Zero
                ? Vector2.Zero
                : Vector2.Normalize(_radiator.Move) * Vector2.Dot(_radiator.Move, differenceUnit);
            _radiator.Game.PhysicsEngine.ApplyDrag(gob, flow + moveBoost, _dragMagnitude);
        }
    }
}
