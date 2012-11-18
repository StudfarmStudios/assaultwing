using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game.Players;

namespace AW2.Game.Logic
{
    public class TeamChoice
    {
        public enum ChoiceType { ExistingTeam, NewTeam }

        public ChoiceType Type { get; private set; }
        public int ExistingTeamID { get; private set; }
        public string NewTeamName { get; private set; }

        public TeamChoice(Team existingTeamID)
        {
            Type = ChoiceType.ExistingTeam;
            ExistingTeamID = existingTeamID.ID;
        }

        public TeamChoice(string newTeamName)
        {
            Type = ChoiceType.NewTeam;
            NewTeamName = newTeamName;
        }
    }
}
