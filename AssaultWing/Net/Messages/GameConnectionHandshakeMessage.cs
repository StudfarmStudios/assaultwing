using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A request from one game instance to another to initiate connection handshake.
    /// This message is handled automatically and internally by the receiving <see cref="Connection"/>.
    /// </summary>
    public class GameConnectionHandshakeMessage : Message
    {
        protected static MessageType messageType = new MessageType(0x31, false);

        /// <summary>
        /// Local UDP end point of the sender of the message.
        /// </summary>
        public IPEndPoint SenderLocalUDPEndPoint { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            checked
            {
                // Game connection handshake request structure:
                // variable length string: local IP address of the sender of the message
                // ushort: local UDP port of the sender of the message
                writer.Write((string)SenderLocalUDPEndPoint.Address.ToString());
                writer.Write((ushort)SenderLocalUDPEndPoint.Port);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            string address = reader.ReadString();
            ushort port = reader.ReadUInt16();
            var ipAddress = IPAddress.Parse(address);
            SenderLocalUDPEndPoint = new IPEndPoint(ipAddress, port);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
