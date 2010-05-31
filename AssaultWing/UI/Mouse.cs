using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace AW2.UI
{
    /// <summary>
    /// A digital button on the mouse.
    /// </summary>
    public enum MouseButtons
    {
        /// <summary>
        /// The left mouse button.
        /// </summary>
        Left,

        /// <summary>
        /// The right mouse button.
        /// </summary>
        Right,

        /// <summary>
        /// The middle mouse button.
        /// </summary>
        Middle,

        /// <summary>
        /// The X1 mouse button.
        /// </summary>
        /// X1 and X2 are additional buttons used on many mouse devices, 
        /// often for forward and backward navigation in Web browsers.
        X1,

        /// <summary>
        /// The X2 mouse button.
        /// </summary>
        /// X1 and X2 are additional buttons used on many mouse devices, 
        /// often for forward and backward navigation in Web browsers.
        X2,
    }

    /// <summary>
    /// A cardinal direction of the mouse pointer.
    /// </summary>
    public enum MouseDirections
    {
        /// <summary>
        /// Upward mouse pointer movement.
        /// </summary>
        Up,

        /// <summary>
        /// Downward mouse pointer movement.
        /// </summary>
        Down,

        /// <summary>
        /// Leftward mouse pointer movement.
        /// </summary>
        Left,

        /// <summary>
        /// Rightward mouse pointer movement.
        /// </summary>
        Right,
    }

    /// <summary>
    /// A direction of the mouse wheel's rotation.
    /// </summary>
    public enum MouseWheelDirections
    {
        /// <summary>
        /// Forward mouse wheel rotation.
        /// </summary>
        Forward,

        /// <summary>
        /// Backward mouse wheel rotation.
        /// </summary>
        Backward,
    }

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
                    return Math.Max(-(NewState.Mouse.X - AssaultWing.Instance.ClientBounds.Width / 2), 0);
                case MouseDirections.Right:
                    return Math.Max(NewState.Mouse.X - AssaultWing.Instance.ClientBounds.Width / 2, 0);
                case MouseDirections.Up:
                    return Math.Max(-(NewState.Mouse.Y - AssaultWing.Instance.ClientBounds.Height / 2), 0);
                case MouseDirections.Down:
                    return Math.Max(NewState.Mouse.Y - AssaultWing.Instance.ClientBounds.Height / 2, 0);
                default: throw new Exception("Unhandled mouse direction " + _direction);
            }
        }
    }

    /// <summary>
    /// A digital mouse button.
    /// </summary>
    public class MouseButton : Control
    {
        private MouseButtons _button;

        public override bool Pulse
        {
            get
            {
                switch (_button)
                {
                    case MouseButtons.Left:
                        return NewState.Mouse.LeftButton == ButtonState.Pressed &&
                               OldState.Mouse.LeftButton == ButtonState.Released;
                    case MouseButtons.Right:
                        return NewState.Mouse.RightButton == ButtonState.Pressed &&
                               OldState.Mouse.RightButton == ButtonState.Released;
                    case MouseButtons.Middle:
                        return NewState.Mouse.MiddleButton == ButtonState.Pressed &&
                               OldState.Mouse.MiddleButton == ButtonState.Released;
                    case MouseButtons.X1:
                        return NewState.Mouse.XButton1 == ButtonState.Pressed &&
                               OldState.Mouse.XButton1 == ButtonState.Released;
                    case MouseButtons.X2:
                        return NewState.Mouse.XButton2 == ButtonState.Pressed &&
                               OldState.Mouse.XButton2 == ButtonState.Released;
                    default: throw new Exception("Unhandled mouse button " + _button);
                }
            }
        }

        public override float Force
        {
            get
            {
                switch (_button)
                {
                    case MouseButtons.Left: return NewState.Mouse.LeftButton == ButtonState.Pressed ? 1f : 0f;
                    case MouseButtons.Right: return NewState.Mouse.RightButton == ButtonState.Pressed ? 1f : 0f;
                    case MouseButtons.Middle: return NewState.Mouse.MiddleButton == ButtonState.Pressed ? 1f : 0f;
                    case MouseButtons.X1: return NewState.Mouse.XButton1 == ButtonState.Pressed ? 1f : 0f;
                    case MouseButtons.X2: return NewState.Mouse.XButton2 == ButtonState.Pressed ? 1f : 0f;
                    default: throw new Exception("Unhandled mouse button " + _button);
                }
            }
        }

        public MouseButton(MouseButtons button)
        {
            _button = button;
        }
    }

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
            this._direction = direction;
            this._forceMinimum = forceMinimum;
            this._forceMaximum = forceMaximum;
            this._pulseMinimum = pulseMinimum;
        }

        private float GetMovement()
        {
            switch (_direction)
            {
                case MouseWheelDirections.Forward:
                    return Math.Max(NewState.Mouse.ScrollWheelValue - OldState.Mouse.ScrollWheelValue, 0);
                case MouseWheelDirections.Backward:
                    return Math.Max(-(NewState.Mouse.ScrollWheelValue - OldState.Mouse.ScrollWheelValue), 0);
                default: throw new Exception("Unhandled mouse wheel direction " + _direction);
            }
        }
    }
}
