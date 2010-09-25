using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating
    /// the state of a player.
    /// </summary>
    public class PlayerUpdateMessage : GameplayMessage
    {
        protected static MessageType messageType = new MessageType(0x28, false);

        /// <summary>
        /// Identifier of the player to update.
        /// </summary>
        public int PlayerID { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Player update (request) message structure:
            // int: player identifier
            // word: data length N
            // N bytes: serialised data of the player
            byte[] writeBytes = StreamedData;
            writer.Write((byte)PlayerID);
            writer.Write(checked((ushort)writeBytes.Length));
            writer.Write(writeBytes, 0, writeBytes.Length);
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
