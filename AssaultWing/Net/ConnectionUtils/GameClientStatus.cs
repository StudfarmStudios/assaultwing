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
        /// Is the game client playing the current arena or hanging out in menus.
        /// This property is maintained by the game client.
        /// </summary>
        public bool IsPlayingArena { get; set; }

        public bool IsDropped { get; set; }
    }
}
