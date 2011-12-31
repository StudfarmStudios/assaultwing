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
        private Keys _key;

        public KeyControlType(Keys key)
        {
            _key = key;
        }

        public Control GetControl()
        {
            return new KeyboardKey(_key);
        }

        public override string ToString()
        {
            return _key.ToString();
        }
    }

    public class GamePadButtonControlType : IControlType
    {
        private int _gamePad;
        private GamePadButtonType _button;

        public GamePadButtonType Button { get { return _button; } }

        public GamePadButtonControlType(int gamePad, GamePadButtonType button)
        {
            _gamePad = gamePad;
            _button = button;
        }

        public Control GetControl()
        {
            return new GamePadButton(_gamePad, _button);
        }

        public override string ToString()
        {
            return string.Format("Pad{0} {1}", _gamePad + 1, _button);
        }
    }

    public class GamePadStickDirectionControlType : IControlType
    {
        private int _gamePad;
        private GamePadStickType _stick;
        private GamePadStickDirectionType _direction;

        public GamePadStickDirectionControlType(int gamePad, GamePadStickType stick, GamePadStickDirectionType direction)
        {
            _gamePad = gamePad;
            _stick = stick;
            _direction = direction;
        }

        public Control GetControl()
        {
            return new GamePadStickDirection(_gamePad, _stick, _direction);
        }

        public override string ToString()
        {
            return string.Format("Pad{0} {1} {2}", _gamePad + 1, _stick, _direction);
        }
    }

    public class PlayerControlsSettings
    {
        private IControlType _thrust;
        private IControlType _left;
        private IControlType _right;
        private IControlType _down;
        private IControlType _fire1;
        private IControlType _fire2;
        private IControlType _extra;

        public IControlType Thrust { get { return _thrust; } set { _thrust = value; } }
        public IControlType Left { get { return _left; } set { _left = value; } }
        public IControlType Right { get { return _right; } set { _right = value; } }
        public IControlType Down { get { return _down; } set { _down = value; } }
        public IControlType Fire1 { get { return _fire1; } set { _fire1 = value; } }
        public IControlType Fire2 { get { return _fire2; } set { _fire2 = value; } }
        public IControlType Extra { get { return _extra; } set { _extra = value; } }

        public void CopyFrom(PlayerControlsSettings other)
        {
            Thrust = other.Thrust;
            Left = other.Left;
            Right = other.Right;
            Down = other.Down;
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
            Down = new KeyControlType(Keys.Down),
            Fire1 = new KeyControlType(Keys.RightControl),
            Fire2 = new KeyControlType(Keys.RightShift),
            Extra = new KeyControlType(Keys.Down),
        };
        public static readonly PlayerControlsSettings PRESET_KEYBOARD_LEFT = new PlayerControlsSettings
        {
            Thrust = new KeyControlType(Keys.W),
            Left = new KeyControlType(Keys.A),
            Right = new KeyControlType(Keys.D),
            Down = new KeyControlType(Keys.S),
            Fire1 = new KeyControlType(Keys.LeftControl),
            Fire2 = new KeyControlType(Keys.LeftShift),
            Extra = new KeyControlType(Keys.S),
        };
        public static readonly PlayerControlsSettings PRESET_GAMEPAD1 = new PlayerControlsSettings
        {
            Thrust = new GamePadStickDirectionControlType(0, GamePadStickType.DPad, GamePadStickDirectionType.Up),
            Left = new GamePadStickDirectionControlType(0, GamePadStickType.RThumb, GamePadStickDirectionType.Left),
            Right = new GamePadStickDirectionControlType(0, GamePadStickType.RThumb, GamePadStickDirectionType.Right),
            Down = new GamePadStickDirectionControlType(0, GamePadStickType.DPad, GamePadStickDirectionType.Down),
            Fire1 = new GamePadButtonControlType(0, GamePadButtonType.RShoulder),
            Fire2 = new GamePadButtonControlType(0, GamePadButtonType.LShoulder),
            Extra = new GamePadButtonControlType(0, GamePadButtonType.RTrigger),
        };
        public static readonly PlayerControlsSettings PRESET_GAMEPAD2 = new PlayerControlsSettings
        {
            Thrust = new GamePadStickDirectionControlType(1, GamePadStickType.DPad, GamePadStickDirectionType.Up),
            Left = new GamePadStickDirectionControlType(1, GamePadStickType.RThumb, GamePadStickDirectionType.Left),
            Right = new GamePadStickDirectionControlType(1, GamePadStickType.RThumb, GamePadStickDirectionType.Right),
            Down = new GamePadStickDirectionControlType(1, GamePadStickType.DPad, GamePadStickDirectionType.Down),
            Fire1 = new GamePadButtonControlType(1, GamePadButtonType.RShoulder),
            Fire2 = new GamePadButtonControlType(1, GamePadButtonType.LShoulder),
            Extra = new GamePadButtonControlType(1, GamePadButtonType.RTrigger),
        };

        private PlayerControlsSettings _player1;
        private PlayerControlsSettings _player2;
        private IControlType _chat;

        public PlayerControlsSettings Player1 { get { return _player1; } }
        public PlayerControlsSettings Player2 { get { return _player2; } }
        public IControlType Chat { get { return _chat; } set { _chat = value; } }

        public ControlsSettings()
        {
            Reset();
        }

        public void Reset()
        {
            _player1 = PRESET_KEYBOARD_RIGHT;
            _player2 = PRESET_KEYBOARD_LEFT;
            _chat = new KeyControlType(Keys.Enter);
        }
    }
}
