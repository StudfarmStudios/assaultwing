using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating
    /// the state of a player.
    /// </summary>
    [MessageType(0x28, false)]
    public class PlayerUpdateMessage : GameplayMessage
    {
        /// <summary>
        /// Identifier of the player to update.
        /// </summary>
        public int PlayerID { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(null))
#endif
            {
                base.SerializeBody(writer);
                // Player update (request) message structure:
                // int: player identifier
                // word: data length N
                // N bytes: serialised data of the player
                byte[] writeBytes = StreamedData;
#if NETWORK_PROFILING
                using (new NetworkProfilingScope("PlayerUpdateMessageHeader"))
#endif
                {
                    writer.Write((byte)PlayerID);
                    writer.Write(checked((ushort)writeBytes.Length));
                }
                writer.Write(writeBytes, 0, writeBytes.Length);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            PlayerID = reader.ReadByte();
            int byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [PlayerID " + PlayerID + "]";
        }
    }
}
