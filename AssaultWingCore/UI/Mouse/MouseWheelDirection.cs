using System;
using Microsoft.Xna.Framework;

namespace AW2.UI.Mouse
{
    /// <summary>
    /// A direction of the analog mouse wheel.
    /// </summary>
    public class MouseWheelDirection : Control
    {
        /// <summary>
        /// The mouse wheel rotation direction that determines the control's state.
        /// </summary>
        private MouseWheelDirections _direction;

        /// <summary>
        /// Largest mouse wheel rotation that won't be registered as control force, in clicks.
        /// </summary>
        private float _forceMinimum;

        /// <summary>
        /// Sufficient mouse wheel rotation for maximal control force, in clicks.
        /// </summary>
        private float _forceMaximum;

        /// <summary>
        /// Largest mouse wheel rotation that won't be registered as control pulse, in clicks.
        /// </summary>
        private float _pulseMinimum;

        public override bool Pulse { get { return GetMovement() >= _pulseMinimum; } }
        public override float Force { get { return MathHelper.Clamp((GetMovement() - _forceMinimum) / (_forceMaximum - _forceMinimum), 0, 1); } }

        /// <summary>
        /// Creates a control from a mouse wheel rotation direction.
        /// </summary>
        /// <param name="direction">The mouse wheel rotation direction.</param>
        /// <param name="forceMinimum">Largest mouse wheel rotation that 
        /// won't be registered as control force, in clicks.</param>
        /// <param name="forceMaximum">Sufficient mouse wheel rotation for 
        /// maximal control force, in clicks.</param>
        /// <param name="pulseMinimum">Largest mouse wheel rotation that 
        /// won't be registered as control pulse, in clicks.</param>
        public MouseWheelDirection(MouseWheelDirections direction, float forceMinimum, float forceMaximum, float pulseMinimum)
        {
            if (!(0 <= forceMinimum && forceMinimum < forceMaximum))
                throw new ArgumentException("Invalid mouse wheel direction movement limits");
            if (!(0 < pulseMinimum))
                throw new ArgumentException("Invalid mouse wheel direction movement limits");
            _direction = direction;
            _forceMinimum = forceMinimum;
            _forceMaximum = forceMaximum;
            _pulseMinimum = pulseMinimum;
        }

        private float GetMovement()
        {
            switch (_direction)
            {
                case MouseWheelDirections.Forward:
                    return Math.Max(NewState.Mouse.ScrollWheelValue - OldState.Mouse.ScrollWheelValue, 0);
                case MouseWheelDirections.Backward:
                    return Math.Max(-(NewState.Mouse.ScrollWheelValue - OldState.Mouse.ScrollWheelValue), 0);
                default: throw new ApplicationException("Unhandled mouse wheel direction " + _direction);
            }
        }
    }
}
