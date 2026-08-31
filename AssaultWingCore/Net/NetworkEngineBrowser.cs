#if BROWSER
using AW2.Core;
using AW2.Net.Connections;
using AW2.Net.Messages;
using Steamworks;

namespace AW2.Net
{
    /// <summary>
    /// Standalone-only network service for the browser target. Browsers do not
    /// expose the TCP/UDP socket APIs used by AW2, and Steamworks is unavailable.
    /// Local games do not require a live connection.
    /// </summary>
    public sealed class NetworkEngineBrowser : NetworkEngine
    {
        private static readonly GameClientConnection[] NoClients = Array.Empty<GameClientConnection>();
        private static readonly ConnectionBase[] NoConnections = Array.Empty<ConnectionBase>();

        public NetworkEngineBrowser(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        public override IEnumerable<GameClientConnection> GameClientConnections => NoClients;
        public override Connection GameServerConnection => null;
        protected override IEnumerable<ConnectionBase> AllConnections => NoConnections;

        public override void StartServer(Func<bool> allowNewConnection) =>
            throw new PlatformNotSupportedException("Network games are disabled in the browser build.");

        public override void StopServer() { }

        public override void StartClient(AssaultWingCore game, AWEndPoint[] serverEndPoints,
            Action<IResult<Connection>> connectionHandler) =>
            throw new PlatformNotSupportedException("Network games are disabled in the browser build.");

        public override void StopClient() { }
        public override bool IsRemovedClientConnection(GameClientConnection connection) => false;
        public override void AddRemovedClientConnection(GameClientConnection connection) { }
        public override void DoClientUdpHandshake(GameServerHandshakeRequestTCP message) { }

        public override GameClientConnection GetGameClientConnection(int connectionID) =>
            throw new InvalidOperationException("The browser build has no network connections.");

        public override Connection GetConnection(int connectionID) =>
            throw new InvalidOperationException("The browser build has no network connections.");

        public override string GetConnectionAddressString(int connectionID) => string.Empty;
        public override CSteamID? GetSteamId(int connectionID) => null;
        public override byte[] GetAssaultWingInstanceKey() => Array.Empty<byte>();
        public override string GetPilotId(int connectionID) => string.Empty;
    }
}
#endif
