using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Helpers;
using AW2.Net;

namespace AW2.UI
{
    public class QuickStartLogic : UserControlledLogic
    {
        private enum StateType { StartMenuEngine, OpenBattlefrontMenu, UpdatePilotData, ConnectToGameServer, StartGameplay, Idle }

        private StateType _state;
        private CommandLineOptions.QuickStartOptions _options;

        public QuickStartLogic(AssaultWing<ClientEvent> game, CommandLineOptions.QuickStartOptions options)
            : base(game)
        {
            _options = options;
        }

        public override void Initialize()
        {
            base.Initialize();
            GameState = GAMESTATE_MENU;
        }

        public override void Update()
        {
            if (Game.NetworkingErrors.Any()) ResetState();
            base.Update();
            switch (_state)
            {
                case StateType.StartMenuEngine:
                    ShowMainMenuAndResetGameplay();
                    _state = StateType.OpenBattlefrontMenu;
                    break;
                case StateType.OpenBattlefrontMenu:
                    if (!MainMenuActive) break;
                    MenuEngine.MainMenu.ItemCollections.Click_NetworkGame();
                    _state = StateType.UpdatePilotData;
                    break;
                case StateType.UpdatePilotData:
                    if (!MainMenuNetworkItemsActive) break;
                    // TODO: Peter: steam achievements and stats would be updated here? (old WebData.UpdatePilotData was here)
                    ShowInfoDialog("Fetching pilot record...", "Update pilot data");
                    _state = StateType.ConnectToGameServer;
                    break;
                case StateType.ConnectToGameServer:
                    // Cancel quickstart FIXME !!! If user Escapes server connection dialog, we should cancel. Doesn't happen now!
                    if (!MainMenuNetworkItemsActive) { _state = StateType.Idle; break; }

                    //if (!Game.DataEngine.LocalPlayer.StatsData.IsLoggedIn) break;
                    HideDialog("Update pilot data");
                    //SetPlayerSettings();
                    if (TryConnectToGameServer())
                        _state = StateType.StartGameplay;
                    else
                        _state = StateType.Idle;
                    break;
                case StateType.StartGameplay:
                    if (!Game.NetworkEngine.IsConnectedToGameServer) break;
                    Game.IsReadyToStartArena = true;
                    _state = StateType.Idle;
                    break;
                case StateType.Idle: break;
                default: throw new ApplicationException("Unexpected state " + _state);
            }
        }

        private void ResetState()
        {
            _state = StateType.Idle;
            Game.IsReadyToStartArena = false;
        }

        private void SetPlayerSettings()
        {
            var player = Game.DataEngine.LocalPlayer;
            var settings = Game.Settings.Players.Player1;
            if (CanonicalString.IsRegistered(_options.ShipName))
                settings.ShipName = player.ShipName = (CanonicalString)_options.ShipName;
            if (CanonicalString.IsRegistered(_options.Weapon2Name))
                settings.Weapon2Name = player.Weapon2Name = (CanonicalString)_options.Weapon2Name;
            if (CanonicalString.IsRegistered(_options.ExtraDeviceName))
                settings.ExtraDeviceName = player.ExtraDeviceName = (CanonicalString)_options.ExtraDeviceName;
            settings.Name = player.Name;
            Game.Settings.ToFile();
        }

        private bool TryConnectToGameServer()
        {
            var gameServerEndPoints = new AWEndPoint[0];
            try
            {
                gameServerEndPoints = _options.GameServerEndPoints.Select(str => AWEndPoint.Parse(Game.Services, str)).ToArray();
            }
            catch (Exception e)
            {
                Log.Write("Failed to parse game server address", e);
                ShowInfoDialog("Error in game server address.");
                return false;
            }
            Game.StartClient(gameServerEndPoints);
            Game.ShowConnectingToGameServerDialog(_options.GameServerName);
            return true;
        }
    }
}
