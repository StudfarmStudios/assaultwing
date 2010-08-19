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

        public override MessageSendType SendType { get { return MessageSendType.UDP; } }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Game connection handshake request structure:
            // <empty>
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
