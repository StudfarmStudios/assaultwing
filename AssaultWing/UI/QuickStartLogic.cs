using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Net;

namespace AW2.UI
{
    public class QuickStartLogic : UserControlledLogic
    {
        private enum StateType { StartMenuEngine, ConnectToGameServer, StartGameplay, Idle }

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
                    _state = StateType.ConnectToGameServer;
                    break;
                case StateType.ConnectToGameServer:
                    StartGameplay();
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

        private void StartGameplay()
        {
            var loginToken = "4f3aadd688f9babf2d00042c"; // !!! hardcoded
            var gameServerName = "Some Server"; // !!! hardcoded
            var gameServerEndPoints = new[] // !!! hardcoded
            {
                new AWEndPoint(new System.Net.IPEndPoint(new System.Net.IPAddress(new byte[] { 192, 168, 11, 2 }), 16727), 16727),
            };
            MenuEngine.MainMenu.ItemCollections.Click_NetworkGame();
            var localPlayer = Game.DataEngine.Players.Single(plr => plr.IsLocal);
            Game.WebData.UpdatePilotData(localPlayer, loginToken);
            Game.StartClient(gameServerEndPoints);
            Game.ShowConnectingToGameServerDialog(gameServerName);
        }
    }
}
