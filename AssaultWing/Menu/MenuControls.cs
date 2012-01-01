using Microsoft.Xna.Framework.Input;
using AW2.UI;

namespace AW2.Menu
{
    public class MenuControls
    {
        public class Directions
        {
            public Control Up { get; private set; }
            public Control Down { get; private set; }
            public Control Left { get; private set; }
            public Control Right { get; private set; }
            public Directions(Control up, Control down, Control left, Control right)
            {
                Up = up; Down = down; Left = left; Right = right;
            }
        }

        public Directions[] PlayerDirs { get; private set; }
        public Directions Dirs { get { return PlayerDirs[0]; } }
        public Control Activate { get; private set; }
        public Control Back { get; private set; }
        public Control Tab { get; private set; }
        public Control TabBack { get; private set; }
        public Control StartGame { get; private set; }

        public MenuControls()
        {
            PlayerDirs = new[]
            {
                new Directions(
                    up: new MultiControl
                    {
                        new KeyboardKey(Keys.Up),
                        new GamePadStickDirection(0, GamePadStickType.DPad, GamePadStickDirectionType.Up),
                        new GamePadStickDirection(0, GamePadStickType.LThumb, GamePadStickDirectionType.Up),
                        new GamePadStickDirection(0, GamePadStickType.RThumb, GamePadStickDirectionType.Up),
                    },
                    down: new MultiControl
                    {
                        new KeyboardKey(Keys.Down),
                        new GamePadStickDirection(0, GamePadStickType.DPad, GamePadStickDirectionType.Down),
                        new GamePadStickDirection(0, GamePadStickType.LThumb, GamePadStickDirectionType.Down),
                        new GamePadStickDirection(0, GamePadStickType.RThumb, GamePadStickDirectionType.Down),
                    },
                    left: new MultiControl
                    {
                        new KeyboardKey(Keys.Left),
                        new GamePadStickDirection(0, GamePadStickType.DPad, GamePadStickDirectionType.Left),
                        new GamePadStickDirection(0, GamePadStickType.LThumb, GamePadStickDirectionType.Left),
                        new GamePadStickDirection(0, GamePadStickType.RThumb, GamePadStickDirectionType.Left),
                    },
                    right: new MultiControl
                    {
                        new KeyboardKey(Keys.Right),
                        new GamePadStickDirection(0, GamePadStickType.DPad, GamePadStickDirectionType.Right),
                        new GamePadStickDirection(0, GamePadStickType.LThumb, GamePadStickDirectionType.Right),
                        new GamePadStickDirection(0, GamePadStickType.RThumb, GamePadStickDirectionType.Right),
                    }),
                new Directions(
                    up: new MultiControl
                    {
                        new KeyboardKey(Keys.W),
                        new GamePadStickDirection(1, GamePadStickType.DPad, GamePadStickDirectionType.Up),
                        new GamePadStickDirection(1, GamePadStickType.LThumb, GamePadStickDirectionType.Up),
                        new GamePadStickDirection(1, GamePadStickType.RThumb, GamePadStickDirectionType.Up),
                    },
                    down: new MultiControl
                    {
                        new KeyboardKey(Keys.S),
                        new GamePadStickDirection(1, GamePadStickType.DPad, GamePadStickDirectionType.Down),
                        new GamePadStickDirection(1, GamePadStickType.LThumb, GamePadStickDirectionType.Down),
                        new GamePadStickDirection(1, GamePadStickType.RThumb, GamePadStickDirectionType.Down),
                    },
                    left: new MultiControl
                    {
                        new KeyboardKey(Keys.A),
                        new GamePadStickDirection(1, GamePadStickType.DPad, GamePadStickDirectionType.Left),
                        new GamePadStickDirection(1, GamePadStickType.LThumb, GamePadStickDirectionType.Left),
                        new GamePadStickDirection(1, GamePadStickType.RThumb, GamePadStickDirectionType.Left),
                    },
                    right: new MultiControl
                    {
                        new KeyboardKey(Keys.D),
                        new GamePadStickDirection(1, GamePadStickType.DPad, GamePadStickDirectionType.Right),
                        new GamePadStickDirection(1, GamePadStickType.LThumb, GamePadStickDirectionType.Right),
                        new GamePadStickDirection(1, GamePadStickType.RThumb, GamePadStickDirectionType.Right),
                    }),
            };
            Activate = new MultiControl
            {
                new KeyboardKey(Keys.Enter),
                new GamePadButton(0, GamePadButtonType.Start),
                new GamePadButton(0, GamePadButtonType.A),
            };
            Back = new MultiControl
            {
                new KeyboardKey(Keys.Escape),
                new GamePadButton(0, GamePadButtonType.Back),
                new GamePadButton(0, GamePadButtonType.B),
            };
            Tab = new MultiControl
            {
                new KeyboardKey(Keys.Tab),
                new GamePadButton(0, GamePadButtonType.RShoulder),
            };
            TabBack = new MultiControl
            {
                new GamePadButton(0, GamePadButtonType.LShoulder),
            };
            StartGame = new MultiControl
            {
                new KeyboardKey(Keys.F10),
                new GamePadButton(0, GamePadButtonType.Start),
            };
        }
    }
}
