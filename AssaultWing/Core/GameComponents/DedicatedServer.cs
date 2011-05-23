using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Core.GameComponents
{
    public class DedicatedServer : AWGameComponent
    {
        private bool _initialized;

        public new AssaultWing Game { get; private set; }

        public DedicatedServer(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
            Game = game;
        }

        public override void Update()
        {
            if (!_initialized)
            {
                Game.NetworkEngine.ConnectToManagementServer();
                if (!Game.StartServer())
                {
                    AssaultWingProgram.Instance.Exit();
                    return;
                }
                Game.InitializePlayers(0);
                Game.PrepareArena(Game.SelectedArenaName);
                Game.StartArena();
                _initialized = true;
            }
        }
    }
}
