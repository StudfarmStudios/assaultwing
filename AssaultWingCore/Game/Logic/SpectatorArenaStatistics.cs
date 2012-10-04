using System;
using AW2.Helpers.Serialization;

namespace AW2.Game.Logic
{
    /// <summary>
    /// Statistics of one <see cref="Spectator"/> from one <see cref="Arena"/>.
    /// </summary>
    public class SpectatorArenaStatistics : INetworkSerializable
    {
        private int _lives;
        private int _kills;
        private int _deaths;
        private float _damageInflictedToMinions;
        private int _bonusesCollected;

        /// <summary>
        /// If positive, how many reincarnations the player has left.
        /// If negative, the player has infinite lives.
        /// If zero, the player cannot play.
        /// </summary>
        public int Lives { get { return _lives; } set { _lives = value; if (Updated != null) Updated(); } }
        public int Kills { get { return _kills; } set { _kills = value; if (Updated != null) Updated(); } }
        public int Deaths { get { return _deaths; } set { _deaths = value; if (Updated != null) Updated(); } }
        public int KillsWithoutDying { get; set; }
        public float DamageInflictedToMinions { get { return _damageInflictedToMinions; } set { _damageInflictedToMinions = value; if (Updated != null) Updated(); } }
        public int BonusesCollected { get { return _bonusesCollected; } set { _bonusesCollected = value; if (Updated != null) Updated(); } }
        public Func<float> Rating { get; set; }

        public event Action Updated;

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
            DamageInflictedToMinions = 0;
            BonusesCollected = 0;
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
                    writer.Write((float)DamageInflictedToMinions);
                    writer.Write((short)BonusesCollected);
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
                DamageInflictedToMinions = reader.ReadSingle();
                BonusesCollected = reader.ReadInt16();
            }
        }
    }
}
