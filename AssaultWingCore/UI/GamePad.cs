using System;
using Microsoft.Xna.Framework.Input;

namespace AW2.UI
{
    public enum GamePadButtonType
    {
        A,
        B,
        Back,
        BigButton,
        LeftShoulder,
        LeftStick,
        RightShoulder,
        RightStick,
        LeftTrigger,
        RightTrigger,
        Start,
        X,
        Y,
    }

    public enum GamePadStickType
    {
        DPad,
        LeftThumbStick,
        RightThumbStick,
    }

    public enum GamePadStickDirectionType
    {
        Up,
        Down,
        Left,
        Right
    };

    /// <summary>
    /// A click button on an XBox 360 controller.
    /// </summary>
    public class GamePadButton : Control
    {
        private int _gamePadIndex;
        private GamePadButtonType _button;

        public GamePadButton(int gamePadIndex, GamePadButtonType button)
        {
            _gamePadIndex = gamePadIndex;
            _button = button;
        }

        public override bool Pulse
        {
            get { return IsGamePadButtonPressed(NewState) && !IsGamePadButtonPressed(OldState); }
        }

        public override float Force { get { return GetGamePadButtonForce(NewState); } }

        public override string ToString()
        {
            return string.Format("{0} on game pad {1}", _button, _gamePadIndex + 1);
        }

        private bool IsGamePadButtonPressed(InputState state)
        {
            var gamePadState = state.GetGamePadState(_gamePadIndex);
            var buttons = gamePadState.Buttons;
            switch (_button)
            {
                case GamePadButtonType.A: return buttons.A == ButtonState.Pressed;
                case GamePadButtonType.B: return buttons.B == ButtonState.Pressed;
                case GamePadButtonType.Back: return buttons.Back == ButtonState.Pressed;
                case GamePadButtonType.BigButton: return buttons.BigButton == ButtonState.Pressed;
                case GamePadButtonType.LeftShoulder: return buttons.LeftShoulder == ButtonState.Pressed;
                case GamePadButtonType.LeftStick: return buttons.LeftStick == ButtonState.Pressed;
                case GamePadButtonType.RightShoulder: return buttons.RightShoulder == ButtonState.Pressed;
                case GamePadButtonType.RightStick: return buttons.RightStick == ButtonState.Pressed;
                case GamePadButtonType.Start: return buttons.Start == ButtonState.Pressed;
                case GamePadButtonType.X: return buttons.X == ButtonState.Pressed;
                case GamePadButtonType.Y: return buttons.Y == ButtonState.Pressed;
                case GamePadButtonType.LeftTrigger: return gamePadState.Triggers.Left >= 0.5f;
                case GamePadButtonType.RightTrigger: return gamePadState.Triggers.Right >= 0.5f;
                default: throw new ApplicationException("Unexpected game pad button " + _button);
            }
        }

        private float GetGamePadButtonForce(InputState state)
        {
            var gamePadState = state.GetGamePadState(_gamePadIndex);
            var buttons = gamePadState.Buttons;
            switch (_button)
            {
                case GamePadButtonType.A: return buttons.A == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.B: return buttons.B == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.Back: return buttons.Back == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.BigButton: return buttons.BigButton == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.LeftShoulder: return buttons.LeftShoulder == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.LeftStick: return buttons.LeftStick == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.RightShoulder: return buttons.RightShoulder == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.RightStick: return buttons.RightStick == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.Start: return buttons.Start == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.X: return buttons.X == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.Y: return buttons.Y == ButtonState.Pressed ? 1 : 0;
                case GamePadButtonType.LeftTrigger: return gamePadState.Triggers.Left;
                case GamePadButtonType.RightTrigger: return gamePadState.Triggers.Right;
                default: throw new ApplicationException("Unexpected game pad button " + _button);
            }
        }
    }

    /// <summary>
    /// A half-axis of a stick on an XBox 360 controller.
    /// </summary>
    public class GamePadStickDirection : Control
    {
        private int _gamePadIndex;
        private GamePadStickType _stick;
        private GamePadStickDirectionType _direction;

        public GamePadStickDirection(int gamePadIndex, GamePadStickType stick, GamePadStickDirectionType direction)
        {
            _gamePadIndex = gamePadIndex;
            _stick = stick;
            _direction = direction;
        }

        public override bool Pulse
        {
            get { return IsGamePadStickDirectionPressed(NewState) && !IsGamePadStickDirectionPressed(OldState); }
        }

