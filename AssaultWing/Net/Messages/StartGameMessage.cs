using System;
using AW2.Helpers;
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
        public byte ArenaID { get; set; }
        public string ArenaToPlay { get; set; }
        public TimeSpan ArenaTimeLeft { get; set; }
        public int WallCount { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                // Start game (request) message structure:
                // byte: arena identifier
                // variable-length string: name of arena to play
                // TimeSpan: time left to play the arena, or zero if the arena doesn't time out
                // int: number of wall objects in the arena
                writer.Write((byte)ArenaID);
                writer.Write((string)ArenaToPlay);
                writer.Write((TimeSpan)ArenaTimeLeft);
                writer.Write((int)WallCount);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            ArenaID = reader.ReadByte();
            ArenaToPlay = reader.ReadString();
            ArenaTimeLeft = reader.ReadTimeSpan();
            WallCount = reader.ReadInt32();
        }

        public override string ToString()
        {
            return base.ToString() + " [" + ArenaToPlay + ", " + WallCount + " walls, "
                + ArenaTimeLeft.ToDurationString("d", "h", "min", "s", usePlurals: false) + " left]";
        }
    }
}
