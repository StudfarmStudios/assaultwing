using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Helpers.Serialization;

namespace AW2.Game
{
    /// <summary>
    /// Statistics of one <see cref="Spectator"/> from one <see cref="Arena"/>.
    /// </summary>
    public class SpectatorArenaStatistics : INetworkSerializable
    {
        /// <summary>
        /// If positive, how many reincarnations the player has left.
        /// If negative, the player has infinite lives.
        /// If zero, the player cannot play.
        /// </summary>
        public int Lives { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int KillsWithoutDying { get; set; }

        public SpectatorArenaStatistics()
        {
            Lives = -1;
        }

        public SpectatorArenaStatistics(GameplayMode gameplayMode)
        {
            Lives = gameplayMode.StartLives;
        }

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            checked
            {
                if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
                {
                    writer.Write((short)Lives);
                    writer.Write((short)Kills);
                    writer.Write((short)Deaths);
                }
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if (mode.HasFlag(SerializationModeFlags.VaryingDataFromServer))
            {
                Lives = reader.ReadInt16();
                Kills = reader.ReadInt16();
                Deaths = reader.ReadInt16();
            }
        }
    }

    /// <summary>
    /// The entry of one player in a score table.
    /// </summary>
    public class Standing
    {
        public string Name { get; private set; }
        public Color Color { get; private set; }
        public bool IsLocal { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int SpectatorID { get; private set; }
        public string LoginToken { get; private set; }

        public Standing(Spectator spec, int score)
        {
            Name = spec.Name;
            Color = spec.Color;
            IsLocal = spec.IsLocal;
            Score = score;
            Kills = spec.ArenaStatistics.Kills;
            Deaths = spec.ArenaStatistics.Deaths;
            SpectatorID = spec.ID;
            LoginToken = spec.LoginToken;
        }
    }

    /// <summary>
    /// A bunch of parameters of the gameplay.
    /// </summary>
    public class GameplayMode
    {
        /// <summary>
        /// The types of ship available for selection in the gameplay mode.
        /// </summary>
        public string[] ShipTypes { get; set; }

        /// <summary>
        /// The types of extra devices available for selection in the gameplay mode.
        /// </summary>
        public string[] ExtraDeviceTypes { get; set; }

        /// <summary>
        /// The types of secondary weapon available for selection in the gameplay mode.
        /// </summary>
        public string[] Weapon2Types { get; set; }

        /// <summary>
        /// Number of lives of a player when starting a new arena.
        /// </summary>
        public int StartLives
        {
            get
            {
                return -1; // infinite lives
            }
        }

        public int CalculateScore(SpectatorArenaStatistics statistics)
        {
            return 2 * statistics.Kills - statistics.Deaths;
        }

        public IEnumerable<Standing> GetStandings(IEnumerable<Spectator> spectators)
        {
            return
                from spec in spectators
                let stats = spec.ArenaStatistics
                let score = CalculateScore(stats)
                orderby score descending, stats.Kills descending, spec.Name
                select new Standing(spec, score);
        }

        public bool ArenaFinished(Arena arena, IEnumerable<Spectator> spectators)
        {
            if (spectators.Count() < 2) return false;
            int spectatorsAlive = spectators.Count(player => player.ArenaStatistics.Lives != 0);
            return spectatorsAlive < 2;
        }
    }
}
