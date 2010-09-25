using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the creation of a gob.
    /// </summary>
    public class GobCreationMessage : GameplayMessage
    {
        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x23, false);

        /// <summary>
        /// Type name of the gob to create.
        /// </summary>
        public CanonicalString GobTypeName { get; set; }

        /// <summary>
        /// Index of the arena layer the gob lives in.
        /// </summary>
        public int LayerIndex { get; set; }

        /// <summary>
        /// Is the gob created to the next arena in the arena playlist,
        /// as opposed to the currently active arena.
        /// </summary>
        public bool CreateToNextArena { get; set; }

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Gob creation (request) message structure:
            // bool as 1 byte: true=create gob to next arena, false=create gob to current arena
            // byte: arena layer index
            // int: canonical form of gob type name
            // int: data length N
            // N bytes: serialised data of the gob (content known only by the Gob subclass in question)
            byte[] writeBytes = StreamedData;
            writer.Write((bool)CreateToNextArena);
            writer.Write(checked((byte)LayerIndex));
            writer.Write((int)GobTypeName.Canonical);
            writer.Write((int)writeBytes.Length);
            writer.Write(writeBytes, 0, writeBytes.Length);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            CreateToNextArena = reader.ReadBoolean();
            LayerIndex = reader.ReadByte();
            GobTypeName = (CanonicalString)reader.ReadInt32();
            int byteCount = reader.ReadInt32();
            StreamedData = reader.ReadBytes(byteCount);
        }

        /// <summary>
        /// Returns a String that represents the current Object. 
        /// </summary>
        public override string ToString()
        {
            return base.ToString() + " [GobTypeName " + GobTypeName + "]";
        }
    }
}
