using System;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client, informing that the connection
    /// is to close.
    /// </summary>
    public class ConnectionClosingMessage : Message
    {
        protected static MessageType messageType = new MessageType(0x2f, false);

        /// <summary>
        /// Human-readable information about why the connection is closed.
        /// </summary>
        public string Info { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Connection closing (request) message structure:
            // variable length string: info about the closing of the connection
            writer.Write((string)Info);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            Info = reader.ReadString();
        }

        public override string ToString()
        {
            return base.ToString() + " [" + Info + "]";
        }
    }
}
