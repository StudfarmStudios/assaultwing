using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Text;

using Sharprompt;

using AW2.Helpers;
using AW2.Settings;
using AW2.Game.Players;
using AW2.Core;
using System.Globalization;

namespace AW2.UI
{

    public class ServerConsole : IDisposable
    {
        private ConcurrentQueue<DedicatedServerEvent> ServerEventQueue = new ConcurrentQueue<DedicatedServerEvent>();
        private ConcurrentQueue<MainThreadOperation> MainThreadOperationQueue = new ConcurrentQueue<MainThreadOperation>();
        private ConcurrentQueue<ReturnInfo> ReturnInfoQueue = new ConcurrentQueue<ReturnInfo>();

        private AssaultWing<DedicatedServerEvent> Game;
        private Thread ConsoleThread;

        private AWSettings Settings { get { return Game.Settings; } }

        public ServerConsole(AssaultWing<DedicatedServerEvent> game)
        {
            ConsoleThread = new Thread(ConsoleThreadFunction);
            Game = game;
        }

        public void Start()
        {
            AW2.Helpers.Log.Written += AddToLogView;
            ConsoleThread.Start();
        }

        public void Dispose()
        {
            ConsoleThread.Join();
            AW2.Helpers.Log.Written -= AddToLogView;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        private enum MainThreadOperation
        {
            GetReturnInfo
        };

        private struct PlayerInfo
        {
            public int Number;
            public Spectator Spectator;
            public TimeSpan? Ping;

            public override string ToString()
            {
                string teamName = Spectator.Team.Name;
                string ping = "-";
                string ranking = "rank: - rating: -";
                if (Spectator.IsRemote)
                {
                    ping = $"{Ping?.TotalMilliseconds:0.#}ms";
                }
                if (teamName == Spectator.Name)
                {
                    teamName = "-";
                }
                if (Spectator.Ranking.IsValid)
                {
                    ranking = Spectator.Ranking.ToString();
                }
                return $"{Number} name:{Spectator.Name} team:{teamName} ping:{ping} deaths:{Spectator.ArenaStatistics.Deaths} kills:{Spectator.ArenaStatistics.Kills} deaths:{Spectator.ArenaStatistics.Deaths} {ranking}";
            }
        }

        private class ReturnInfo
        {
            public List<PlayerInfo> PlayerInfos = new List<PlayerInfo>();
        }

        private static String HelpText =
        @"Welcome!

        This is the Assault Wing dedicated server console.

        Type a command and press Enter to execute it. Additional parameters are asked separately.
        Note that you can use the arrow keys to select a command from the menu below the prompt.

        If the prompt gets obscured by the server logging, simply press ENTER to get it back.

        Type 'Help' and press ENTER to see this help text again.
        
        Have fun!
        ";

        private void ConsoleThreadFunction()
        {
            Thread.Sleep(6000); // Allow the server to spam when it starts before showing the prompt.
            Console.WriteLine("\n" + HelpText + "\n");
            try
            {
                var keepConsoleGoing = true;
                while (keepConsoleGoing)
                {
                    var command = Prompt.Select<ServerConsoleCommand>("Select Assault Wing Server Command");

                    if (command == ServerConsoleCommand.Stop)
                    {
                        keepConsoleGoing = false;
                    }

                    switch (command)
                    {
                        case ServerConsoleCommand.Help:
                            Console.WriteLine(HelpText);
                            break;
                        case ServerConsoleCommand.Stop:
                            ServerEventQueue.Enqueue(new DedicatedServerEvent { Type = DedicatedServerEvent.EventType.Stop });
                            break;
                        case ServerConsoleCommand.EndRound:
                            ServerEventQueue.Enqueue(new DedicatedServerEvent { Type = DedicatedServerEvent.EventType.EndRound });
                            break;
                        case ServerConsoleCommand.Say:
                            var message = Prompt.Input<string>("Say to players");
                            if (message.Trim().Length > 0)
                            {
                                ServerEventQueue.Enqueue(new DedicatedServerEvent { Type = DedicatedServerEvent.EventType.Say, StringPayload = message });
                            }
                            break;
                        case ServerConsoleCommand.SelectNextArena:
                            DoConsoleSelectNextArena();
                            break;
                        case ServerConsoleCommand.SetRoundLength:
                            DoConsoleSetRoundLength();
                            break;
                        case ServerConsoleCommand.SetBotsEnabled:
                            DoConsoleSetBotsEnabled();
                            break;
                        case ServerConsoleCommand.SaveSettings:
                            Settings.ToFile();
                            break;
                        case ServerConsoleCommand.Kick:
                            DoConsoleKick();
                            break;
                        case ServerConsoleCommand.Players:
                            DoConsolePlayers();
                            break;
                        default:
                            Log.Write($"Command not implemented yet: {command}");
                            break;
                    }

                    Thread.Sleep(1000); // Allow the possible log messages resulting from the command to be printed before the next prompt.
                }
            }
            catch (Exception e)
            {
                Log.Write("Server console command support failed: {0}", e.Message);
            }
        }

        private ReturnInfo GetReturnInfo()
        {
            MainThreadOperationQueue.Enqueue(MainThreadOperation.GetReturnInfo);

            ReturnInfo info;
            while (!ReturnInfoQueue.TryDequeue(out info))
            {
                Thread.Sleep(100);
            }
            return info;
        }

        public DedicatedServerEvent? Update()
        {
            if (MainThreadOperationQueue.TryDequeue(out var operation))
            {
                switch (operation)
                {
                    case MainThreadOperation.GetReturnInfo:
                        var spectators = Game.DataEngine.Spectators;
                        List<PlayerInfo> playerInfos = spectators
                            .OrderBy(s => s.LastDisconnectTime)
                            .ThenBy(s => s.ConnectionID)
                            .Select((spec, i) =>
                        {
                            var info = new PlayerInfo() { Number = i + 1 };
                            if (spec.IsRemote)
                            {
                                info.Ping = Game.NetworkEngine.GetClientPingTime(spec.ConnectionID);
                            }
                            info.Spectator = spec;
                            return info;
                        }).ToList();

                        ReturnInfoQueue.Enqueue(new ReturnInfo() { PlayerInfos = playerInfos }); // Make a copy to avoid threading problems.
                        break;
                }
            }

            if (ServerEventQueue.TryDequeue(out var command))
            {
                if (command.EventSourceMessage is null)
                {
                    command.EventSourceMessage = "a console command";
                }

                Game.ExternalProgramLogicEvent(command);

                return command;
            }
            else
            {
                return null;
            }
        }

        private void AddToLogView(string text)
        {
#if !DEBUG
            // In debug builds the log is written to the console, but in 
            // release builds dedicated server would not see any logs on screen.
            Console.WriteLine(text);
#endif
        }

        private void DoConsoleKick()
        {
            var info = GetReturnInfo();
            if (info.PlayerInfos.Count == 0)
            {
                Console.WriteLine("No players to kick");
                return;
            }
            var infos = new List<PlayerInfo>();
            var cancelItem = new PlayerInfo();
            infos.Add(cancelItem);
            infos.AddRange(info.PlayerInfos);
            var selectOptions = new SelectOptions<PlayerInfo>
            {
                Items = infos,
                PageSize = 10,
                TextSelector = info =>
                {
                    if (info.Spectator == null)
                    {
                        return "[Cancel]";
                    }
                    else
                    {
                        return info.ToString();
                    }
                },
                Message = "Select player to kick",
                DefaultValue = cancelItem
            };
            var playerToKick = Prompt.Select<PlayerInfo>(selectOptions);
            if (playerToKick.Spectator != null)
            {
                var reasonMessage = Prompt.Input<string>("Show reason to players");
                ServerEventQueue.Enqueue(new DedicatedServerEvent
                {
                    Type = DedicatedServerEvent.EventType.Kick,
                    StringPayload = playerToKick.Spectator.PilotId,
                    StringPayload2 = reasonMessage
                });
            }
        }

        private void DoConsolePlayers()
        {
            var infos = GetReturnInfo().PlayerInfos;
            StringBuilder sb = new StringBuilder();
            sb.Append("Players:\n");
            foreach (var it in infos)
            {
                sb.Append(it.ToString() + "\n");
            }
            Console.WriteLine(sb.ToString());
        }

        private void DoConsoleSetRoundLength()
        {
            var minutes = Prompt.Input<int>("Give round length in minutes",
                defaultValue: (int)Settings.Net.DedicatedServerArenaTimeout.TotalMinutes,
                validators: new[] { SharpromptUtil.IntMinMaxInclusive(1, 180) });

            ServerEventQueue.Enqueue(new DedicatedServerEvent
            {
                Type = DedicatedServerEvent.EventType.SetRoundLength,
                TimeSpanPayload = TimeSpan.FromMinutes(minutes)
            });
        }

        private void DoConsoleSetBotsEnabled()
        {
            var botsEnabled = Prompt.Confirm("Bots enabled", defaultValue: Settings.Players.BotsEnabled);

            ServerEventQueue.Enqueue(new DedicatedServerEvent
            {
                Type = DedicatedServerEvent.EventType.SetBotsEnabled,
                BooleanPayload = botsEnabled
            });
        }

        private void DoConsoleSelectNextArena()
        {
            var arena = Prompt.Select<string>("Select next arena",
                items: Game.DataEngine.GameplayMode.Arenas
            );
            if (arena.Trim().Length > 0)
            {
                ServerEventQueue.Enqueue(new DedicatedServerEvent { Type = DedicatedServerEvent.EventType.SelectNextArena, StringPayload = arena });
            }

        }
    }
}
