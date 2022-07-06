using System.Net.Sockets;
using AW2.Core;
using AW2.Net.ConnectionUtils;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A Steam network connection to a game client from a server.
    /// </summary>
    public class GameClientConnectionSteam : ConnectionSteam, GameClientConnection
    {
        public GameClientStatus ConnectionStatus { get; set; }

        /// <summary>
        /// Creates a new connection to a game client.
        /// </summary>
        public GameClientConnectionSteam(AssaultWingCore game)
            : base(game)
        {
            Name = string.Format("Game Client {0}");
            ConnectionStatus = new GameClientStatus();
        }
    }
}
