using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game client to a game server, identifying the client
    /// on the server and establishing a UDP connection.
    /// </summary>
    [MessageType(0x26, false)]
    public class GameServerHandshakeRequestUDP : Message
    {
        public override MessageSendType SendType { get { return MessageSendType.UDP; } }

        /// <summary>
        /// The identifier the game client provided via TCP in an earlier
        /// <see cref="GameServerHandshakeRequestTCP"/> message.
        /// </summary>
        public byte[] GameClientKey { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
            checked
            {
                // Game server handshake (UDP) request structure:
                // short: number of bytes, K
                // K bytes: game client key
                writer.Write((short)GameClientKey.Length);
                writer.Write(GameClientKey);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            int keyLength = reader.ReadInt16();
            GameClientKey = reader.ReadBytes(keyLength);
        }

        public override string ToString()
        {
            return base.ToString() + " [key = " + AW2.Helpers.MiscHelper.BytesToString(new ArraySegment<byte>(GameClientKey)) + "]";
        }
    }
}
