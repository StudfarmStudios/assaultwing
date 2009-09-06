using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Events;
using AW2.Net;
using AW2.Net.Messages;

namespace AW2.UI
{
    /// <summary>
    /// Basic user interface implementation.
    /// </summary>
    class UIEngineImpl : GameComponent
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
        // HACK: Remove from release builds: showOnlyPlayer1Control, showOnlyPlayer2Control, showEverybodyControl
        private Control fullscreenControl;
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
            EventEngine eventEngine = (EventEngine)Game.Services.GetService(typeof(EventEngine));
            InputState newState = InputState.GetState();

            // Reset mouse cursor to the middle of the game window.
            if (eatMouse)
                Mouse.SetPosition(
                    AssaultWing.Instance.ClientBounds.Width / 2,
                    AssaultWing.Instance.ClientBounds.Height / 2);

            // Update controls.
            Control.ForEachControl(control => control.SetState(ref oldState, ref newState));

            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                // Fetch control messages from game clients.
                PlayerControlsMessage message = null;
                while ((message = AssaultWing.Instance.NetworkEngine.GameClientConnections.Messages.TryDequeue<PlayerControlsMessage>()) != null)
                {
                    var player = AssaultWing.Instance.DataEngine.Spectators.First(plr => plr.Id == message.PlayerId);
                    if (player.ConnectionId != message.ConnectionId)
                    {
                        // A client sent controls for a player that lives on another game instance.
                        // We silently ignore the controls.
                        continue;
                    }
                    ((RemoteControl)player.Controls.thrust).SetControlState(
                        message.GetControlState(PlayerControlType.Thrust).force,
                        message.GetControlState(PlayerControlType.Thrust).pulse);
                    ((RemoteControl)player.Controls.left).SetControlState(
                        message.GetControlState(PlayerControlType.Left).force,
                        message.GetControlState(PlayerControlType.Left).pulse);
                    ((RemoteControl)player.Controls.right).SetControlState(
                        message.GetControlState(PlayerControlType.Right).force,
                        message.GetControlState(PlayerControlType.Right).pulse);
                    ((RemoteControl)player.Controls.down).SetControlState(
                        message.GetControlState(PlayerControlType.Down).force,
                        message.GetControlState(PlayerControlType.Down).pulse);
                    ((RemoteControl)player.Controls.fire1).SetControlState(
                        message.GetControlState(PlayerControlType.Fire1).force,
                        message.GetControlState(PlayerControlType.Fire1).pulse);
                    ((RemoteControl)player.Controls.fire2).SetControlState(
                        message.GetControlState(PlayerControlType.Fire2).force,
                        message.GetControlState(PlayerControlType.Fire2).pulse);
                    ((RemoteControl)player.Controls.extra).SetControlState(
                        message.GetControlState(PlayerControlType.Extra).force,
                        message.GetControlState(PlayerControlType.Extra).pulse);
                }
            }

            oldState = newState;

            // Check general controls.
            if (fullscreenControl.Pulse)
                AssaultWing.Instance.ToggleFullscreen();
            if (showEverybodyControl.Pulse)
                AssaultWing.Instance.ShowOnlyPlayer(-1);
            if (showOnlyPlayer1Control.Pulse)
                AssaultWing.Instance.ShowOnlyPlayer(0);
            if (showOnlyPlayer2Control.Pulse && AssaultWing.Instance.DataEngine.Spectators.Count > 1)
                AssaultWing.Instance.ShowOnlyPlayer(1);
        }
    }
}
