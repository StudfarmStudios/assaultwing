using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        }

        static readonly Curve slowFastSlowCurve, slowFastCurve, fastSlowCurve, linearCurve;
        Curve curve;
        TimeSpan time1;
        Vector2 pos1, pos2;
        float durationSeconds;

        static MovementCurve()
        {
            slowFastSlowCurve = new Curve();
            slowFastSlowCurve.Keys.Add(new CurveKey(0, 0, 0, 0, CurveContinuity.Smooth));
            slowFastSlowCurve.Keys.Add(new CurveKey(0.37f, 0.18f, 0.481587321f, 0.338412672f, CurveContinuity.Smooth));
            slowFastSlowCurve.Keys.Add(new CurveKey(0.63f, 0.82f, 0.338412672f, 0.481587321f, CurveContinuity.Smooth));
            slowFastSlowCurve.Keys.Add(new CurveKey(1, 1, 0, 0, CurveContinuity.Smooth));
            slowFastSlowCurve.PreLoop = slowFastSlowCurve.PostLoop = CurveLoopType.Constant;
            slowFastCurve = new Curve();
            slowFastCurve.Keys.Add(new CurveKey(0, 0, 0, 0, CurveContinuity.Smooth));
            slowFastCurve.Keys.Add(new CurveKey(0.468407154f, 0.0652343f, 0.201716289f, 0.121062361f, CurveContinuity.Smooth));
            slowFastCurve.Keys.Add(new CurveKey(0.749527067f, 0.322778642f, 0.494328052f, 0.440437645f, CurveContinuity.Smooth));
            slowFastCurve.Keys.Add(new CurveKey(1, 1, 0.677221358f, 0, CurveContinuity.Smooth));
            slowFastCurve.PreLoop = slowFastCurve.PostLoop = CurveLoopType.Constant;
            fastSlowCurve = new Curve();
            fastSlowCurve.Keys.Add(new CurveKey(0, 0, 0, 0.677221358f, CurveContinuity.Smooth));
            fastSlowCurve.Keys.Add(new CurveKey(0.250472933f, 0.677221358f, 0.440437645f, 0.494328052f, CurveContinuity.Smooth));
            fastSlowCurve.Keys.Add(new CurveKey(0.531592867f, 0.9347657f, 0.121062361f, 0.201716289f, CurveContinuity.Smooth));
            fastSlowCurve.Keys.Add(new CurveKey(1, 1, 0, 0, CurveContinuity.Smooth));
            fastSlowCurve.PreLoop = fastSlowCurve.PostLoop = CurveLoopType.Constant;
            linearCurve = new Curve();
            linearCurve.Keys.Add(new CurveKey(0, 0));
            linearCurve.Keys.Add(new CurveKey(1, 1));
            linearCurve.ComputeTangents(CurveTangent.Linear);
            linearCurve.PreLoop = linearCurve.PostLoop = CurveLoopType.Constant;
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
            float t = (float)(now - time1).TotalSeconds / durationSeconds;
            return Vector2.Lerp(pos1, pos2, curve.Evaluate(t));
        }

        void SetTarget(Vector2 startPos, Vector2 finishPos, TimeSpan now, float durationSeconds, Curvature type)
        {
            pos1 = startPos;
            pos2 = finishPos;
            time1 = now;
            this.durationSeconds = durationSeconds;
            switch (type)
            {
                case Curvature.SlowFastSlow: curve = slowFastSlowCurve; break;
                case Curvature.SlowFast: curve = slowFastCurve; break;
                case Curvature.FastSlow: curve = fastSlowCurve; break;
                case Curvature.Linear: curve = linearCurve; break;
                default: throw new Exception("Unexpected curvature " + type);
            }
        }
    }
}
