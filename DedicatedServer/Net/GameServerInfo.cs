using System;

namespace AW2.Net
{
    /// <summary>
    /// General information about an Assault Wing game server.
    /// </summary>
    public class GameServerInfo
    {
        public string Name { get; set; }
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }
        public int WaitingPlayers { get; set; }

        /// <summary>
        /// Identifier given by the management server.
        /// </summary>
        public string ManagementID { get; set; }

        public Version AWVersion { get; set; }
    }
}
