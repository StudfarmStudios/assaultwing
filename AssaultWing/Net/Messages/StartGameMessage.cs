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
        public int WallCount { get; set; }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                // Start game (request) message structure:
                // variable-length string: name of arena to play
                // int: number of wall objects in the arena
                writer.Write((string)ArenaToPlay);
                writer.Write((int)WallCount);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            ArenaToPlay = reader.ReadString();
            WallCount = reader.ReadInt32();
        }

        public override string ToString()
        {
            return base.ToString() + " [" + ArenaToPlay + ", " + WallCount + " walls]";
        }
    }
}