        public override float Force { get { return GetGamePadStickDirectionForce(NewState); } }

        private bool IsGamePadStickDirectionPressed(InputState state)
        {
            var gamePadState = state.GetGamePadState(_gamePadIndex);
            switch (_stick)
            {
                case GamePadStickType.DPad:
                    switch (_direction)
                    {
                        case GamePadStickDirectionType.Up: return gamePadState.DPad.Up == ButtonState.Pressed;
                        case GamePadStickDirectionType.Down: return gamePadState.DPad.Down == ButtonState.Pressed;
                        case GamePadStickDirectionType.Left: return gamePadState.DPad.Left == ButtonState.Pressed;
                        case GamePadStickDirectionType.Right: return gamePadState.DPad.Right == ButtonState.Pressed;
                        default: throw new ApplicationException("Unexpected game pad stick direction " + _direction);
                    }
                case GamePadStickType.LeftThumbStick:
                    switch (_direction)
                    {
                        case GamePadStickDirectionType.Up: return gamePadState.ThumbSticks.Left.Y > 0.5f;
                        case GamePadStickDirectionType.Down: return gamePadState.ThumbSticks.Left.Y < 0.5f;
                        case GamePadStickDirectionType.Left: return gamePadState.ThumbSticks.Left.X < 0.5f;
                        case GamePadStickDirectionType.Right: return gamePadState.ThumbSticks.Left.X > 0.5f;
                        default: throw new ApplicationException("Unexpected game pad stick direction " + _direction);
                    }
                case GamePadStickType.RightThumbStick:
                    switch (_direction)
                    {
                        case GamePadStickDirectionType.Up: return gamePadState.ThumbSticks.Right.Y > 0.5f;
                        case GamePadStickDirectionType.Down: return gamePadState.ThumbSticks.Right.Y < 0.5f;
                        case GamePadStickDirectionType.Left: return gamePadState.ThumbSticks.Right.X < 0.5f;
                        case GamePadStickDirectionType.Right: return gamePadState.ThumbSticks.Right.X > 0.5f;
                        default: throw new ApplicationException("Unexpected game pad stick direction " + _direction);
                    }
                default: throw new ApplicationException("Unexpected game pad stick " + _stick);
            }
        }

        private float GetGamePadStickDirectionForce(InputState state)
        {
            var gamePadState = state.GetGamePadState(_gamePadIndex);
            switch (_stick)
            {
                case GamePadStickType.DPad:
                    switch (_direction)
                    {
                        case GamePadStickDirectionType.Up: return gamePadState.DPad.Up == ButtonState.Pressed ? 1 : 0;
                        case GamePadStickDirectionType.Down: return gamePadState.DPad.Down == ButtonState.Pressed ? 1 : 0;
                        case GamePadStickDirectionType.Left: return gamePadState.DPad.Left == ButtonState.Pressed ? 1 : 0;
                        case GamePadStickDirectionType.Right: return gamePadState.DPad.Right == ButtonState.Pressed ? 1 : 0;
                        default: throw new ApplicationException("Unexpected game pad stick direction " + _direction);
                    }
                case GamePadStickType.LeftThumbStick:
                    switch (_direction)
                    {
                        case GamePadStickDirectionType.Up: return Math.Max(0, gamePadState.ThumbSticks.Left.Y);
                        case GamePadStickDirectionType.Down: return Math.Min(0, gamePadState.ThumbSticks.Left.Y);
                        case GamePadStickDirectionType.Left: return Math.Min(0, gamePadState.ThumbSticks.Left.X);
                        case GamePadStickDirectionType.Right: return Math.Max(0, gamePadState.ThumbSticks.Left.X);
                        default: throw new ApplicationException("Unexpected game pad stick direction " + _direction);
                    }
                case GamePadStickType.RightThumbStick:
                    switch (_direction)
                    {
                        case GamePadStickDirectionType.Up: return Math.Max(0, gamePadState.ThumbSticks.Right.Y);
                        case GamePadStickDirectionType.Down: return Math.Min(0, gamePadState.ThumbSticks.Right.Y);
                        case GamePadStickDirectionType.Left: return Math.Min(0, gamePadState.ThumbSticks.Right.X);
                        case GamePadStickDirectionType.Right: return Math.Max(0, gamePadState.ThumbSticks.Right.X);
                        default: throw new ApplicationException("Unexpected game pad stick direction " + _direction);
                    }
                default: throw new ApplicationException("Unexpected game pad stick " + _stick);
            }
        }
    }
}
