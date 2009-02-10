namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the creation of a gob.
    /// </summary>
    /// To initialise a message for sending, call <c>BeginWrite</c> and
    /// serialise the gob's state with appropriate calls to the various
    /// write methods of the returned writer. Then call <c>EndWrite</c>
    /// and send the message.
    /// 
    /// To get the serialised data from a message, call <c>BeginRead</c>
    /// and deserialise the gob's state with appropriate calls to the various
    /// read methods of the returned reader. Then call <c>EndRead</c>.
    public class GobCreationMessage : GameplayMessage
    {
        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x23, false);

        /// <summary>
        /// Type name of the gob to create.
        /// </summary>
        public string GobTypeName { get; set; }

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Player controls (request) message structure:
            // 32 bytes string: gob type name
            // word: data length N
            // N bytes: serialised data of the gob (content known only by the Gob subclass in question)
            byte[] writeBytes = StreamedData;
            writer.Write((string)GobTypeName, 32, true);
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
            GobTypeName = reader.ReadString(32);
            int byteCount = reader.ReadUInt16();
            StreamedData = reader.ReadBytes(byteCount);
        }
    }
}
