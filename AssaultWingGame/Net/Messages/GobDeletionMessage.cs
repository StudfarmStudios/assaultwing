using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the deletion of a gob.
    /// </summary>
    [MessageType(0x25, false)]
    public class GobDeletionMessage : GameplayMessage
    {
        /// <summary>
        /// Identifier of the gob to delete.
        /// </summary>
        public int GobId { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
            base.SerializeBody(writer);
            // Player controls (request) message structure:
            // int: gob identifier
            writer.Write((int)GobId);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            GobId = reader.ReadInt32();
        }

        public override string ToString()
        {
            return base.ToString() + " [GobId " + GobId + "]";
        }
    }
}
