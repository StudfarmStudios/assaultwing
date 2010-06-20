using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game client to the game server, sending identification information
    /// about the version of Assault Wing the client is running.
    /// </summary>
    public class JoinGameRequest : Message
    {
        protected static MessageType messageType = new MessageType(0x20, false);

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Join game request structure:
            // <empty>
            // TODO: Send CanonicalStrings to server for matching
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
