using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game;
using AW2.Game.Arenas;
using AW2.Helpers;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;
using AW2.Settings;

namespace AW2.Core.GameComponents
{
    public class DedicatedServer : AWGameComponent
    {
        private enum EventType { ARENA_FINISH, ARENA_INIT };

        private bool _initialized;
        private List<ArenaInfo> _arenaInfos;
        private TimeSpan _nextEvent;
        private EventType _nextEventType;
        private string _lastArenaName;
        private List<TimeSpan> _arenaTimeoutMessages;

        public new AssaultWing Game { get; private set; }

        private NetSettings Settings { get { return Game.Settings.Net; } }
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
            Game.MessageHandlers.ActivateHandlers(Game.MessageHandlers.GetStandaloneMenuHandlers(mess => { }));
            Game.NetworkEngine.ConnectToManagementServer();
            if (!Game.StartServer())
            {
                AssaultWingProgram.Instance.Exit();
                _nextEvent = TimeSpan.MaxValue;
            }
            else
            {
                _arenaInfos = (
                    from arena in Game.DataEngine.GetTypeTemplates<Arena>()
                    let info = arena.Info
                    where !Settings.DedicatedServerArenaNames.Any() || Settings.DedicatedServerArenaNames.Contains(info.Name)
                    select info
                    ).ToList();
                Game.SelectedArenaName = ChooseArenaName();
            }
        }

        private void SendMessages()
        {
            if (_nextEventType == EventType.ARENA_FINISH && _arenaTimeoutMessages.Any() && Now + _arenaTimeoutMessages[0] >= _nextEvent)
            {
                var text = "Arena will change in " + _arenaTimeoutMessages[0].ToDurationString("day", "hour", "minute", "second", usePlurals: true);
                foreach (var plr in Game.DataEngine.Players)
                {
                    if (!plr.IsRemote) continue;
                    var message = new PlayerMessageMessage { Message = new PlayerMessage(text, PlayerMessage.DEFAULT_COLOR), };
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
                    _nextEvent = Now + Settings.DedicatedServerArenaFinishCooldown;
                    _nextEventType = EventType.ARENA_INIT;
                    Game.FinishArena();
                    Game.SelectedArenaName = ChooseArenaName();
                    break;
                case EventType.ARENA_INIT:
                    _nextEvent = Now + Settings.DedicatedServerArenaTimeout;
                    _nextEventType = EventType.ARENA_FINISH;
                    Game.PrepareSelectedArena();
                    Game.StartArena();
                    _arenaTimeoutMessages = (
                        from time in Settings.DedicatedServerArenaTimeoutMessages
                        where time < Settings.DedicatedServerArenaTimeout
                        orderby time.Ticks descending
                        select time
                        ).ToList();
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
