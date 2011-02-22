﻿using System;

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

        /// <summary>
        /// Identifier given by the management server.
        /// </summary>
        public int ManagementID { get; set; }
    }
}