using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;

namespace AW2.UI
{
    public class QuickStartLogic : UserControlledLogic
    {
        private bool _quickStartDone;

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
            if (!_quickStartDone) StartGameplay();
            _quickStartDone = true;
        }

        private void StartGameplay()
        {
            var loginToken = "4f3aadd688f9babf2d00042c"; // !!! hardcoded login token
            var gameServerManagementID = 0; // !!! hardcoded
            var gameServerName = "server " + gameServerManagementID; // !!! hardcoded

            MenuEngine.MainMenu.ItemCollections.Click_NetworkGame();
            var localPlayer = Game.DataEngine.Players.Single(plr => plr.IsLocal);
            Game.WebData.UpdatePilotData(localPlayer, loginToken);
            MenuEngine.MainMenu.ItemCollections.Click_ConnectToGameServer(gameServerManagementID, gameServerName);
            Game.IsReadyToStartArena = true;
        }
    }
}
