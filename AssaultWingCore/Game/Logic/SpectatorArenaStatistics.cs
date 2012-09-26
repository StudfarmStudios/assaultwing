using System;
using AW2.Helpers.Serialization;

namespace AW2.Game.Logic
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
        public Func<float> Rating { get; set; }

        public SpectatorArenaStatistics()
        {
            Lives = -1;
            Rating = () => 0;
        }

        public void Reset(GameplayMode gameplayMode)
        {
            Lives = gameplayMode.StartLives;
            Kills = 0;
            Deaths = 0;
            KillsWithoutDying = 0;
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
}
