using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating the state of a spectator.
    /// </summary>
    [MessageType(0x28, false)]
    public class SpectatorUpdateMessage : GameplayMessage
    {
        /// <summary>
        /// Identifier of the spectator to update.
        /// </summary>
        public int SpectatorID { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(null))
#endif
            {
                base.SerializeBody(writer);
                // Spectator update (request) message structure:
                // byte: spectator identifier
                // ushort: data length N
                // N bytes: serialised data of the spectator
                byte[] writeBytes = StreamedData;
#if NETWORK_PROFILING
                using (new NetworkProfilingScope("SpectatorUpdateMessageHeader"))
#endif
                {
                    writer.Write((byte)SpectatorID);
                    writer.Write(checked((ushort)writeBytes.Length));
                }
                writer.Write(writeBytes, 0, writeBytes.Length);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            SpectatorID = reader.ReadByte();
            int byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [SpectatorID " + SpectatorID + "]";
        }
    }
}
