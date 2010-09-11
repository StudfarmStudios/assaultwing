using System;
using Microsoft.Xna.Framework;

namespace AW2.UI.Mouse
{
    /// <summary>
    /// A cardinal direction of the mouse pointer.
    /// </summary>
    public class MouseDirection : Control
    {
        /// <summary>
        /// The mouse pointer direction that determines the control's state.
        /// </summary>
        private MouseDirections _direction;

        /// <summary>
        /// Largest mouse movement that won't be registered as control force, in pixels.
        /// </summary>
        private float _forceMinimum;

        /// <summary>
        /// Sufficient mouse movement for maximal control force, in pixels.
        /// </summary>
        private float _forceMaximum;

        /// <summary>
        /// Largest mouse movement that won't be registered as control pulse, in pixels.
        /// </summary>
        private float _pulseMinimum;

        public override bool Pulse { get { return GetMovement() >= _pulseMinimum; } }
        public override float Force { get { return MathHelper.Clamp((GetMovement() - _forceMinimum) / (_forceMaximum - _forceMinimum), 0, 1); } }

        /// <summary>
        /// Creates a control from a mouse pointer cardinal direction.
        /// </summary>
        /// <param name="direction">The mouse pointer cardinal direction.</param>
        /// <param name="forceMinimum">Largest mouse movement that 
        /// won't be registered as control force, in pixels.</param>
        /// <param name="forceMaximum">Sufficient mouse movement for 
        /// maximal control force, in pixels.</param>
        /// <param name="pulseMinimum">Largest mouse movement that 
        /// won't be registered as control pulse, in pixels.</param>
        public MouseDirection(MouseDirections direction, float forceMinimum, float forceMaximum, float pulseMinimum)
        {
            if (!(0 <= forceMinimum && forceMinimum < forceMaximum))
                throw new ArgumentException("Invalid mouse direction movement limits");
            if (!(0 < pulseMinimum))
                throw new ArgumentException("Invalid mouse direction movement limits");
            _direction = direction;
            _forceMinimum = forceMinimum;
            _forceMaximum = forceMaximum;
            _pulseMinimum = pulseMinimum;
        }

        private float GetMovement()
        {
            switch (_direction)
            {
                case MouseDirections.Left:
                    return Math.Max(-(NewState.Mouse.X - AssaultWingCore.Instance.ClientBounds.Width / 2), 0);
                case MouseDirections.Right:
                    return Math.Max(NewState.Mouse.X - AssaultWingCore.Instance.ClientBounds.Width / 2, 0);
                case MouseDirections.Up:
                    return Math.Max(-(NewState.Mouse.Y - AssaultWingCore.Instance.ClientBounds.Height / 2), 0);
                case MouseDirections.Down:
                    return Math.Max(NewState.Mouse.Y - AssaultWingCore.Instance.ClientBounds.Height / 2, 0);
                default: throw new Exception("Unhandled mouse direction " + _direction);
            }
        }
    }
}
