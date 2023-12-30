using AW2.Game;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Stats;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A network message used to sync PilotRanking in between the clients and the server.
    /// </summary>
    [MessageType(0x2c, false)]
    public class PilotRankingMessage : GameplayMessage
    {
        /// <summary>
        /// A player identifier to whom this PilotRanking update is meant to.
        /// </summary>
        public int PlayerID { get; set; }
        public PilotRanking PilotRanking;

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.SerializeBody(writer);
                checked
                {
                    writer.Write((short)PlayerID);
                    writer.Write((byte)PilotRanking.State);
                    writer.Write((int)PilotRanking.Rank);
                    writer.Write((long)PilotRanking.RankDownloadedTime.Ticks);
                    writer.Write((int)PilotRanking.Rating);
                    writer.Write((long)PilotRanking.RatingAwardedTime.Ticks);
                }
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            PlayerID = reader.ReadInt16();
            PilotRanking = new PilotRanking()
            {
                State = (PilotRanking.StateType)reader.ReadByte(),
                Rank = reader.ReadInt32(),
                RankDownloadedTime = new DateTime(reader.ReadInt64()),
                Rating = reader.ReadInt32(),
                RatingAwardedTime = new DateTime(reader.ReadInt64())
            };
        }

        public override string ToString()
        {
            return base.ToString() + " [" + PlayerID + ", " + PilotRanking + "]";
        }
    }
}
