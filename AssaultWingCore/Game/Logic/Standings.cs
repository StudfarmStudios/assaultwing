using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AW2.Game.Players;
using TeamStandings = System.Tuple<AW2.Game.Logic.Standing, AW2.Game.Logic.Standing[]>;

namespace AW2.Game.Logic
{
    public class Standings : IEnumerable<TeamStandings>
    {
        private TeamStandings[] _standings;

        public bool HasTrivialTeams { get; private set; }
        public int TeamCount { get; private set; }
        public int SpectatorCount { get; private set; }
        public Standing this[Team team] { get { return GetTeamStandings(team).Item1; } }
        public Standing this[Spectator spec] { get { return GetTeamStandings(spec.Team).Item2.First(x => x.ID == spec.ID); } }

        public Standing? FindForSpectator(Spectator spec) { return GetTeamStandings(spec.Team).Item2.FirstOrDefault(x => x.ID == spec.ID); }

        public Standings(TeamStandings[] standings)
        {
            _standings = standings;
            HasTrivialTeams = standings.All(x => !x.Item2.Skip(1).Any());
            TeamCount = standings.Count();
            SpectatorCount = standings.Sum(x => x.Item2.Count());
        }

        public IEnumerable<Standing> GetSpectators()
        {
            // TODO: Peter: Trivial teams?
            // Debug.Assert(!HasTrivialTeams);
            return
                from x in _standings
                where x.Item2.Any()
                select x.Item2.First();
        }

        public IEnumerator<TeamStandings> GetEnumerator()
        {
            return _standings.AsEnumerable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _standings.GetEnumerator();
        }

        private TeamStandings GetTeamStandings(Team team)
        {
            return _standings.First(x => x.Item1.ID == team.ID);
        }
    }
}
