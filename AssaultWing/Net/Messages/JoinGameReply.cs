using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client replying
    /// to a request to join the server. Contains information
    /// about the game the server is hosting.
    /// </summary>
    public class JoinGameReply : Message
    {
        /// <summary>
        /// Information about the players that want to join the game.
        /// </summary>
        public List<PlayerInfo> PlayerInfos { get; set; }

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x20, true);

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        protected override void SerializeBody()
        {
            // TODO !!!
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        protected override void Deserialize(byte[] body)
        {
            // TODO !!!
        }
    }
}
