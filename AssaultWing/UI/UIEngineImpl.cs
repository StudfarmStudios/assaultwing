using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Events;

namespace AW2.UI
{
    /// <summary>
    /// Basic user interface implementation.
    /// </summary>
    class UIEngineImpl : GameComponent, UIEngine
    {
        /// <summary>
        /// The state of input controls in the previous frame.
        /// </summary>
        private InputState oldState;

        /// <summary>
        /// True iff mouse input is eaten by the game.
        /// </summary>
        bool eatMouse;

        /// <summary>
        /// Controls for general functionality.
        /// </summary>
        // HACK: Remove from release builds: menuControl, dialogControl, showOnlyPlayer1Control, showOnlyPlayer2Control, showEverybodyControl
        private Control exitControl1, menuControl, fullscreenControl, dialogControl;
        private Control showOnlyPlayer1Control, showOnlyPlayer2Control, showEverybodyControl;

        /// <summary>
        /// If mouse input is being consumed for the purposes of using the mouse
        /// for game controls. Such consumption prevents other programs from using
        /// the mouse in any practical manner. Defaults to <b>false</b>.
        /// </summary>
        public bool MouseControlsEnabled { get { return eatMouse; } set { eatMouse = value; } }

        public UIEngineImpl(Microsoft.Xna.Framework.Game game) : base(game)
        {
            oldState = InputState.GetState();
            eatMouse = false;
            exitControl1 = new KeyboardKey(Keys.Escape);
            menuControl = new KeyboardKey(Keys.M);
            dialogControl = new KeyboardKey(Keys.N);
            fullscreenControl = new KeyboardKey(Keys.F10);
            showOnlyPlayer1Control = new KeyboardKey(Keys.F11);
            showOnlyPlayer2Control = new KeyboardKey(Keys.F12);
            showEverybodyControl = new KeyboardKey(Keys.F9);
        }

        /// <summary>
        /// Reacts to user input.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update</param>
        public override void Update(GameTime gameTime)
        {
            DataEngine data = (DataEngine)Game.Services.GetService(typeof(DataEngine));
            EventEngine eventEngine = (EventEngine)Game.Services.GetService(typeof(EventEngine));
            InputState newState = InputState.GetState();

            // Reset mouse cursor to the middle of the game window.
            if (eatMouse)
            {
                Mouse.SetPosition(AssaultWing.Instance.ClientBounds.Width / 2,
                    AssaultWing.Instance.ClientBounds.Height / 2);
            }

            // Update controls.
            Action<Control> updateControl = delegate(Control control)
            {
                control.SetState(ref oldState, ref newState);
            };
            Control.ForEachControl(updateControl);

            oldState = newState;

            // Check general controls.
            if (exitControl1.Pulse)
            {
                Game.Exit();
            }
            if (menuControl.Pulse)
            {
                AssaultWing game = (AssaultWing)Game;
                game.SwitchMenu();
            }
            if (dialogControl.Pulse)
            {
                AssaultWing game = (AssaultWing)Game;
                game.ToggleDialog();
            }

            if (fullscreenControl.Pulse)
            {
                AssaultWing.Instance.ToggleFullscreen();
            }
            if (showEverybodyControl.Pulse)
                AssaultWing.Instance.ShowOnlyPlayer(-1);
            if (showOnlyPlayer1Control.Pulse)
                AssaultWing.Instance.ShowOnlyPlayer(0);
            if (showOnlyPlayer2Control.Pulse)
                AssaultWing.Instance.ShowOnlyPlayer(1);
        }
    }
}
