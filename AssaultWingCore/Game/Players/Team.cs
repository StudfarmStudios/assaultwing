using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game.Logic;
using AW2.Core;

namespace AW2.Game.Players
{
    /// <summary>
    /// A group of co-operating <see cref="Spectator"/> instances.
    /// </summary>
    public class Team
    {
        private List<Spectator> _members;

        public string Name { get; private set; }
        public IEnumerable<Spectator> Members { get { return _members; } }
        public ArenaStatistics ArenaStatistics { get; private set; }

        public Team(string name)
        {
            Name = name;
            _members = new List<Spectator>();
            ArenaStatistics = new ArenaStatistics();
        }

        /// <summary>
        /// Updates <see cref="Members"/> according to <see cref="Spectator.Team"/>.
        /// To be called by the spectator itself after changing its team.
        /// </summary>
        public void UpdateAssignment(Spectator spectator)
        {
            var isMember = _members.Contains(spectator);
            if (spectator.Team == this && !isMember) _members.Add(spectator);
            else if (spectator.Team != this && isMember) _members.Remove(spectator);
        }

        /// <summary>
        /// Resets the spectator's internal state for a new arena.
        /// </summary>
        public virtual void ResetForArena(GameplayMode gameplayMode)
        {
            ArenaStatistics.Reset(gameplayMode);
        }
    }
}
