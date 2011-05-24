using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game;
using AW2.Game.Arenas;
using AW2.Helpers;
using AW2.Net.MessageHandling;

namespace AW2.Core.GameComponents
{
    public class DedicatedServer : AWGameComponent
    {
        private enum EventType { ARENA_FINISH, ARENA_INIT };

        private static readonly TimeSpan ARENA_TIMEOUT = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ARENA_FINISH_COOLDOWN = TimeSpan.FromSeconds(5);

        private bool _initialized;
        private List<ArenaInfo> _arenaInfos;
        private TimeSpan _nextEvent;
        private EventType _nextEventType;
        private string _lastArenaName;

        public new AssaultWing Game { get; private set; }

        public DedicatedServer(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
            Game = game;
            _nextEventType = EventType.ARENA_INIT;
        }

        public override void Update()
        {
            EnsureInitialized();
            HandleEvent();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            MessageHandlers.ActivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(mess => { }));
            Game.NetworkEngine.ConnectToManagementServer();
            if (!Game.StartServer())
            {
                AssaultWingProgram.Instance.Exit();
                _nextEvent = TimeSpan.MaxValue;
            }
            else
            {
                Game.InitializePlayers(0);
                _arenaInfos = Game.DataEngine.GetTypeTemplates<Arena>().Select(a => a.Info).ToList();
            }
        }

        private void HandleEvent()
        {
            if (_nextEvent >= Game.GameTime.TotalRealTime) return;
            switch (_nextEventType)
            {
                case EventType.ARENA_FINISH:
                    _nextEvent = Game.GameTime.TotalRealTime + ARENA_FINISH_COOLDOWN;
                    _nextEventType = EventType.ARENA_INIT;
                    Game.FinishArena();
                    break;
                case EventType.ARENA_INIT:
                    _nextEvent = Game.GameTime.TotalRealTime + ARENA_TIMEOUT;
                    _nextEventType = EventType.ARENA_FINISH;
                    Game.SelectedArenaName = ChooseArenaName();
                    Game.PrepareSelectedArena();
                    Game.StartArena();
                    break;
                default: throw new ApplicationException("Invalid event type " + _nextEventType);
            }
        }

        private string ChooseArenaName()
        {
            var arenaIndex = RandomHelper.GetRandomInt(_arenaInfos.Count);
            var candidate = _arenaInfos[arenaIndex].Name;
            if (candidate == _lastArenaName) candidate = _arenaInfos[(arenaIndex + 1) % _arenaInfos.Count].Name;
            _lastArenaName = candidate;
            return candidate;
        }
    }
}
