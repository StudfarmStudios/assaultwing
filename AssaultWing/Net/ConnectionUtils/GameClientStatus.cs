namespace AW2.Net.ConnectionUtils
{
    [System.Diagnostics.DebuggerDisplay("Arena {CurrentArenaName}, Playing {IsPlayingArena}")]
    public class GameClientStatus
    {
        private string _currentArenaName;

        /// <summary>
        /// Name of the arena the instance at the end of the connection is currently running,
        /// or the empty string if no arena is being run.
        /// This property is maintained by the game server.
        /// </summary>
        public string CurrentArenaName { get { return _currentArenaName ?? ""; } set { _currentArenaName = value; } }
        public bool IsRunningArena { get { return CurrentArenaName != ""; } }

        /// <summary>
        /// If true, the game client requests to spawn his ship. If the client already has a ship,
        /// the value of this flag has no effect.
        /// </summary>
        public bool IsRequestingSpawn { get; set; }

        /// <summary>
        /// Is the game client ready to start the next arena.
        /// This property is maintained by the game client.
        /// </summary>
        public bool IsReadyToStartArena { get; set; }

        /// <summary>
        /// A unique identifier of the client, provided by the client.
        /// </summary>
        public byte[] ClientKey { get; set; }

        public bool HasPlayerSettings { get; set; }
        public bool IsDropped { get; set; }
    }
}
