using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Core.GameComponents;

namespace AW2.UI
{
    public class DedicatedServerLogic : ProgramLogic
    {
        private enum GameStateType { Initializing, Gameplay }

        private GameStateType _gameState;
        private GameStateType GameState
        {
            get { return _gameState; }
            set
            {
                DisableGameState(_gameState);
                _gameState = value;
                EnableGameState(value);
            }
        }

        public DedicatedServerLogic(AssaultWing game)
            : base(game)
        {
            var dedicatedServer = new DedicatedServer(game, 13);
            game.Components.Add(dedicatedServer);
            dedicatedServer.Enabled = true;
        }

        public override void StartArena()
        {
            Game.StartArenaBase();
            GameState = GameStateType.Gameplay;
        }

        public override void FinishArena()
        {
            Game.DataEngine.ClearGameState();
            GameState = GameStateType.Initializing;
        }

        private void EnableGameState(GameStateType value)
        {
            switch (value)
            {
                case GameStateType.Initializing:
                    break;
                case GameStateType.Gameplay:
                    Game.LogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PreFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    Game.PostFrameLogicEngine.Enabled = Game.DataEngine.Arena.IsForPlaying;
                    break;
                default:
                    throw new ApplicationException("Unexpected game state " + value);
            }
        }

        private void DisableGameState(GameStateType value)
        {
            switch (value)
            {
                case GameStateType.Initializing:
                    break;
                case GameStateType.Gameplay:
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
