using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Core.GameComponents;

namespace AW2.UI
{
    // TODO: Fix naming in a separate PR, I think this should be called DedicatedServerLogic and the base class just ServerLogic. 
    public class DedicatedServerLogicStandalone : DedicatedServerLogic<DedicatedServerEvent>
    {
        public DedicatedServerLogicStandalone(AssaultWing<DedicatedServerEvent> game, bool consoleServer)
            : base(game, consoleServer)
        {
        }

        public override void ExternalEvent(DedicatedServerEvent e)
        {
            switch (e.Type)
            {
                case DedicatedServerEvent.EventType.Stop:
                    DedicatedServer.CommandAdminSayToAllPlayers($"The server is shutting down. ({e.EventSourceMessage}.)");
                    DedicatedServer.CommandFinishArenaNow();
                    break;
                case DedicatedServerEvent.EventType.EndRound:
                    DedicatedServer.CommandAdminSayToAllPlayers($"Ending this arena. ({e.EventSourceMessage}.)");
                    DedicatedServer.CommandFinishArenaNow();
                    break;
                case DedicatedServerEvent.EventType.Say:
                    DedicatedServer.CommandAdminSayToAllPlayers(e.StringPayload);
                    break;

                case DedicatedServerEvent.EventType.SelectNextArena:
                    var arenaName = e.StringPayload;
                    DedicatedServer.CommandAdminSayToAllPlayers($"Selected the {arenaName} as the next arena. ({e.EventSourceMessage}.)");
                    DedicatedServer.CommandSelectNextArena(arenaName);
                    break;

                case DedicatedServerEvent.EventType.Kick:
                    var playerPilotId = e.StringPayload;
                    var reason = e.StringPayload2;
                    var kickedPlayer = DedicatedServer.CommandKickPlayer(playerPilotId, reason);
                    if (kickedPlayer != null)
                    {
                        DedicatedServer.CommandAdminSayToAllPlayers($"Kicked {kickedPlayer.Name}. Reason: {reason}. ({e.EventSourceMessage}.)");
                    }
                    break;

                case DedicatedServerEvent.EventType.SetRoundLength:
                    DedicatedServer.CommandAdminSayToAllPlayers($"Round length set to {e.TimeSpanPayload}. ({e.EventSourceMessage}.)");
                    var timeLeft = DedicatedServer.CommandSetRoundLength(e.TimeSpanPayload);
                    if (timeLeft < DedicatedServer.ArenaCommandEndGraceTime)
                    {
                        DedicatedServer.CommandAdminSayToAllPlayers($"Arena ending. ({e.EventSourceMessage})");
                    }
                    break;
                case DedicatedServerEvent.EventType.SetBotsEnabled:
                    DedicatedServer.CommandAdminSayToAllPlayers($"BotsEnabled set to {e.BooleanPayload}. ({e.EventSourceMessage}.)");
                    Game.Settings.Players.BotsEnabled = e.BooleanPayload;
                    break;
            }
            base.ExternalEvent(e);
        }
    }
}
