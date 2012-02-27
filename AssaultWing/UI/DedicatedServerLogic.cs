using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Core.GameComponents;

namespace AW2.UI
{
    public class DedicatedServerLogic : ProgramLogic
    {
        public DedicatedServerLogic(AssaultWing game)
            : base(game)
        {
            var dedicatedServer = new DedicatedServer(game, 13);
            game.Components.Add(dedicatedServer);
            dedicatedServer.Enabled = true;
        }

        public override void FinishArena()
        {
            Game.DataEngine.ClearGameState();
            Game.GameState = GameState.Initializing;
        }

        public override void EnableGameState(GameState value)
        {
            switch (value)
            {
                case GameState.Initializing:
                    break;
                case GameState.Gameplay:
                    Game.LogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PreFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PostFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    break;
                default:
                    throw new ApplicationException("Unexpected game state " + value);
            }
        }

        public override void DisableGameState(GameState value)
        {
            switch (value)
            {
                case GameState.Initializing:
                    break;
                case GameState.Gameplay:
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
