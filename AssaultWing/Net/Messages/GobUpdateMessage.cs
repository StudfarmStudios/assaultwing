namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client updating
    /// the state of a gob.
    /// </summary>
    public class GobUpdateMessage : GameplayMessage
    {
        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x24, false);

        /// <summary>
        /// Identifier of the gob to update.
        /// </summary>
        public int GobId { get; set; }

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Gob update (request) message structure:
            // int: gob identifier
            // word: data length N
            // N bytes: serialised data of the gob (content known only by the Gob subclass in question)
            byte[] writeBytes = StreamedData;
            writer.Write((int)GobId);
            writer.Write(checked((ushort)writeBytes.Length));
            writer.Write(writeBytes, 0, writeBytes.Length);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            GobId = reader.ReadInt32();
            int byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
        }

        /// <summary>
        /// Returns a String that represents the current Object. 
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + " [GobId " + GobId + "]";
        }
    }
}
