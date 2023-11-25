using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Core.GameComponents;
using AW2.Stats;

namespace AW2.UI
{
    public class DedicatedServerLogic<E> : ProgramLogic<E>
    {
        private const int GAMESTATE_INITIALIZING = 0;
        private const int GAMESTATE_GAMEPLAY = 1;

        protected SteamServerComponent SteamServerComponent { get; init; }

        protected DedicatedServer DedicatedServer { get; init; }

        public DedicatedServerLogic(AssaultWing<E> game, bool consoleServer)
            : base(game)
        {
            DedicatedServer = new DedicatedServer(game, 13);
            SteamServerComponent = new SteamServerComponent(game, 0, consoleServer = consoleServer);
            game.Components.Add(SteamServerComponent);
            game.Components.Add(DedicatedServer);

            if (game.IsSteam)
            {
                var ratingsUpdater = new PilotRatingsUpdater(game);
                game.Components.Add(ratingsUpdater);
                ratingsUpdater.Enabled = true;
            }

            DedicatedServer.Enabled = true;
            SteamServerComponent.Enabled = true;
        }

        public override void StartArena()
        {
            Game.StartArenaBase();
            GameState = GAMESTATE_GAMEPLAY;
            SteamServerComponent.SendUpdatedServerDetailsToSteam();
        }

        public override void FinishArena()
        {
            Game.DataEngine.UpdateStandings();
            var finalStandings = Game.DataEngine.Standings;

            // Update pilot ratings and send updates to clients
            Game.Components.OfType<PilotRatingsUpdater>()?.FirstOrDefault()?.EndArena(finalStandings);
            Game.SendPilotRankingsToClientsOnServer();

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
