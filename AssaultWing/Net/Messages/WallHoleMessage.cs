using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client about
    /// making a hole in an arena wall.
    /// </summary>
    public class WallHoleMessage : GameplayMessage
    {
        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x2c, false);
    }
}
