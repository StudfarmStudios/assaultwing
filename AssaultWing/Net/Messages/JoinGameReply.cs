using System.Collections.Generic;
using System.Linq;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client replying
    /// to a request to join the server.
    /// </summary>
    public class JoinGameReply : Message
    {
        protected static MessageType messageType = new MessageType(0x20, true);

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Join game reply structure:
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
