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

        public QuickStartLogic(AssaultWing game, CommandLineOptions.QuickStartOptions options)
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
            base.Update();
            switch (_state)
            {
                case StateType.StartMenuEngine:
                    ShowMainMenuAndResetGameplay();
                    _state = StateType.OpenBattlefrontMenu;
                    break;
                case StateType.OpenBattlefrontMenu:
                    if (!MainMenuActive) break;
                    MenuEngine.MainMenu.ItemCollections.Click_NetworkGame(loginPilots: false);
                    _state = StateType.UpdatePilotData;
                    break;
                case StateType.UpdatePilotData:
                    if (!MainMenuNetworkItemsActive) break;
                    Game.WebData.UpdatePilotData(Game.DataEngine.LocalPlayer, _options.LoginToken);
                    ShowInfoDialog("Fetching pilot record...", "Update pilot data");
                    _state = StateType.ConnectToGameServer;
                    break;
                case StateType.ConnectToGameServer:
                    if (!MainMenuNetworkItemsActive) { _state = StateType.Idle; break; } // Cancel quickstart FIXME !!! If user Escapes server connection dialog, we should cancel. Doesn't happen now!
                    if (!Game.DataEngine.LocalPlayer.GetStats().IsLoggedIn) break;
                    HideDialog("Update pilot data");
                    ConnectToGameServer();
                    _state = StateType.StartGameplay;
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

        private void ConnectToGameServer()
        {
            Game.WebData.UpdatePilotRanking(Game.DataEngine.LocalPlayer);
            Game.StartClient(_options.GameServerEndPoints.Select(str => AWEndPoint.Parse(str)).ToArray());
            Game.ShowConnectingToGameServerDialog(_options.GameServerName);
        }
    }
}
