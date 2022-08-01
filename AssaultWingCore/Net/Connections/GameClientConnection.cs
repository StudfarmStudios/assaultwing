using AW2.Net.ConnectionUtils;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A network connection to a game client.
    /// </summary>
    public interface GameClientConnection : Connection
    {
        public GameClientStatus ConnectionStatus { get; set; }
    }
}