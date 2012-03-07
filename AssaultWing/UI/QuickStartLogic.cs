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
        private AWEndPoint[] _gameServerEndPoints;
        private string _gameServerName;
        private string _loginToken;

        public QuickStartLogic(AssaultWing game, string[] gameServerEndPoints, string gameServerName, string loginToken)
            : base(game)
        {
            _gameServerEndPoints = gameServerEndPoints.Select(str => AWEndPoint.Parse(str)).ToArray();
            _gameServerName = gameServerName;
            _loginToken = loginToken;
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
                    MenuEngine.MainMenu.ItemCollections.Click_NetworkGame();
                    _state = StateType.UpdatePilotData;
                    break;
                case StateType.UpdatePilotData:
                    if (!MainMenuNetworkItemsActive) break;
                    Game.WebData.UpdatePilotData(Game.DataEngine.LocalPlayer, _loginToken);
                    ShowInfoDialog("Fetching pilot record...", "Update pilot data");
                    _state = StateType.ConnectToGameServer;
                    break;
                case StateType.ConnectToGameServer:
                    if (!MainMenuNetworkItemsActive) { _state = StateType.Idle; break; } // Cancel quickstart
                    if (!Game.DataEngine.LocalPlayer.GetStats().IsLoggedIn) break;
                    HideDialog("Update pilot data");
                    // UNDONE for testing !!! Game.Settings.ToFile(); // Save pilot name
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
            Game.StartClient(_gameServerEndPoints);
            Game.ShowConnectingToGameServerDialog(_gameServerName);
        }
    }
}
