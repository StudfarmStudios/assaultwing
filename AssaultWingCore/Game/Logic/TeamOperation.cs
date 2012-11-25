using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game.Players;

namespace AW2.Game.Logic
{
    public class TeamOperation
    {
        public enum ChoiceType
        {
            AssignToExistingTeam,
            AssignToNewTeam,
            CreateToExistingTeam,
            CreateToNewTeam,
            Remove,
        }

        public ChoiceType Type { get; private set; }
        public Team ExistingTeam { get; private set; }
        public Spectator ExistingSpectator { get; private set; }
        public string NewTeamName { get; private set; }
        public string NewSpectatorName { get; private set; }

        public static TeamOperation AssignToExistingTeam(Team existingTeam, Spectator existingSpectator)
        {
            return new TeamOperation
            {
                Type = ChoiceType.AssignToExistingTeam,
                ExistingTeam = existingTeam,
                ExistingSpectator = existingSpectator,
            };
        }

        public static TeamOperation AssignToNewTeam(string newTeamName, Spectator existingSpectator)
        {
            return new TeamOperation
            {
                Type = ChoiceType.AssignToNewTeam,
                NewTeamName = newTeamName,
                ExistingSpectator = existingSpectator,
            };
        }

        public static TeamOperation CreateToExistingTeam(Team existingTeam, string newSpectatorName)
        {
            return new TeamOperation
            {
                Type = ChoiceType.CreateToExistingTeam,
                ExistingTeam = existingTeam,
                NewSpectatorName = newSpectatorName,
            };
        }

        public static TeamOperation CreateToNewTeam(string newTeamName, string newSpectatorName)
        {
            return new TeamOperation
            {
                Type = ChoiceType.CreateToNewTeam,
                NewTeamName = newTeamName,
                NewSpectatorName = newSpectatorName,
            };
        }

        public static TeamOperation Remove(Spectator spectator)
        {
            return new TeamOperation
            {
                Type = ChoiceType.Remove,
                ExistingSpectator = spectator,
            };
        }

        private TeamOperation() { }
    }
}
