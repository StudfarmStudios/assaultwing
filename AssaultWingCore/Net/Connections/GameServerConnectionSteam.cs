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

        /// <summary>
        /// Creates a new connection to a game server.
        /// </summary>
        public GameServerConnectionSteam(AssaultWingCore game, HSteamNetConnection handle, SteamNetConnectionInfo_t info)
            : base(game, handle, info)
        {
            Name = $"Game Server {Steam.IdentityToString(info.m_identityRemote)}";
        }

        protected override void DisposeImpl(bool error)
        {
            if (error) Game.NetworkingErrors.Enqueue("Connection to server lost.");
            base.DisposeImpl(error);
        }
    }
}
