using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game;
using AW2.Net.Messages;

namespace AW2.UI
{
    /// <summary>
    /// Basic user interface implementation.
    /// </summary>
    public class UIEngineImpl : AWGameComponent
    {
        /// <summary>
        /// The state of input controls in the previous frame.
        /// </summary>
        private InputState _oldState;

        /// <summary>
        /// True iff mouse input is eaten by the game.
        /// </summary>
        private bool _eatMouse;

        /// <summary>
        /// If mouse input is being consumed for the purposes of using the mouse
        /// for game controls. Such consumption prevents other programs from using
        /// the mouse in any practical manner. Defaults to <b>false</b>.
        /// </summary>
        public bool MouseControlsEnabled { get { return _eatMouse; } set { _eatMouse = value; } }

        public UIEngineImpl(AssaultWingCore game)
            : base(game)
        {
            _oldState = InputState.GetState();
            _eatMouse = false;
        }

        public override void Update()
        {
            var newState = InputState.GetState();

            // Reset mouse cursor to the middle of the game window.
            if (_eatMouse)
                Microsoft.Xna.Framework.Input.Mouse.SetPosition(Game.GraphicsDeviceService.ClientBounds.Width / 2, Game.GraphicsDeviceService.ClientBounds.Height / 2);

            Control.SetState(ref _oldState, ref newState);
            _oldState = newState;
        }

        public static void HandlePlayerControlsMessage(PlayerControlsMessage mess)
        {
            var player = AssaultWingCore.Instance.DataEngine.Spectators.First(plr => plr.ID == mess.PlayerID);
            if (player.ConnectionID != mess.ConnectionID)
            {
                // A client sent controls for a player that lives on another game instance.
                // We silently ignore the controls.
                return;
            }
            foreach (PlayerControlType control in System.Enum.GetValues(typeof(PlayerControlType)))
                SetRemoteControlState((RemoteControl)player.Controls[control], mess.GetControlState(control));
            var playerPlayer = player as Player;
            if (playerPlayer != null && playerPlayer.Ship != null)
                playerPlayer.Ship.LocationPredicter.StoreControlStates(mess.ControlStates, AssaultWingCore.Instance.NetworkEngine.GetMessageGameTime(mess));
        }

        private static void SetRemoteControlState(RemoteControl control, ControlState state)
        {
            control.SetControlState(state.Force, state.Pulse);
        }
    }
}
