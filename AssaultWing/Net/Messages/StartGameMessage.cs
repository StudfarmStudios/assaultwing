using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// that the game session starts.
    /// </summary>
    [MessageType(0x21, false)]
    public class StartGameMessage : Message
    {
        public string ArenaToPlay { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
            // Start game (request) message structure:
            // variable-length string: name of arena to play
            writer.Write((string)ArenaToPlay);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            ArenaToPlay = reader.ReadString();
        }

        public override string ToString()
        {
            return base.ToString() + " [" + ArenaToPlay + "]";
        }
    }
}
