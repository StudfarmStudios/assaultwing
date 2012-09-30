using System;
using Microsoft.Xna.Framework.Input;
using AW2.UI;

namespace AW2.Settings
{
    // TODO: Replace IControlType by Control. It works when you add LimitedSerializationAttibute to Control etc.
    public interface IControlType
    {
        Control GetControl();
    }

    public class KeyControlType : IControlType
    {
        public Keys Key { get; private set; }

        public KeyControlType(Keys key)
        {
            Key = key;
        }

        public Control GetControl()
        {
            return new KeyboardKey(Key);
        }

        public override string ToString()
        {
            return Key.ToString();
        }
    }

    public class GamePadButtonControlType : IControlType
    {
        public int GamePad { get; private set; }
        public GamePadButtonType Button { get; private set; }

        public GamePadButtonControlType(int gamePad, GamePadButtonType button)
        {
            GamePad = gamePad;
            Button = button;
        }

        public Control GetControl()
        {
            return new GamePadButton(GamePad, Button);
        }

        public override string ToString()
        {
            return string.Format("Pad{0} {1}", GamePad + 1, Button);
        }
    }

    public class GamePadStickDirectionControlType : IControlType
    {
        public int GamePad { get; private set; }
        public GamePadStickType Stick { get; private set; }
        public GamePadStickDirectionType Direction { get; private set; }

        public GamePadStickDirectionControlType(int gamePad, GamePadStickType stick, GamePadStickDirectionType direction)
        {
            GamePad = gamePad;
            Stick = stick;
            Direction = direction;
        }

        public Control GetControl()
        {
            return new GamePadStickDirection(GamePad, Stick, Direction);
        }

        public override string ToString()
        {
            return string.Format("Pad{0} {1} {2}", GamePad + 1, Stick, Direction);
        }
    }

    public class PlayerControlsSettings
    {
        public IControlType Thrust { get; set; }
        public IControlType Left { get; set; }
        public IControlType Right { get; set; }
        public IControlType Fire1 { get; set; }
        public IControlType Fire2 { get; set; }
        public IControlType Extra { get; set; }

        public void CopyFrom(PlayerControlsSettings other)
        {
            Thrust = other.Thrust;
            Left = other.Left;
            Right = other.Right;
            Fire1 = other.Fire1;
            Fire2 = other.Fire2;
            Extra = other.Extra;
        }
    }

    public class ControlsSettings
    {
        public static readonly PlayerControlsSettings PRESET_KEYBOARD_RIGHT = new PlayerControlsSettings
        {
            Thrust = new KeyControlType(Keys.Up),
            Left = new KeyControlType(Keys.Left),
            Right = new KeyControlType(Keys.Right),
            Fire1 = new KeyControlType(Keys.RightControl),
            Fire2 = new KeyControlType(Keys.RightShift),
            Extra = new KeyControlType(Keys.Down),
        };
        public static readonly PlayerControlsSettings PRESET_KEYBOARD_LEFT = new PlayerControlsSettings
        {
            Thrust = new KeyControlType(Keys.W),
            Left = new KeyControlType(Keys.A),
            Right = new KeyControlType(Keys.D),
            Fire1 = new KeyControlType(Keys.LeftControl),
            Fire2 = new KeyControlType(Keys.LeftShift),
            Extra = new KeyControlType(Keys.S),
        };
        public static readonly PlayerControlsSettings PRESET_GAMEPAD1 = new PlayerControlsSettings
        {
            Thrust = new GamePadStickDirectionControlType(0, GamePadStickType.DPad, GamePadStickDirectionType.Up),
            Left = new GamePadStickDirectionControlType(0, GamePadStickType.RThumb, GamePadStickDirectionType.Left),
            Right = new GamePadStickDirectionControlType(0, GamePadStickType.RThumb, GamePadStickDirectionType.Right),
            Fire1 = new GamePadButtonControlType(0, GamePadButtonType.RShoulder),
            Fire2 = new GamePadButtonControlType(0, GamePadButtonType.LShoulder),
            Extra = new GamePadButtonControlType(0, GamePadButtonType.RTrigger),
        };
        public static readonly PlayerControlsSettings PRESET_GAMEPAD2 = new PlayerControlsSettings
        {
            Thrust = new GamePadStickDirectionControlType(1, GamePadStickType.DPad, GamePadStickDirectionType.Up),
            Left = new GamePadStickDirectionControlType(1, GamePadStickType.RThumb, GamePadStickDirectionType.Left),
            Right = new GamePadStickDirectionControlType(1, GamePadStickType.RThumb, GamePadStickDirectionType.Right),
            Fire1 = new GamePadButtonControlType(1, GamePadButtonType.RShoulder),
            Fire2 = new GamePadButtonControlType(1, GamePadButtonType.LShoulder),
            Extra = new GamePadButtonControlType(1, GamePadButtonType.RTrigger),
        };

        public PlayerControlsSettings Player1 { get; private set; }
        public PlayerControlsSettings Player2 { get; private set; }
        public IControlType Chat { get; set; }

        public ControlsSettings()
        {
            Reset();
        }

        public void Reset()
        {
            Player1 = PRESET_KEYBOARD_RIGHT;
            Player2 = PRESET_KEYBOARD_LEFT;
            Chat = new KeyControlType(Keys.Enter);
        }
    }
}
