using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game;
using AW2.Game.Logic;
using AW2.Game.Players;
using AW2.Helpers;
using AW2.Net.Connections;
using AW2.Net.Messages;
using AW2.Settings;
using Microsoft.Xna.Framework;

namespace AW2.Core.GameComponents
{
    public class DedicatedServer : AWGameComponent
    {
        public static readonly TimeSpan ArenaCommandEndGraceTime = TimeSpan.FromSeconds(3);

        private enum EventType { ARENA_FINISH, ARENA_INIT };

        private bool _initialized;
        private IEnumerable<string> Arenas
        {
            get
            {
                return Game.DataEngine.GameplayMode.Arenas
                    .Where(arena => !Settings.DedicatedServerArenaNames.Any() || Settings.DedicatedServerArenaNames.Contains(arena));
            }
        }
        private TimeSpan _nextEvent;
        private EventType _nextEventType;
        private string _previousArenaName;
        private string? _nextArenaName;
        private List<TimeSpan> _arenaTimeoutMessages;

        public new AssaultWingCore Game { get; private set; }

        private NetSettings Settings { get { return Game.Settings.Net; } }
        private TimeSpan Now { get { return Game.GameTime.TotalRealTime; } }

        private List<(string, DateTime)> PilotIdsToDrop = new List<(string, DateTime)>();

        public DedicatedServer(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            Game = game;
            _nextEventType = EventType.ARENA_INIT;
        }

        public override void Update()
        {
            EnsureInitialized();
            DropPilots();
            SendMessages();
            HandleEvent();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            // TODO: Peter: Steam network, do we need something like the GetStandaloneMenuHandlers that was here
            if (Game.StartServer() != null)
            {
                // TODO: Peter: Handle exit from DedicatedServer
                // AssaultWingProgram.Instance.Exit();
                _nextEvent = TimeSpan.MaxValue;
            }
            else
            {
                Game.DataEngine.GameplayMode = ChooseGameplayMode();
                Game.SelectedArenaName = ChooseArenaName();
            }
        }

        private void DropPilots()
        {
            var now = DateTime.UtcNow;
            List<string> pilotIdsDueToDrop = PilotIdsToDrop.Where(pair => pair.Item2 < now).Select(pair => pair.Item1).ToList();
            foreach (var pilotId in pilotIdsDueToDrop)
            {
                var specAndConn = FindPlayerAndConnectionByPilotId(pilotId);
                if (specAndConn is not null)
                {
                    var (spec, conn) = specAndConn.Value;
                    Game.NetworkEngine.DropClient(conn);
                }
            }
            PilotIdsToDrop.RemoveAll(p => pilotIdsDueToDrop.Contains(p.Item1));
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
                    Game.DataEngine.GameplayMode = ChooseGameplayMode();
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

        public void CommandFinishArenaNow()
        {
            TimeArenaFinishEvent(ArenaCommandEndGraceTime);
        }

        public void TimeArenaFinishEvent(TimeSpan fromNow)
        {
            var arenaFinishTime = Now + fromNow;
            _nextEvent = arenaFinishTime;
            _nextEventType = EventType.ARENA_FINISH;
            Game.DataEngine.ArenaFinishTime = arenaFinishTime;
        }

        public void CommandAdminSayToAllPlayers(string text)
        {
            var messageContent = text.Trim();
            if (messageContent == "") return;
            var preText = "ADMIN>";
            var textColor = Color.White;
            var message = new PlayerMessage(preText, messageContent, textColor);
            Log.Write("Admin says: " + messageContent);
            foreach (var plr in Game.DataEngine.Players) plr.Messages.Add(message);
        }

        private (Spectator, GameClientConnection)? FindPlayerAndConnectionByPilotId(string pilotId)
        {
            Spectator? spec = Game.DataEngine.Spectators.FirstOrDefault(s => s.PilotId == pilotId);
            GameClientConnection? conn = Game.NetworkEngine.GameClientConnections.FirstOrDefault(c => c.ID == spec?.ConnectionID);
            if (spec is not null && conn is not null)
            {
                return (spec, conn);
            }
            else
            {
                return null;
            }
        }

        public Spectator? CommandKickPlayer(string pilotId, string reason)
        {
            var specAndConn = FindPlayerAndConnectionByPilotId(pilotId);
            if (specAndConn is not null)
            {
                var (spec, conn) = specAndConn.Value;
                var shutdownNotice = new ConnectionClosingMessage { Info = $"kicked: {reason}" };
                conn.Send(shutdownNotice);
                PilotIdsToDrop.Add((pilotId, DateTime.UtcNow + TimeSpan.FromSeconds(2))); // allow some time to show the mssage
                Log.Write($"Player {spec.Name} kicked by the server: {reason}");
                return spec;
            }
            else
            {
                Log.Write($"Couldn't find player to kick");
                return null;
            }
        }

        public void CommandSelectNextArena(string arenaName)
        {
            _nextArenaName = arenaName;
        }

        public TimeSpan CommandSetRoundLength(TimeSpan timeSpan)
        {
            Settings.DedicatedServerArenaTimeout = timeSpan;
            var timeLeft = timeSpan - Game.DataEngine.ArenaTotalTime;
            if (timeLeft < ArenaCommandEndGraceTime)
            {
                TimeArenaFinishEvent(ArenaCommandEndGraceTime);
            }
            else
            {
                TimeArenaFinishEvent(timeLeft);
            }
            return timeLeft;
        }

        private GameplayMode ChooseGameplayMode()
        {
            var gameplayModes = Game.DataEngine.GetTypeTemplates<GameplayMode>().ToArray();
            return gameplayModes[RandomHelper.GetRandomInt(gameplayModes.Length)];
        }

        private string ChooseArenaName()
        {
            if (_nextArenaName is null)
            {
                var arenaInfos = Arenas.ToArray();
                var arenaIndex = RandomHelper.GetRandomInt(arenaInfos.Length);
                var candidate = arenaInfos[arenaIndex];
                if (candidate == _previousArenaName) candidate = arenaInfos[(arenaIndex + 1) % arenaInfos.Length];
                _previousArenaName = candidate;
                return candidate;
            }
            else
            {
                var candidate = _nextArenaName;
                _nextArenaName = null;
                return candidate;
            }
        }
    }
}
