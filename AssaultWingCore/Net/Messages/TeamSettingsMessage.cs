using System;
using System.Collections.Generic;
using AW2.Helpers.Serialization;
using AW2.Game;
using AW2.Game.Players;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game instance to another, requesting the update of the settings
    /// of a <see cref="AW2.Game.Players.Team"/>. This may implicitly request creating the team
    /// on the remote game instance.
    /// </summary>
    [MessageType(0x32, false)]
    public class TeamSettingsMessage : StreamMessage
    {
        private List<int> _ids = new List<int>();

        public IEnumerable<int> IDs { get { return _ids; } }

        public void Add(int id, INetworkSerializable item, SerializationModeFlags mode)
        {
            _ids.Add(id);
            Write(item, mode);
        }

        /// <summary>
        /// Reads the updates. If <paramref name="itemFinder"/> returns null,
        /// then that and the following updates in this message are skipped.
        /// </summary>
        public void Read(Func<int, INetworkSerializable> itemFinder, SerializationModeFlags serializationMode, int framesAgo)
        {
            for (int i = 0; i < _ids.Count; i++)
            {
                var item = itemFinder(_ids[i]);
                if (item == null) break;
                Read(item, serializationMode, framesAgo);
            }
        }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                // Team settings (request) message structure:
                // byte: number of teams to update, K
                // ushort: total byte count of team data
                // K bytes: identifiers of the teams
                // ushort: data length N
                // repeat K times:
                //   ??? bytes: serialised data of a team (content known only by the class in question)
                byte[] writeBytes = StreamedData;
#if NETWORK_PROFILING
                using (new NetworkProfilingScope("TeamSettingsMessageHeader"))
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
            return base.ToString() + " [" + _ids.Count + " teams]";
        }
    }
}
