using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace AW2.UI
{
    /// <summary>
    /// The state of all input controls, including keyboard, mouse and Xbox 360 Controllers.
    /// </summary>
    public struct InputState
    {
        /// <summary>
        /// State of the keyboard.
        /// </summary>
        public KeyboardState keyboard;

        /// <summary>
        /// State of the mouse.
        /// </summary>
        public MouseState mouse;

        /// <summary>
        /// State of the first Xbox game controller.
        /// </summary>
        public GamePadState gamePad1;

        /// <summary>
        /// State of the second Xbox game controller.
        /// </summary>
        public GamePadState gamePad2;

        /// <summary>
        /// State of the third Xbox game controller.
        /// </summary>
        public GamePadState gamePad3;

        /// <summary>
        /// State of the fourth Xbox game controller.
        /// </summary>
        public GamePadState gamePad4;

        /// <summary>
        /// Gets the current state of all input devices.
        /// </summary>
        /// <returns>Current state of all input devices.</returns>
        public static InputState GetState()
        {
            return new InputState(
                Keyboard.GetState(),
                Mouse.GetState(),
                GamePad.GetState(PlayerIndex.One),
                GamePad.GetState(PlayerIndex.Two),
                GamePad.GetState(PlayerIndex.Three),
                GamePad.GetState(PlayerIndex.Four)
            );
        }

        private InputState(KeyboardState keyboard, MouseState mouse, GamePadState pad1, GamePadState pad2, GamePadState pad3, GamePadState pad4)
        {
            this.keyboard = keyboard;
            this.mouse = mouse;
            this.gamePad1 = pad1;
            this.gamePad2 = pad2;
            this.gamePad3 = pad3;
            this.gamePad4 = pad4;
        }
    }

    /// <summary>
    /// Represents a one-direction control on an input device.
    /// </summary>
    /// The control can be read either as a pulse control or a force control.
    /// A pulse control only signals an instant action.
    /// A force control signals constant action with an amount of force.
    /// It is important to call the base constructor in all subclass constructors;
    /// failure to do this will result in the control not working.
    /// When a control is no longer used, it must be released with Release() to
    /// stop it from being updated.
    public abstract class Control
    {
        /// <summary>
        /// The list of registered controls.
        /// </summary>
        static List<Control> controls = new List<Control>();

        /// <summary>
        /// Creates a new control and registers it to receive regular updates from UIEngine.
        /// </summary>
        public Control()
        {
            controls.Add(this);
        }

        /// <summary>
        /// Releases the control so that it won't be updated anymore.
        /// </summary>
        public void Release()
        {
            controls.Remove(this as Control);
        }

        /// <summary>
        /// Performs the specified action on each registered control.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each registered control.</param>
        public static void ForEachControl(Action<Control> action)
        {
            foreach (Control control in controls)
            {
                action(control);
            }
        }
 
        /// <summary>
        /// Sets the control's state based on the current and previous state of all inputs.
        /// </summary>
        /// <param name="oldState">The old state of all inputs.</param>
        /// <param name="newState">The current state of all inputs.</param>
        public abstract void SetState(ref InputState oldState, ref InputState newState);

        /// <summary>
        /// Returns if the control gave a pulse.
        /// </summary>
        public abstract bool Pulse { get; }

        /// <summary>
        /// Returns the amount of control force; a float between 0 and 1.
        /// </summary>
        public abstract float Force { get; }
    }

    /// <summary>
    /// The type of a player's control.
    /// </summary>
    public enum PlayerControlType
    {
        /// <summary>
        /// Thrusts the player's ship forward, or moves up in a menu.
        /// </summary>
        Thrust,

        /// <summary>
        /// Turns the player's ship counter-clockwise, or moves left in a menu.
        /// </summary>
        Left,

        /// <summary>
        /// Turns the player's ship clockwise, or moves right in a menu.
        /// </summary>
        Right,

        /// <summary>
        /// Moves down in a menu.
        /// </summary>
        Down,

        /// <summary>
        /// Fires the player's ship's primary weapon, or performs a selected action in a menu.
        /// </summary>
        Fire1,

        /// <summary>
        /// Fires the player's ship's secondary weapon, or performs an additional action in a menu.
        /// </summary>
        Fire2,

        /// <summary>
        /// Uses the player's ship's extra function, or performs yet an additional action in a menu.
        /// </summary>
        Extra,
    };

    /// <summary>
    /// A player's in-game controls.
    /// </summary>
    public struct PlayerControls
    {
        /// <summary>
        /// Thrusts the player's ship forward, or moves up in a menu.
        /// </summary>
        public Control thrust;
        
        /// <summary>
        /// Turns the player's ship counter-clockwise, or moves left in a menu.
        /// </summary>
        public Control left;
        
        /// <summary>
        /// Turns the player's ship clockwise, or moves right in a menu.
        /// </summary>
        public Control right;
        
        /// <summary>
        /// Moves down in a menu.
        /// </summary>
        public Control down;

        /// <summary>
        /// Fires the player's ship's primary weapon, or performs a selected action in a menu.
        /// </summary>
        public Control fire1;

        /// <summary>
        /// Fires the player's ship's secondary weapon, or performs an additional action in a menu.
        /// </summary>
        public Control fire2;

        /// <summary>
        /// Uses the player's ship's extra function, or performs yet an additional action in a menu.
        /// </summary>
        public Control extra;

        /// <summary>
        /// Returns the specified control.
        /// </summary>
        /// <param name="type">The type of control.</param>
        /// <returns>The control.</returns>
        public Control this[PlayerControlType type]
        {
            get
            {
                switch (type)
                {
                    case PlayerControlType.Thrust: return thrust;
                    case PlayerControlType.Left: return left;
                    case PlayerControlType.Right: return right;
                    case PlayerControlType.Down: return down;
                    case PlayerControlType.Fire1: return fire1;
                    case PlayerControlType.Fire2: return fire2;
                    case PlayerControlType.Extra: return extra;
                    default: throw new ArgumentException("Unknown control type " + Enum.GetName(typeof(PlayerControlType), type));
                }
            }
        }
    }
}
