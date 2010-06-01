using System;
using Microsoft.Xna.Framework.Input;

namespace AW2.UI.Mouse
{
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
                    default: throw new ApplicationException("Unhandled mouse button " + _button);
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
                    default: throw new ApplicationException("Unhandled mouse button " + _button);
                }
            }
        }

        public MouseButton(MouseButtons button)
        {
            _button = button;
        }
    }
}
