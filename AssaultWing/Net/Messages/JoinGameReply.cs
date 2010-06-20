using System.Collections.Generic;
using System.Linq;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client replying
    /// to a request to join the server.
    /// </summary>
    public class JoinGameReply : Message
    {
        protected static MessageType messageType = new MessageType(0x20, true);

        /// <summary>
        /// The list of canonical strings on the game server.
        /// </summary>
        public IList<string> CanonicalStrings { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Join game reply message structure:
            // int: number of canonical strings, K
            // repeat K - 1 (all but the zero-indexed canonical string)
            //   32 byte string: string value
            writer.Write((int)CanonicalStrings.Count);
            foreach (var canonical in CanonicalStrings.Skip(1))
                writer.Write((string)canonical, 32, true);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int canonicalStringCount = reader.ReadInt32();
            CanonicalStrings = new string[canonicalStringCount];
            CanonicalStrings[0] = null;
            for (int i = 1; i < canonicalStringCount; ++i)
                CanonicalStrings[i] = reader.ReadString(32);
        }

        public override string ToString()
        {
            return base.ToString() + " [" + CanonicalStrings.Count + " CanonicalStrings]";
        }
    }
}
