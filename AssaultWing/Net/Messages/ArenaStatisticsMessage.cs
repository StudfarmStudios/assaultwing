using AW2.Helpers.Serialization;
using System.Collections.Generic;
using AW2.Game;
using System;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating
    /// the current arena standings.
    /// </summary>
    [MessageType(0x31, false)]
    public class ArenaStatisticsMessage : StreamMessage
    {
        private List<int> _spectatorIDs { get; set; }

        public ArenaStatisticsMessage()
        {
            _spectatorIDs = new List<int>();
        }

        /// <summary>
        /// Adds the arena statistics of a spectator to the message.
        /// </summary>
        public void AddSpectatorStatistics(int spectatorID, INetworkSerializable spectatorArenaStatistics)
        {
            _spectatorIDs.Add(spectatorID);
            Write(spectatorArenaStatistics, SerializationModeFlags.VaryingDataFromServer);
        }

        /// <summary>
        /// Reads spectator arena statistics from the message.
        /// </summary>
        /// <param name="spectatorArenaStatisticsFinder">A method returning the arena statistics
        /// of a spectator for his identifier, or null if not found.</param>
        public void ReadSpectatorStatistics(Func<int, INetworkSerializable> spectatorArenaStatisticsFinder)
        {
            for (int i = 0; i < _spectatorIDs.Count; i++)
            {
                var statistics = spectatorArenaStatisticsFinder(_spectatorIDs[i]);
                if (statistics == null) return;
                Read(statistics, SerializationModeFlags.VaryingDataFromServer, 0);
            }
        }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                // Arena standings (request) message structure:
                // byte: spectator count, N
                // N * byte: spectator identifiers
                // ushort: data length, K
                // K bytes: spectator arena standings (N times) in the same order as spectator identifiers
                var writeBytes = StreamedData;
                writer.Write((byte)_spectatorIDs.Count);
                foreach (var id in _spectatorIDs) writer.Write((byte)id);
                writer.Write(((ushort)writeBytes.Length));
                writer.Write(writeBytes);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            var spectatorCount = reader.ReadByte();
            _spectatorIDs.Clear();
            _spectatorIDs.Capacity = spectatorCount;
            for (int i = 0; i < spectatorCount; i++) _spectatorIDs.Add(reader.ReadByte());
            var byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [" + _spectatorIDs.Count + " spectators]";
        }
    }
}
