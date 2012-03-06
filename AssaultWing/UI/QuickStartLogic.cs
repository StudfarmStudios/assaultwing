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

        public QuickStartLogic(AssaultWing game)
            : base(game)
        {
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
                    var loginToken = "4f3aadd688f9babf2d00042c"; // !!! hardcoded
                    Game.WebData.UpdatePilotData(Game.DataEngine.LocalPlayer, loginToken);
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
            var gameServerName = "Some Server"; // !!! hardcoded
            var gameServerEndPoints = new[] // !!! hardcoded
            {
                new AWEndPoint(new System.Net.IPEndPoint(new System.Net.IPAddress(new byte[] { 192, 168, 11, 2 }), 16727), 16727),
            };
            Game.WebData.UpdatePilotRanking(Game.DataEngine.LocalPlayer);
            Game.StartClient(gameServerEndPoints);
            Game.ShowConnectingToGameServerDialog(gameServerName);
        }
    }
}
