using System;
using AW2.Helpers.Serialization;

namespace AW2.Game.Logic
{
    /// <summary>
    /// Statistics of one <see cref="AW2.Game.Players.Spectator"/>
    /// for one <see cref="AW2.Game.Arena"/>.
    /// </summary>
    public class ArenaStatistics : INetworkSerializable
    {
        private int _lives;
        private int _kills;
        private int _deaths;
        private float _damageInflictedToMinions;
        private int _bonusesCollected;

        /// <summary>
        /// How many reincarnations there is left, or negative for infinite lives.
        /// </summary>
        public int Lives { get { return _lives; } set { _lives = value; } }
        public int Kills { get { return _kills; } set { _kills = value; } }
        public int Deaths { get { return _deaths; } set { _deaths = value; } }
        public int KillsWithoutDying { get; set; }
        public float DamageInflictedToMinions { get { return _damageInflictedToMinions; } set { _damageInflictedToMinions = value; } }
        public int BonusesCollected { get { return _bonusesCollected; } set { _bonusesCollected = value; } }
        public Func<float> Rating { get; set; }

        public bool IsEmpty { get { return Kills == 0 && Deaths == 0 && DamageInflictedToMinions == 0 && BonusesCollected == 0; } }

        public ArenaStatistics()
        {
            Lives = -1;
            Rating = () => 0;
        }

        public ArenaStatistics Clone()
        {
            var currentRating = Rating();
            return new ArenaStatistics
            {
                Lives = Lives,
                Kills = Kills,
                Deaths = Deaths,
                KillsWithoutDying = KillsWithoutDying,
                DamageInflictedToMinions = DamageInflictedToMinions,
                BonusesCollected = BonusesCollected,
                Rating = () => currentRating,
            };
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

        public override string ToString()
        {
            return string.Format("Lives={0} Kills={1} Deaths={2} Damage={3} Bonuses={4}", Lives, Kills, Deaths, DamageInflictedToMinions, BonusesCollected);
        }
    }
}
