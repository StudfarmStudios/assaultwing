using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Core.GameComponents;

namespace AW2.UI
{
    public class DedicatedServerLogic<E> : ProgramLogic<E>
    {
        private const int GAMESTATE_INITIALIZING = 0;
        private const int GAMESTATE_GAMEPLAY = 1;

        public DedicatedServerLogic(AssaultWing<E> game)
            : base(game)
        {
            var dedicatedServer = new DedicatedServer(game, 13);
            game.Components.Add(new SteamServerComponent(game, 0));
            game.Components.Add(dedicatedServer);
            dedicatedServer.Enabled = true;
        }

        public override void StartArena()
        {
            Game.StartArenaBase();
            GameState = GAMESTATE_GAMEPLAY;
        }

        public override void FinishArena()
        {
            Game.DataEngine.ClearGameState();
            GameState = GAMESTATE_INITIALIZING;
        }

        protected override void EnableGameState(int value)
        {
            switch (value)
            {
                case GAMESTATE_INITIALIZING:
                    break;
                case GAMESTATE_GAMEPLAY:
                    Game.LogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PreFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PostFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    break;
                default:
                    throw new ApplicationException("Unexpected game state " + value);
            }
        }

        protected override void DisableGameState(int value)
        {
            switch (value)
            {
                case GAMESTATE_INITIALIZING:
                    break;
                case GAMESTATE_GAMEPLAY:
                    Game.LogicEngine.Enabled = false;
                    Game.PreFrameLogicEngine.Enabled = false;
                    Game.PostFrameLogicEngine.Enabled = false;
                    break;
                default:
                    throw new ApplicationException("Unexpected game state " + value);
            }
        }
    }
}
