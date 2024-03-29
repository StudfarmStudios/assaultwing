namespace AW2.Net.ConnectionUtils
{
    [System.Diagnostics.DebuggerDisplay("{State}, Arena={CurrentArenaName}, Running={IsRunningArena}")]
    public class GameClientStatus
    {
        public enum StateType { Initializing, Active, Dropped };

        private string _currentArenaName;

        /// <summary>
        /// Name of the arena the instance at the end of the connection is currently running,
        /// or the empty string if no arena is being run.
        /// This property is maintained by the game server.
        /// </summary>
        public string CurrentArenaName { get { return _currentArenaName ?? ""; } set { _currentArenaName = value; } }
        public bool IsRunningArena { get { return CurrentArenaName != ""; } }

        /// <summary>
        /// If null, the client is not requesting spawn. Otherwise the value is the <see cref="Arena.ID"/>
        /// of the arena into which the client is requesting to be spawned. If the client already has a ship,
        /// the value of this flag has no effect.
        /// </summary>
        public byte? IsRequestingSpawnForArenaID { get; set; }

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
        public StateType State { get; set; }
    }
}
