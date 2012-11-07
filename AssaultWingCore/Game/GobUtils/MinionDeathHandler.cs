using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Game.Players;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    public class MinionDeathHandler
    {
        public static void OnMinionDeath(Coroner coroner)
        {
            if (coroner.Game.NetworkMode == NetworkMode.Client) return;
            SendStats(coroner);
            SendMessages(coroner);
        }

        private static void SendStats(Coroner coroner)
        {
            var stats = coroner.Game.Stats;
            switch (coroner.DeathType)
            {
                default: throw new ApplicationException("Invalid DeathType " + coroner.DeathType);
                case Coroner.DeathTypeType.Kill:
                    stats.Send(new
                    {
                        Killer = stats.GetStatsString(coroner.KillerSpectator),
                        Victim = stats.GetStatsString(coroner.KilledSpectator),
                        Pos = coroner.DamageInfo.Target.Pos,
                    });
                    break;
                case Coroner.DeathTypeType.TeamKill:
                    coroner.Game.Stats.Send(new
                    {
                        TeamKiller = stats.GetStatsString(coroner.KillerSpectator),
                        Victim = stats.GetStatsString(coroner.KilledSpectator),
                        Pos = coroner.DamageInfo.Target.Pos,
                    });
                    break;
                case Coroner.DeathTypeType.Accident:
                    coroner.Game.Stats.Send(new
                    {
                        Suicide = stats.GetStatsString(coroner.KilledSpectator),
                        Pos = coroner.DamageInfo.Target.Pos,
                    });
                    break;
            }
        }

        private static void SendMessages(Coroner coroner)
        {
            var killerPlayer = coroner.KillerSpectator as Player;
            var killedPlayer = coroner.KilledSpectator as Player;
            var players = coroner.Game.DataEngine.Players;
            switch (coroner.DeathType)
            {
                default: throw new ApplicationException("Unexpected DeathType " + coroner.DeathType);
                case Coroner.DeathTypeType.Kill:
                    CreateKillMessage(coroner.KillerSpectator, coroner.DamageInfo.Target.Pos);
                    if (killerPlayer != null) killerPlayer.Messages.Add(new PlayerMessage(coroner.MessageToKiller, PlayerMessage.KILL_COLOR));
                    if (killedPlayer != null) killedPlayer.Messages.Add(new PlayerMessage(coroner.MessageToCorpse, PlayerMessage.DEATH_COLOR));
                    break;
                case Coroner.DeathTypeType.TeamKill:
                    CreateTeamKillMessage(coroner.KillerSpectator, coroner.DamageInfo.Target.Pos);
                    if (killerPlayer != null) killerPlayer.Messages.Add(new PlayerMessage(coroner.MessageToKiller, PlayerMessage.SUICIDE_COLOR));
                    if (killedPlayer != null) killedPlayer.Messages.Add(new PlayerMessage(coroner.MessageToCorpse, PlayerMessage.DEATH_COLOR));
                    break;
                case Coroner.DeathTypeType.Accident:
                    CreateSuicideMessage(coroner.KilledSpectator, coroner.DamageInfo.Target.Pos);
                    if (killedPlayer != null) killedPlayer.Messages.Add(new PlayerMessage(coroner.MessageToCorpse, PlayerMessage.SUICIDE_COLOR));
                    break;
            }
            var bystanderMessage = new PlayerMessage(coroner.MessageToBystander, PlayerMessage.DEFAULT_COLOR);
            foreach (var plr in coroner.GetBystandingPlayers(players)) plr.Messages.Add(bystanderMessage);
            if (coroner.SpecialMessage != null)
            {
                var specialMessage = new PlayerMessage(coroner.SpecialMessage, PlayerMessage.SPECIAL_KILL_COLOR);
                foreach (var plr in players) plr.Messages.Add(specialMessage);
            }
        }

        private static void CreateSuicideMessage(Spectator perpetrator, Vector2 pos)
        {
            CreateDeathMessage(perpetrator, pos, "b_icon_take_life");
        }

        private static void CreateTeamKillMessage(Spectator perpetrator, Vector2 pos)
        {
            CreateDeathMessage(perpetrator, pos, "b_icon_take_life");
        }

        private static void CreateKillMessage(Spectator perpetrator, Vector2 pos)
        {
            CreateDeathMessage(perpetrator, pos, "b_icon_add_kill");
        }

        private static void CreateDeathMessage(Spectator perpetrator, Vector2 Pos, string iconName)
        {
            Gob.CreateGob<ArenaMessage>(perpetrator.Game, (CanonicalString)"deathmessage", gob =>
            {
                gob.ResetPos(Pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                gob.Message = perpetrator.Name;
                gob.IconName = iconName;
                gob.DrawColor = perpetrator.Color;
                perpetrator.Game.DataEngine.Arena.Gobs.Add(gob);
            });
        }
    }
}
