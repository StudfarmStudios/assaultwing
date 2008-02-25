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
        /// Controls for general functionality.
        /// </summary>
        // HACK: Remove from release builds: menuControl, showOnlyPlayer1Control, showOnlyPlayer2Control, showEverybodyControl
        private Control exitControl1, menuControl, fullscreenControl, dialogControl;
        private Control showOnlyPlayer1Control, showOnlyPlayer2Control, showEverybodyControl;

        public UIEngineImpl(Microsoft.Xna.Framework.Game game) : base(game)
        {
            oldState = InputState.GetState();
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

            // Check player controls.
            Action<Player> playerControlCheck = delegate(Player player)
            {
                foreach (PlayerControlType controlType in Enum.GetValues(typeof(PlayerControlType)))
                {
                    Control control = player.Controls[controlType];
                    // TODO: We can skip sending an event if the control is known to be used only
                    // as a pulse control and control.Pulse is false (even if control.Force > 0).
                    if (control.Force > 0 || control.Pulse)
                    {
                        PlayerControlEvent eve = new PlayerControlEvent(player.Name, controlType, control.Force, control.Pulse);
                        eventEngine.SendEvent(eve);
                    }
                }
            };
            data.ForEachPlayer(playerControlCheck);
        }
    }
}
