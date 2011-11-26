using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace AW2.UI
{
    /// <summary>
    /// The state of all input controls, including keyboard, mouse and Xbox 360 Controllers.
    /// </summary>
    public class InputState
    {
        public static readonly InputState EMPTY = new InputState
        {
            Keyboard = new KeyboardState(),
            Mouse = new MouseState(),
            GamePad1 = new GamePadState(),
            GamePad2 = new GamePadState(),
            GamePad3 = new GamePadState(),
            GamePad4 = new GamePadState(),
        };

        public KeyboardState Keyboard { get; private set; }
        public MouseState Mouse { get; private set; }
        public GamePadState GamePad1 { get; private set; }
        public GamePadState GamePad2 { get; private set; }
        public GamePadState GamePad3 { get; private set; }
        public GamePadState GamePad4 { get; private set; }

        public static InputState GetState()
        {
            return new InputState
            {
                Keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState(),
                Mouse = Microsoft.Xna.Framework.Input.Mouse.GetState(),
                GamePad1 = Microsoft.Xna.Framework.Input.GamePad.GetState(PlayerIndex.One),
                GamePad2 = Microsoft.Xna.Framework.Input.GamePad.GetState(PlayerIndex.Two),
                GamePad3 = Microsoft.Xna.Framework.Input.GamePad.GetState(PlayerIndex.Three),
                GamePad4 = Microsoft.Xna.Framework.Input.GamePad.GetState(PlayerIndex.Four)
            };
        }

        public GamePadState GetGamePadState(int gamePadIndex)
        {
            if (gamePadIndex < 0 || gamePadIndex > 3) throw new IndexOutOfRangeException("Invalid game pad index " + gamePadIndex);
            return
                gamePadIndex == 0 ? GamePad1 :
                gamePadIndex == 1 ? GamePad2 :
                gamePadIndex == 2 ? GamePad3 :
                GamePad4;
        }
    }
}
