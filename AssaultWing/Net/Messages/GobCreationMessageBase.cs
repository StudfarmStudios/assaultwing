using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the creation of a gob.
    /// </summary>
    public abstract class GobCreationMessageBase : GameplayMessage
    {
        /// <summary>
        /// Type name of the gob to create.
        /// </summary>
        public CanonicalString GobTypeName { get; set; }

        /// <summary>
        /// Index of the arena layer the gob lives in.
        /// </summary>
        public int LayerIndex { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Gob creation (request) message structure:
            // byte: arena layer index
            // int: canonical form of gob type name
            // int: data length N
            // N bytes: serialised data of the gob (content known only by the Gob subclass in question)
            byte[] writeBytes = StreamedData;
            writer.Write(checked((byte)LayerIndex));
            writer.Write((int)GobTypeName.Canonical);
            writer.Write((int)writeBytes.Length);
            writer.Write(writeBytes, 0, writeBytes.Length);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            LayerIndex = reader.ReadByte();
            GobTypeName = (CanonicalString)reader.ReadInt32();
            int byteCount = reader.ReadInt32();
            StreamedData = reader.ReadBytes(byteCount);
        }

        public override string ToString()
        {
            return base.ToString() + " [GobTypeName " + GobTypeName + "]";
        }
    }
}
