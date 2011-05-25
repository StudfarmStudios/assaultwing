using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game;
using AW2.Game.Arenas;
using AW2.Helpers;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;

namespace AW2.Core.GameComponents
{
    public class DedicatedServer : AWGameComponent
    {
        private enum EventType { ARENA_FINISH, ARENA_INIT };

        private static readonly TimeSpan ARENA_TIMEOUT = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ARENA_FINISH_COOLDOWN = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan[] ARENA_TIMEOUT_MESSAGES = new[]
        {
            TimeSpan.FromHours(1),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(20),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(10),
        };

        private bool _initialized;
        private List<ArenaInfo> _arenaInfos;
        private TimeSpan _nextEvent;
        private EventType _nextEventType;
        private string _lastArenaName;
        private List<TimeSpan> _arenaTimeoutMessages;

        public new AssaultWing Game { get; private set; }

        private TimeSpan Now { get { return Game.GameTime.TotalRealTime; } }

        public DedicatedServer(AssaultWing game, int updateOrder)
            : base(game, updateOrder)
        {
            Game = game;
            _nextEventType = EventType.ARENA_INIT;
        }

        public override void Update()
        {
            EnsureInitialized();
            SendMessages();
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
                Game.SelectedArenaName = ChooseArenaName();
            }
        }

        private void SendMessages()
        {
            if (_nextEventType == EventType.ARENA_FINISH && _arenaTimeoutMessages.Any() && Now + _arenaTimeoutMessages[0] >= _nextEvent)
            {
                var text = "Arena will be closed in " + _arenaTimeoutMessages[0].ToDurationString();
                var message = new PlayerMessageMessage { Message = new PlayerMessage(text, PlayerMessage.DEFAULT_COLOR), };
                foreach (var plr in Game.DataEngine.Players)
                {
                    if (!plr.IsRemote) continue;
                    message.PlayerID = plr.ID;
                    Game.NetworkEngine.GetGameClientConnection(plr.ConnectionID).Send(message);
                }
                _arenaTimeoutMessages.RemoveAt(0);
            }
        }

        private void HandleEvent()
        {
            if (_nextEvent >= Now) return;
            switch (_nextEventType)
            {
                case EventType.ARENA_FINISH:
                    _nextEvent = Now + ARENA_FINISH_COOLDOWN;
                    _nextEventType = EventType.ARENA_INIT;
                    Game.FinishArena();
                    Game.SelectedArenaName = ChooseArenaName();
                    break;
                case EventType.ARENA_INIT:
                    _nextEvent = Now + ARENA_TIMEOUT;
                    _nextEventType = EventType.ARENA_FINISH;
                    Game.PrepareSelectedArena();
                    Game.StartArena();
                    _arenaTimeoutMessages = ARENA_TIMEOUT_MESSAGES.Where(t => t < ARENA_TIMEOUT).OrderByDescending(t => t.Ticks).ToList();
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
