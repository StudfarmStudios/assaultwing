using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace AW2.UI
{
    /// <summary>
    /// A digital button on the mouse.
    /// </summary>
    enum MouseButtons
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
    enum MouseDirections
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
    enum MouseWheelDirections
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
    class MouseDirection : Control
    {
        /// <summary>
        /// The mouse pointer direction that determines the control's state.
        /// </summary>
        MouseDirections direction;

        /// <summary>
        /// Largest mouse movement that won't be registered as control force, in pixels.
        /// </summary>
        float forceMinimum;

        /// <summary>
        /// Sufficient mouse movement for maximal control force, in pixels.
        /// </summary>
        float forceMaximum;

        /// <summary>
        /// Largest mouse movement that won't be registered as control pulse, in pixels.
        /// </summary>
        float pulseMinimum;

        /// <summary>
        /// Minimum number of successive frames during which pulse minimum
        /// has been exceeded in order to register as a pulse.
        /// </summary>
        int pulseMinimumCount;

        /// <summary>
        /// Amount of control force based on latest input state update.
        /// </summary>
        float force;

        /// <summary>
        /// Number of frames at which mouse movement has exceeded pulse minimum movement.
        /// </summary>
        int pulseCount;

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
        /// <param name="pulseMinimumCount">Minimum number of successive frames during which
        /// pulse minimum has been exceeded in order to register as a pulse.</param>
        public MouseDirection(MouseDirections direction, float forceMinimum, float forceMaximum,
            float pulseMinimum, int pulseMinimumCount)
            : base()
        {
            if (!(0 <= forceMinimum && forceMinimum < forceMaximum))
                throw new ArgumentException("Invalid mouse direction movement limits");
            if (!(0 < pulseMinimum && 0 < pulseMinimumCount))
                throw new ArgumentException("Invalid mouse direction movement limits");
            this.direction = direction;
            this.forceMinimum = forceMinimum;
            this.forceMaximum = forceMaximum;
            this.pulseMinimum = pulseMinimum;
            this.pulseMinimumCount = pulseMinimumCount;
            this.pulseCount = 0;
            this.force = 0;
        }

        public override void SetState(ref InputState oldState, ref InputState newState)
        {
            float movement = 0;
            switch (direction)
            {
                case MouseDirections.Left:
                    movement = Math.Max(-(newState.Mouse.X - AssaultWing.Instance.ClientBounds.Width / 2), 0);
                    break;
                case MouseDirections.Right:
                    movement = Math.Max(newState.Mouse.X - AssaultWing.Instance.ClientBounds.Width / 2, 0);
                    break;
                case MouseDirections.Up:
                    movement = Math.Max(-(newState.Mouse.Y - AssaultWing.Instance.ClientBounds.Height / 2), 0);
                    break;
                case MouseDirections.Down:
                    movement = Math.Max(newState.Mouse.Y - AssaultWing.Instance.ClientBounds.Height / 2, 0);
                    break;
                default:
                    throw new Exception("Unhandled mouse direction " + direction);
            }
            force = MathHelper.Clamp((movement - forceMinimum) / (forceMaximum - forceMinimum), 0, 1);
            if (movement >= pulseMinimum)
                ++pulseCount;
            else
                pulseCount = 0;
        }

        /// <summary>
        /// Returns if the control gave a pulse.
        /// </summary>
        public override bool Pulse { get { return pulseCount >= pulseMinimumCount; } }

        /// <summary>
        /// Returns the amount of control force; a float between 0 and 1.
        /// </summary>
        public override float Force { get { return force; } }
    }

    /// <summary>
    /// A digital mouse button.
    /// </summary>
    class MouseButton : Control
    {
        MouseButtons button;
        bool pulse;
        float force;

        /// <summary>
        /// Creates a control from a mouse button.
        /// </summary>
        /// <param name="button">The mouse button.</param>
        public MouseButton(MouseButtons button)
            : base()
        {
            this.button = button;
            pulse = false;
            force = 0;
        }

        public override void SetState(ref InputState oldState, ref InputState newState)
        {
            switch (button)
            {
                case MouseButtons.Left:
                    pulse = newState.Mouse.LeftButton == ButtonState.Pressed &&
                            oldState.Mouse.LeftButton == ButtonState.Released;
                    force = newState.Mouse.LeftButton == ButtonState.Pressed ? 1f : 0f;
                    break;
                case MouseButtons.Right:
                    pulse = newState.Mouse.RightButton == ButtonState.Pressed &&
                            oldState.Mouse.RightButton == ButtonState.Released;
                    force = newState.Mouse.RightButton == ButtonState.Pressed ? 1f : 0f;
                    break;
                case MouseButtons.Middle:
                    pulse = newState.Mouse.MiddleButton == ButtonState.Pressed &&
                            oldState.Mouse.MiddleButton == ButtonState.Released;
                    force = newState.Mouse.MiddleButton == ButtonState.Pressed ? 1f : 0f;
                    break;
                case MouseButtons.X1:
                    pulse = newState.Mouse.XButton1 == ButtonState.Pressed &&
                            oldState.Mouse.XButton1 == ButtonState.Released;
                    force = newState.Mouse.XButton1 == ButtonState.Pressed ? 1f : 0f;
                    break;
                case MouseButtons.X2:
                    pulse = newState.Mouse.XButton2 == ButtonState.Pressed &&
                            oldState.Mouse.XButton2 == ButtonState.Released;
                    force = newState.Mouse.XButton2 == ButtonState.Pressed ? 1f : 0f;
                    break;
                default:
                    throw new Exception("Unhandled mouse button " + button);
            }
        }

        /// <summary>
        /// Returns if the control gave a pulse.
        /// </summary>
        public override bool Pulse { get { return pulse; } }

        /// <summary>
        /// Returns the amount of control force; a float between 0 and 1.
        /// </summary>
        public override float Force { get { return force; } }
    }

    /// <summary>
    /// A direction of the analog mouse wheel.
    /// </summary>
    class MouseWheelDirection : Control
    {
        /// <summary>
        /// The mouse wheel rotation direction that determines the control's state.
        /// </summary>
        MouseWheelDirections direction;

        /// <summary>
        /// Largest mouse wheel rotation that won't be registered as control force, in clicks.
        /// </summary>
        float forceMinimum;

        /// <summary>
        /// Sufficient mouse wheel rotation for maximal control force, in clicks.
        /// </summary>
        float forceMaximum;

        /// <summary>
        /// Largest mouse wheel rotation that won't be registered as control pulse, in clicks.
        /// </summary>
        float pulseMinimum;

        /// <summary>
        /// Minimum number of successive frames during which pulse minimum
        /// has been exceeded in order to register as a pulse.
        /// </summary>
        int pulseMinimumCount;

        /// <summary>
        /// Amount of control force based on latest input state update.
        /// </summary>
        float force;

        /// <summary>
        /// Number of frames at which mouse wheel rotation has exceeded pulse minimum rotation.
        /// </summary>
        int pulseCount;

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
        /// <param name="pulseMinimumCount">Minimum number of successive frames during which
        /// pulse minimum has been exceeded in order to register as a pulse.</param>
        public MouseWheelDirection(MouseWheelDirections direction, float forceMinimum, float forceMaximum,
            float pulseMinimum, int pulseMinimumCount)
            : base()
        {
            if (!(0 <= forceMinimum && forceMinimum < forceMaximum))
                throw new ArgumentException("Invalid mouse wheel direction movement limits");
            if (!(0 < pulseMinimum && 0 < pulseMinimumCount))
                throw new ArgumentException("Invalid mouse wheel direction movement limits");
            this.direction = direction;
            this.forceMinimum = forceMinimum;
            this.forceMaximum = forceMaximum;
            this.pulseMinimum = pulseMinimum;
            this.pulseMinimumCount = pulseMinimumCount;
            this.pulseCount = 0;
            this.force = 0;
        }

        public override void SetState(ref InputState oldState, ref InputState newState)
        {
            float movement = 0;
            switch (direction)
            {
                case MouseWheelDirections.Forward:
                    movement = Math.Max(newState.Mouse.ScrollWheelValue - oldState.Mouse.ScrollWheelValue, 0);
                    break;
                case MouseWheelDirections.Backward:
                    movement = Math.Max(-(newState.Mouse.ScrollWheelValue - oldState.Mouse.ScrollWheelValue), 0);
                    break;
                default:
                    throw new Exception("Unhandled mouse wheel direction " + direction);
            }
            force = MathHelper.Clamp((movement - forceMinimum) / (forceMaximum - forceMinimum), 0, 1);
            if (movement >= pulseMinimum)
                ++pulseCount;
            else
                pulseCount = 0;
        }

        /// <summary>
        /// Returns if the control gave a pulse.
        /// </summary>
        public override bool Pulse { get { return pulseCount >= pulseMinimumCount; } }

        /// <summary>
        /// Returns the amount of control force; a float between 0 and 1.
        /// </summary>
        public override float Force { get { return force; } }
    }
}
