using System;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
    /// <summary>
    /// Smooth movement curve towards a target in 2D space.
    /// </summary>
    public class MovementCurve
    {
        /// <summary>
        /// Type of curvature of a <see cref="MovementCurve"/> instance.
        /// </summary>
        public enum Curvature
        {
            /// <summary>
            /// Movement starts slowly, speeds up, and slows down again.
            /// </summary>
            SlowFastSlow,

            /// <summary>
            /// Movement starts slowly, speeds up, and stops suddenly.
            /// </summary>
            SlowFast,

            /// <summary>
            /// Movement starts instantly, and slows down to a stop.
            /// </summary>
            FastSlow,

            /// <summary>
            /// Movement is linear, constant speed.
            /// </summary>
            Linear,

            /// <summary>
            /// Movement is almost linear, with mild acceleration and deceleration at the ends.
            /// </summary>
            AlmostLinear,
        }

        private static readonly Curve SLOW_FAST_SLOW_CURVE, SLOW_FAST_CURVE, FAST_SLOW_CURVE, LINEAR_CURVE, ALMOST_LINEAR_CURVE;
        private Curve _curve;
        private TimeSpan _time1;
        private float _durationSeconds;

        public Vector2 StartPos { get; private set; }
        public Vector2 EndPos { get; private set; }

        static MovementCurve()
        {
            SLOW_FAST_SLOW_CURVE = new Curve();
            SLOW_FAST_SLOW_CURVE.Keys.Add(new CurveKey(0, 0, 0, 0, CurveContinuity.Smooth));
            SLOW_FAST_SLOW_CURVE.Keys.Add(new CurveKey(0.37f, 0.18f, 0.481587321f, 0.338412672f, CurveContinuity.Smooth));
            SLOW_FAST_SLOW_CURVE.Keys.Add(new CurveKey(0.63f, 0.82f, 0.338412672f, 0.481587321f, CurveContinuity.Smooth));
            SLOW_FAST_SLOW_CURVE.Keys.Add(new CurveKey(1, 1, 0, 0, CurveContinuity.Smooth));
            SLOW_FAST_SLOW_CURVE.PreLoop = SLOW_FAST_SLOW_CURVE.PostLoop = CurveLoopType.Constant;
            SLOW_FAST_CURVE = new Curve();
            SLOW_FAST_CURVE.Keys.Add(new CurveKey(0, 0, 0, 0, CurveContinuity.Smooth));
            SLOW_FAST_CURVE.Keys.Add(new CurveKey(0.468407154f, 0.0652343f, 0.201716289f, 0.121062361f, CurveContinuity.Smooth));
            SLOW_FAST_CURVE.Keys.Add(new CurveKey(0.749527067f, 0.322778642f, 0.494328052f, 0.440437645f, CurveContinuity.Smooth));
            SLOW_FAST_CURVE.Keys.Add(new CurveKey(1, 1, 0.677221358f, 0, CurveContinuity.Smooth));
            SLOW_FAST_CURVE.PreLoop = SLOW_FAST_CURVE.PostLoop = CurveLoopType.Constant;
            FAST_SLOW_CURVE = new Curve();
            FAST_SLOW_CURVE.Keys.Add(new CurveKey(0, 0, 0, 0.677221358f, CurveContinuity.Smooth));
            FAST_SLOW_CURVE.Keys.Add(new CurveKey(0.250472933f, 0.677221358f, 0.440437645f, 0.494328052f, CurveContinuity.Smooth));
            FAST_SLOW_CURVE.Keys.Add(new CurveKey(0.531592867f, 0.9347657f, 0.121062361f, 0.201716289f, CurveContinuity.Smooth));
            FAST_SLOW_CURVE.Keys.Add(new CurveKey(1, 1, 0, 0, CurveContinuity.Smooth));
            FAST_SLOW_CURVE.PreLoop = FAST_SLOW_CURVE.PostLoop = CurveLoopType.Constant;
            LINEAR_CURVE = new Curve();
            LINEAR_CURVE.Keys.Add(new CurveKey(0, 0));
            LINEAR_CURVE.Keys.Add(new CurveKey(1, 1));
            LINEAR_CURVE.ComputeTangents(CurveTangent.Linear);
            LINEAR_CURVE.PreLoop = LINEAR_CURVE.PostLoop = CurveLoopType.Constant;
            ALMOST_LINEAR_CURVE = new Curve();
            ALMOST_LINEAR_CURVE.Keys.Add(new CurveKey(0, 0, 0, 0.25f, CurveContinuity.Smooth));
            ALMOST_LINEAR_CURVE.Keys.Add(new CurveKey(1, 1, 0.25f, 0, CurveContinuity.Smooth));
            ALMOST_LINEAR_CURVE.PreLoop = LINEAR_CURVE.PostLoop = CurveLoopType.Constant;
        }

        /// <summary>
        /// Creates a <see cref="MovementCurve"/>.
        /// </summary>
        public MovementCurve(Vector2 startPos)
        {
            SetTarget(startPos, startPos, TimeSpan.Zero, 1, Curvature.SlowFastSlow);
        }

        /// <summary>
        /// Sets a new target for the movement.
        /// </summary>
        public void SetTarget(Vector2 finishPos, TimeSpan now, float durationSeconds, Curvature type)
        {
            SetTarget(Evaluate(now), finishPos, now, durationSeconds, type);
        }

        /// <summary>
        /// Evaluates the current position on the movement curve.
        /// </summary>
        public Vector2 Evaluate(TimeSpan now)
        {
            float t = (float)(now - _time1).TotalSeconds / _durationSeconds;
            return Vector2.Lerp(StartPos, EndPos, _curve.Evaluate(t));
        }

        private void SetTarget(Vector2 startPos, Vector2 finishPos, TimeSpan now, float durationSeconds, Curvature type)
        {
            StartPos = startPos;
            EndPos = finishPos;
            _time1 = now;
            _durationSeconds = durationSeconds;
            switch (type)
            {
                case Curvature.SlowFastSlow: _curve = SLOW_FAST_SLOW_CURVE; break;
                case Curvature.SlowFast: _curve = SLOW_FAST_CURVE; break;
                case Curvature.FastSlow: _curve = FAST_SLOW_CURVE; break;
                case Curvature.Linear: _curve = LINEAR_CURVE; break;
                case Curvature.AlmostLinear: _curve = ALMOST_LINEAR_CURVE; break;
                default: throw new ApplicationException("Unexpected curvature " + type);
            }
        }
    }
}
