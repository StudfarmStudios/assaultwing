using System;
using System.Collections.Generic;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client, requesting the update of the settings of the game session.
    /// </summary>
    [MessageType(0x30, false)]
    public class GameSettingsRequest : Message
    {
        public string ArenaToPlay { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
            checked
            {
                // Game settings request structure:
                // variable-length string: name of arena to play
                writer.Write((string)ArenaToPlay);
            }
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
