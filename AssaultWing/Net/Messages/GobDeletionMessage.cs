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
        public int GobID { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.SerializeBody(writer);
                // Player controls (request) message structure:
                // short: gob identifier
                writer.Write((short)GobID);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            GobID = reader.ReadInt16();
        }

        public override string ToString()
        {
            return base.ToString() + " [GobID " + GobID + "]";
        }
    }
}
