using System;
using System.Collections.Generic;
using AW2.Game.Players;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating the state of a spectator or a team.
    /// </summary>
    [MessageType(0x28, false)]
    public class SpectatorOrTeamUpdateMessage : StreamMessage
    {
        private List<int> _ids = new List<int>();

        public void Add(int id, INetworkSerializable spectatorOrTeam, SerializationModeFlags serializationMode)
        {
            _ids.Add(id);
            Write(spectatorOrTeam, serializationMode);
        }

        /// <summary>
        /// Reads the updates for the spectators and teams. If <paramref name="spectatorOrTeamFinder"/> returns null,
        /// then that and the following updates in this message are skipped.
        /// </summary>
        public void Read(Func<int, INetworkSerializable> spectatorOrTeamFinder, SerializationModeFlags serializationMode, int framesAgo)
        {
            for (int i = 0; i < _ids.Count; i++)
            {
                var spectatorOrTeam = spectatorOrTeamFinder(_ids[i]);
                if (spectatorOrTeam == null) break;
                Read(spectatorOrTeam, serializationMode, framesAgo);
            }
        }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(null))
#endif
            checked
            {
                // Spectator or team update (request) message structure:
                // byte: number of spectators and teams to update, K
                // ushort: total byte count of spectator and team data
                // K bytes: identifiers of the spectators and teams
                // ushort: data length N
                // repeat K times:
                //   ??? bytes: serialised data of a spectator or a team (content known only by the class in question)
                byte[] writeBytes = StreamedData;
#if NETWORK_PROFILING
                using (new NetworkProfilingScope("SpectatorOrTeamUpdateMessageHeader"))
#endif
                {
                    writer.Write((byte)_ids.Count);
                    writer.Write((ushort)writeBytes.Length);
                    foreach (var id in _ids) writer.Write((byte)id);
                }
                writer.Write(writeBytes, 0, writeBytes.Length);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int count = reader.ReadByte();
            int totalByteCount = reader.ReadUInt16();
            _ids.Clear();
            for (int i = 0; i < count; i++) _ids.Add(reader.ReadByte());
            StreamedData = reader.ReadBytes(totalByteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [" + _ids.Count + " spectators and teams]";
        }
    }
}
