using System;
using System.Collections.Generic;
using AW2.Helpers;
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
        public CanonicalString GameplayMode { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                // Game settings request structure:
                // variable-length string: name of arena to play
                // canonical string: name of gameplay mode
                writer.Write((string)ArenaToPlay);
                writer.Write((CanonicalString)GameplayMode);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            ArenaToPlay = reader.ReadString();
            GameplayMode = reader.ReadCanonicalString();
        }

        public override string ToString()
        {
            return base.ToString() + " [" + ArenaToPlay + ", " + GameplayMode + "]";
        }
    }
}
