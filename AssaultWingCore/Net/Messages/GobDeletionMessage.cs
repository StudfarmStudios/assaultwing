using System.Collections.Generic;
using System.Linq;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the deletion one or more gobs.
    /// </summary>
    [MessageType(0x25, false)]
    public class GobDeletionMessage : GameplayMessage
    {
        /// <summary>
        /// Identifiers of the gobs to delete.
        /// </summary>
        public List<int> GobIDs { get; set; }

        public GobDeletionMessage()
        {
            GobIDs = new List<int>();
        }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.SerializeBody(writer);
                // Gob deletion (request) message structure:
                // byte: gob count, N
                // N * short: gob identifiers
                writer.Write((byte)GobIDs.Count);
                foreach (var gobID in GobIDs) writer.Write((short)gobID);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            var idCount = reader.ReadByte();
            GobIDs.Clear();
            GobIDs.Capacity = idCount;
            for (int i = 0; i < idCount; i++) GobIDs.Add(reader.ReadInt16());
        }

        public override string ToString()
        {
            return base.ToString() + " [GobIDs: " + string.Join(", ", GobIDs.Select(id => id.ToString()).ToArray()) + "]";
        }
    }
}
