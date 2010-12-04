using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game client to the game server, sending identification information
    /// about the version of Assault Wing the client is running.
    /// </summary>
    [MessageType(0x20, false)]
    public class JoinGameRequest : Message
    {
        /// <summary>
        /// The list of canonical strings on the game client.
        /// </summary>
        public IList<string> CanonicalStrings { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Join game request structure:
            // int: number of canonical strings, K
            // repeat K - 1 (all but the zero-indexed canonical string)
            //   length-prefixed string: string value
            writer.Write((int)CanonicalStrings.Count());
            foreach (var canonical in CanonicalStrings.Skip(1))
                writer.Write((string)canonical);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int canonicalStringCount = reader.ReadInt32();
            CanonicalStrings = new List<string>(canonicalStringCount);
            CanonicalStrings.Add(null);
            for (int i = 1; i < canonicalStringCount; ++i)
                CanonicalStrings.Add(reader.ReadString());
        }

        public override string ToString()
        {
            return base.ToString() + " [" + CanonicalStrings.Count() + " canonical strings]";
        }
    }
}
