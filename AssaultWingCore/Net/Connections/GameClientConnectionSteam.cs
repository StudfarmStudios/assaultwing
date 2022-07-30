using AW2.Core;
using AW2.Net.ConnectionUtils;
using Steamworks;

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
        public GameClientConnectionSteam(AssaultWingCore game, HSteamNetConnection handle, SteamNetConnectionInfo_t info)
            : base(game, handle, info)
        {
            Name = $"Game Client {ID}";
            ConnectionStatus = new GameClientStatus();
        }

        protected override void DisposeImpl(bool error)
        {
            Game.NetworkEngine.DropClient(this);
            SteamGameServerNetworkingSockets.CloseConnection(Handle, 0, "Connection disposed by the server", true);
            base.DisposeImpl(error);
        }

        protected override int ReceiveMessages(IntPtr[] outMessages, int maxMessages)
        {
            return SteamGameServerNetworkingSockets.ReceiveMessagesOnConnection(Handle, outMessages, maxMessages);
        }

        protected override EResult SendMessage(IntPtr data, uint size, int flags, out long outMessageNumber)
        {
            return SteamGameServerNetworkingSockets.SendMessageToConnection(Handle, data, size, flags, out outMessageNumber);
        }
    }
}
