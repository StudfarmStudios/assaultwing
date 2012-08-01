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
        private IEnumerable<ArenaInfo> ArenaInfos
        {
            get
            {
                return
                    from arena in Game.DataEngine.GetTypeTemplates<Arena>()
                    let info = arena.Info
                    where !Settings.DedicatedServerArenaNames.Any() || Settings.DedicatedServerArenaNames.Contains(info.Name)
                    select info;
            }
        }
        private TimeSpan _nextEvent;
        private EventType _nextEventType;
        private string _previousArenaName;
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
            Game.NetworkEngine.EnsureConnectionToManagementServer();
            if (Game.StartServer() != null)
            {
                AssaultWingProgram.Instance.Exit();
                _nextEvent = TimeSpan.MaxValue;
            }
            else
            {
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
                    Game.RefreshGameSettings();
                    Game.SelectedArenaName = ChooseArenaName();
                    break;
                case EventType.ARENA_INIT:
                    var arenaFinishTime = Now + Settings.DedicatedServerArenaTimeout;
                    _nextEvent = arenaFinishTime;
                    _nextEventType = EventType.ARENA_FINISH;
                    Game.LoadSelectedArena();
                    foreach (var conn in Game.NetworkEngine.GameClientConnections) conn.PingInfo.AllowLatePingsForAWhile();
                    Game.DataEngine.Arena.Initialize();
                    Game.StartArena();
                    Game.DataEngine.ArenaFinishTime = arenaFinishTime;
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
            var arenaInfos = ArenaInfos.ToArray();
            var arenaIndex = RandomHelper.GetRandomInt(arenaInfos.Length);
            var candidate = arenaInfos[arenaIndex].Name;
            if (candidate == _previousArenaName) candidate = arenaInfos[(arenaIndex + 1) % arenaInfos.Length].Name;
            _previousArenaName = candidate;
            return candidate;
        }
    }
}
