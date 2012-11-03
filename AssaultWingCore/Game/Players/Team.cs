using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Logic;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Players
{
    /// <summary>
    /// A group of co-operating <see cref="Spectator"/> instances.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{ID}: {Name} is {Color} with {Members.Count} members")]
    public class Team : INetworkSerializable
    {
        public const int UNINITIALIZED_ID = 0;

        private List<Spectator> _members;
        private Func<int, Spectator> _findSpectator;

        public int ID { get; set; }
        public string Name { get; private set; }
        public Color Color { get; set; }
        public IEnumerable<Spectator> Members { get { return _members; } }
        public ArenaStatistics ArenaStatistics { get; private set; }

        public Team(string name, Func<int, Spectator> findSpectator)
        {
            Name = name;
            Color = Color.LightGray;
            _members = new List<Spectator>();
            _findSpectator = findSpectator;
            ArenaStatistics = new ArenaStatistics();
        }

        /// <summary>
        /// Updates <see cref="Members"/> according to <see cref="Spectator.Team"/>.
        /// To be called by the spectator itself after changing its team.
        /// </summary>
        public void UpdateAssignment(Spectator spectator)
        {
            var isMember = Members.Contains(spectator);
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

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer))
                {
                    writer.Write((string)Name);
                    writer.Write((Color)Color);
                }
                ArenaStatistics.Serialize(writer, mode);
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer))
            {
                Name = reader.ReadString();
                Color = reader.ReadColor();
            }
            ArenaStatistics.Deserialize(reader, mode, framesAgo);
        }

        public override string ToString()
        {
            return string.Format("'{0}', ID {1}, {2}, members [{3}]",
                Name, ID, Color, string.Join(", ", Members.Select(m => m.Name)));
        }
    }
}
