using System;
using Microsoft.Xna.Framework.Input;
using AW2.UI;

namespace AW2.Settings
{
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
    }

    public class ControlsSettings
    {
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
            _player1 = new PlayerControlsSettings
            {
                Thrust = new KeyControlType(Keys.Up),
                Left = new KeyControlType(Keys.Left),
                Right = new KeyControlType(Keys.Right),
                Down = new KeyControlType(Keys.Down),
                Fire1 = new KeyControlType(Keys.RightControl),
                Fire2 = new KeyControlType(Keys.RightShift),
                Extra = new KeyControlType(Keys.Down),
            };
            _player2 = new PlayerControlsSettings
            {
                Thrust = new KeyControlType(Keys.W),
                Left = new KeyControlType(Keys.A),
                Right = new KeyControlType(Keys.D),
                Down = new KeyControlType(Keys.X),
                Fire1 = new KeyControlType(Keys.LeftControl),
                Fire2 = new KeyControlType(Keys.LeftShift),
                Extra = new KeyControlType(Keys.X),
            };
            _chat = new KeyControlType(Keys.Enter);
        }
    }
}
