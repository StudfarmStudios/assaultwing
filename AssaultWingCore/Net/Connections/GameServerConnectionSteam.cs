using AW2.Core;
using AW2.Net.ConnectionUtils;
using Steamworks;
using AW2.Helpers;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A Steam network connection to a game server from game client.
    /// </summary>
    public class GameServerConnectionSteam : ConnectionSteam
    {

        private readonly AWEndPointSteam _endpoint;

        /// <summary>
        /// Creates a new connection to a game server.
        /// </summary>
        public GameServerConnectionSteam(AssaultWingCore game, HSteamNetConnection handle, SteamNetConnectionInfo_t info, AWEndPointSteam endpoint)
            : base(game, handle, info)
        {
            _endpoint = endpoint;
            Name = $"Game Server {_endpoint}";
        }

        protected override void DisposeImpl(bool error)
        {
            SteamNetworkingSockets.CloseConnection(Handle, 0, "Connection disposed by the client", true);
            if (error) Game.NetworkingErrors.Enqueue("Connection to the server lost.");
            base.DisposeImpl(error);
        }

        protected override int ReceiveMessages(IntPtr[] outMessages, int maxMessages)
        {
            return SteamNetworkingSockets.ReceiveMessagesOnConnection(Handle, outMessages, maxMessages);
        }

        protected override EResult SendMessage(IntPtr data, uint size, int flags, out long outMessageNumber)
        {
            return SteamNetworkingSockets.SendMessageToConnection(Handle, data, size, flags, out outMessageNumber);
        }

    }
}
