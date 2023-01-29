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
    public class GameServerHandshakeRequestTCP : Message
    {
        /// <summary>
        /// The list of canonical strings on the game client.
        /// </summary>
        public IList<string> CanonicalStrings { get; set; }

        /// <summary>
        /// The identifier that will identify the game client in a later
        /// <see cref="GameServerHandshakeRequestUDP"/> message.
        /// </summary>
        public byte[] GameClientKey { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                // Game server handshake (TCP) request structure:
                // int: number of canonical strings, K
                // repeat K - 1 (all but the zero-indexed canonical string)
                //   length-prefixed string: string value
                // short: number of bytes, K
                // K bytes: game client key
                writer.Write((int)CanonicalStrings.Count());
                foreach (var canonical in CanonicalStrings.Skip(1))
                    writer.Write((string)canonical);
                writer.Write((short)GameClientKey.Length);
                writer.Write(GameClientKey);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int canonicalStringCount = reader.ReadInt32();
            CanonicalStrings = new List<string>(canonicalStringCount);
            CanonicalStrings.Add(null);
            for (int i = 1; i < canonicalStringCount; ++i)
                CanonicalStrings.Add(reader.ReadString());
            int keyLength = reader.ReadInt16();
            GameClientKey = reader.ReadBytes(keyLength);
        }

        public override string ToString()
        {
            return base.ToString() + " [" + CanonicalStrings.Count() + " canonical strings, "
                + " key = " + AW2.Helpers.MiscHelper.BytesToString(new ArraySegment<byte>(GameClientKey)) + "]";
        }
    }
}
