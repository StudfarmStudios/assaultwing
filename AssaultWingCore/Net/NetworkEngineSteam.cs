using AW2.Core;
using AW2.Helpers;
using AW2.Net.Connections;
using AW2.Net.Messages;
using Steamworks;
namespace AW2.Net
{
    /// <summary>
    /// Network engine. Takes care of communications between several
    /// Assault Wing instances over the Internet.
    /// </summary>
    /// <para>
    /// A game server can communicate with its game clients by sending
    /// multicast messages via <c>SendToClients</c> and receiving
    /// messages via <c>ReceiveFromClients</c>. Messages can be
    /// received by type, so each part of the game logic can poll for 
    /// messages that are relevant to it without interfering with
    /// other parts of the game logic. Each received message
    /// contains an identifier of the connection to the client who
    /// sent that message.
    /// </para><para>
    /// A game client can communicate with its game server by sending
    /// messages via <c>SendToServer</c> and receiving messages via
    /// <c>ReceiveFromServer</c>.
    /// </para><para>
    /// <see cref="NetworkEngine"/> reacts to incoming messages according
    /// to message handlers that other components register.
    /// </para>
    /// <seealso cref="Message.ConnectionID"/>
    public class NetworkEngineSteam : NetworkEngine
    {
        private SecureId secureId = new SecureId();
        private List<GameClientConnectionSteam> _GameClientConnections = new List<GameClientConnectionSteam>();
        private Func<bool> AllowNewServerConnection;

        /// <summary>
        /// Clients to be removed from <c>clientConnections</c>.
        /// </summary>
        private List<GameClientConnectionSteam> _removedClientConnections = new List<GameClientConnectionSteam>();

        /// <summary>
        /// Unlike the old code, don't encode any identifiable information. Just a random GUID.
        /// </summary>
        private readonly Guid InstanceKey = Guid.NewGuid();

        public NetworkEngineSteam(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        public override IEnumerable<GameClientConnection> GameClientConnections { get { return _GameClientConnections; } }

        private GameServerConnectionSteam? _GameServerConnection;

        public override Connection GameServerConnection { get { return _GameServerConnection; } }

        private readonly List<HSteamListenSocket> ListenSockets = new List<HSteamListenSocket>();

        private readonly List<(AWEndPointSteam, HSteamNetConnection)> GameServerConnectionAttempts = new List<(AWEndPointSteam, HSteamNetConnection)>();

        override protected IEnumerable<ConnectionSteam> AllConnections
        {
            get
            {
                if (IsConnectedToGameServer)
                    yield return _GameServerConnection;
                if (_GameClientConnections != null)
                    foreach (var conn in _GameClientConnections) yield return conn;
            }
        }

        public override void DoClientUdpHandshake(GameServerHandshakeRequestTCP mess)
        {
            // Here in the original network engine the servers sends UDP packets towards the
            // probable ports where the client upstream connection might be coming from to
            // increase chances if the NAT busting / UPnP working. I don't think we need
            // it with Steam networking.
        }

        public override bool IsRemovedClientConnection(GameClientConnection connection)
        {
            return _removedClientConnections.Contains(connection);
        }

        public override void AddRemovedClientConnection(GameClientConnection connection)
        {
            _removedClientConnections.Add((GameClientConnectionSteam)connection);
        }

        public override byte[] GetAssaultWingInstanceKey()
        {
            return InstanceKey.ToByteArray();
        }

        public override ConnectionSteam GetConnection(int connectionID)
        {
            var result = AllConnections.First(conn => conn.ID == connectionID);
            if (result == null) throw new ArgumentException("Connection not found with ID " + connectionID);
            return result;
        }

        public override string GetConnectionAddressString(int connectionID)
        {
            var result = AllConnections.First(conn => conn.ID == connectionID);
            if (result == null) return $"Unknown connection {connectionID}";
            return Steam.IdentityToAddrPreferred(result.Info);
        }

        override public GameClientConnectionSteam GetGameClientConnection(int connectionID)
        {
            return _GameClientConnections.First(conn => conn.ID == connectionID);
        }

#if ALLOW_MULTIPLE_CLIENTS_PER_HOST
        private Dictionary<int, CSteamID> ConnectionIdToAssignedSteamId = new Dictionary<int, CSteamID>();
        private uint FakeSteamIdCounter = 1;
#endif

        override public CSteamID? GetSteamId(int connectionId)
        {
            var steamId = AllConnections.FirstOrDefault(conn => conn.ID == connectionId)?.SteamId;

#if ALLOW_MULTIPLE_CLIENTS_PER_HOST
            if (steamId is not null)
            {
                if (ConnectionIdToAssignedSteamId.ContainsKey(connectionId))
                {
                    return ConnectionIdToAssignedSteamId[connectionId];
                }
                else
                {
                    var originalConnection = ConnectionIdToAssignedSteamId
                        .Where(kvp => kvp.Value == steamId).ToList();

                    if (originalConnection.Count == 0)
                    {
                        // Create entry for the first connection with this CSteamID.
                        ConnectionIdToAssignedSteamId[connectionId] = steamId.Value;
                    }
                    else
                    {
                        // Assign a fake CSteamID to produce a different PilotId
                        // for this connection. to allow testing Steam features
                        // by connecting multiple clients using the same steam
                        // profile.
                        steamId = new CSteamID(new AccountID_t(FakeSteamIdCounter++), EUniverse.k_EUniverseInvalid, EAccountType.k_EAccountTypeInvalid);
                        Log.Write($"Using fake steamId {steamId} for connection {connectionId} to allow testing.");
                        ConnectionIdToAssignedSteamId[connectionId] = steamId.Value;
                    }
                }
            }
#endif

            return steamId;
        }

        public GameClientConnectionSteam? GetGameClientConnectionByHandle(HSteamNetConnection handle)
        {
            return _GameClientConnections.FirstOrDefault(conn => conn.Handle == handle);
        }

        private SteamApiService SteamApiService => Game.Services.GetService<SteamApiService>();

        private class ClientCallbacks : SteamApiService.CallbackBundleKey { };

        public override void StartClient(AssaultWingCore game, AWEndPoint[] serverEndPoints, Action<IResult<Connection>> connectionHandler)
        {
            StartClientConnectionHandler = connectionHandler;

            SteamApiService.Callback<ClientCallbacks, SteamNetConnectionStatusChangedCallback_t>(
                LogCallbackError<SteamNetConnectionStatusChangedCallback_t>(OnClientConnectionStatusChanged));

            var steamEndPoints = serverEndPoints.OfType<AWEndPointSteam>().ToArray() ?? Array.Empty<AWEndPointSteam>();
            var endPointsString = string.Join(", ", serverEndPoints.Select(e => e.ToString()));
            if (steamEndPoints.Length != serverEndPoints.Length)
            {
                throw new ArgumentException("NetworkEngineSteam can only handle end points of the format ip:host:port and other steam network identity formats.\n" +
                    $"Some of these are not compatible '{endPointsString}'");
            }
            Log.Write($"Client starts connecting to the following end points: {endPointsString}");

            foreach (var endpoint in steamEndPoints)
            {
                HSteamNetConnection connection;
                if (endpoint.UseDirectIp)
                {
                    // Don't hide the client IP address from the server by using the steam relay network.
                    connection = SteamNetworkingSockets.ConnectByIPAddress(ref endpoint.DirectIp, 0, new SteamNetworkingConfigValue_t[] { });
                }
                else
                {
                    connection = SteamNetworkingSockets.ConnectP2P(ref endpoint.SteamNetworkingIdentity, 0, 0, new SteamNetworkingConfigValue_t[] { });
                }

                GameServerConnectionAttempts.Add((endpoint, connection));
            }
        }

        private class ServerCallbacks : SteamApiService.CallbackBundleKey { };

        public override void StartServer(Func<bool> allowNewConnection)
        {
            SteamApiService.ServerCallback<ServerCallbacks, SteamNetConnectionStatusChangedCallback_t>(
                LogCallbackError<SteamNetConnectionStatusChangedCallback_t>(OnServerConnectionStatusChanged));
            var port = (ushort)Game.Settings.Net.GameServerPort;
            Log.Write($"Starting game server on port {port} and Steam P2P");
            AllowNewServerConnection = allowNewConnection;
            SteamNetworkingIPAddr portOnlyIpv4Addr = new SteamNetworkingIPAddr();
            portOnlyIpv4Addr.Clear();
            portOnlyIpv4Addr.SetIPv4(0, port);
            SteamNetworkingIPAddr portOnlyIpv6Addr = new SteamNetworkingIPAddr();
            portOnlyIpv6Addr.Clear();
            portOnlyIpv6Addr.SetIPv6(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, port);

            // ListenSockets.Add(SteamNetworkingSockets.CreateListenSocketIP(ref portOnlyIpv4Addr, 0, new SteamNetworkingConfigValue_t[]{}));
            // ListenSockets.Add(SteamNetworkingSockets.CreateListenSocketIP(ref portOnlyIpv6Addr, 0, new SteamNetworkingConfigValue_t[]{}));
            // ListenSockets.Add(SteamNetworkingSockets.CreateListenSocketP2P(0, 0, new SteamNetworkingConfigValue_t[]{}));

            ListenSockets.Add(SteamGameServerNetworkingSockets.CreateListenSocketIP(ref portOnlyIpv4Addr, 0, new SteamNetworkingConfigValue_t[] { }));
            ListenSockets.Add(SteamGameServerNetworkingSockets.CreateListenSocketIP(ref portOnlyIpv6Addr, 0, new SteamNetworkingConfigValue_t[] { }));
            ListenSockets.Add(SteamGameServerNetworkingSockets.CreateListenSocketP2P(0, 0, new SteamNetworkingConfigValue_t[] { }));
        }

        private Callback<T>.DispatchDelegate LogCallbackError<T>(Callback<T>.DispatchDelegate action)
        {
            return (T t) =>
            {
                try
                {
                    action(t);
                }
                catch (Exception e)
                {
                    Log.Write($"Error processing {typeof(T)}", e);
                }
            };
        }

        private void HandleGameServerConnectionStatus(GameServerConnectionSteam connection, SteamNetConnectionStatusChangedCallback_t status)
        {
            connection.Info = status.m_info;

            switch (status.m_info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                    Log.Write($"Connection '{connection.Name}' lost. state: {SteamApiService.NetStateToString(status.m_info.m_eState)}: {status.m_info.m_eEndReason} / \"{status.m_info.m_szEndDebug}\"");
                    SteamNetworkingSockets.CloseConnection(status.m_hConn, 0, "Server connection lost", true);
                    _GameServerConnection?.QueueError("Server connection lost");
                    break;
            }

        }

        private void OnClientConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t status)
        {

            if (_GameServerConnection != null && _GameServerConnection.Handle.m_HSteamNetConnection == status.m_hConn.m_HSteamNetConnection)
            {
                HandleGameServerConnectionStatus(_GameServerConnection, status);
                return;
            }

            var thisAttempt = GameServerConnectionAttempts.Find(t => t.Item2.m_HSteamNetConnection == status.m_hConn.m_HSteamNetConnection);
            var otherAttempts = GameServerConnectionAttempts.Where(t => t.Item2.m_HSteamNetConnection != status.m_hConn.m_HSteamNetConnection).ToArray();

            if (thisAttempt.Item1 == null)
            {
                return; // Ignore statuses for connection attemps that we have already closed and forgotten about.
            }

            var endpoint = thisAttempt.Item1;
            var handler = StartClientConnectionHandler;

            switch (status.m_info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    if (handler != null)
                    {
                        foreach (var otherAttempt in otherAttempts)
                        {
                            // Close other parallel connections immediately to avoid confusion
                            SteamNetworkingSockets.CloseConnection(otherAttempt.Item2, 0, "Other parallel connection attempt succeeded", false);
                        }
                        GameServerConnectionAttempts.Clear(); // Forget about other connection attempts
                        StartClientConnectionHandler = null;
                        _GameServerConnection = new GameServerConnectionSteam(Game, status.m_hConn, status.m_info, endpoint);
                        Log.Write($"Connection '{_GameServerConnection.Name}' established.");
                        handler(new Result<Connection>(_GameServerConnection));
                    }
                    else
                    {
                        EndConnectionAttempt(status, endpoint, "Connected, but another connection already selected.");
                    }
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                    if (otherAttempts.Length == 0)
                    { // Final / only attempt failed
                        EndConnectionAttempt(status, endpoint, "Failed to connect to server.");
                        StartClientConnectionHandler = null;
                        if (handler != null)
                        {
                            handler(new Result<Connection>(new ApplicationException("Failed to connect to server")));
                        }
                    }
                    else
                    {
                        // One of multiple attempts failed. Let others keep trying.
                        EndConnectionAttempt(status, endpoint, $"Other {otherAttempts.Length} attempt(s) ongoing.");
                    }
                    break;
            }
        }

        private void EndConnectionAttempt(SteamNetConnectionStatusChangedCallback_t status, AWEndPointSteam endpoint, string debugMessage)
        {
            Log.Write($"Ending connection attempt to endpoint {endpoint}. {debugMessage} handle: #{status.m_hConn}, " +
                $"state: {SteamApiService.NetStateToString(status.m_info.m_eState)}: {status.m_info.m_eEndReason} / \"{status.m_info.m_szEndDebug}\"");
            SteamNetworkingSockets.CloseConnection(status.m_hConn, 0, debugMessage, true);
            GameServerConnectionAttempts.RemoveAll(t => t.Item2.m_HSteamNetConnection == status.m_hConn.m_HSteamNetConnection);
        }

        public void OnServerConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t status)
        {
            var identity = Steam.IdentityToString(status.m_info.m_identityRemote);
            var connection = GetGameClientConnectionByHandle(status.m_hConn);

            if (connection != null)
            {
                connection.Info = status.m_info;
            }

            var logPrefix = $"Connection #{status.m_hConn}";

            switch (status.m_info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                    if (AllowNewServerConnection())
                    {
                        var acceptResult = SteamGameServerNetworkingSockets.AcceptConnection(status.m_hConn);
                        switch (acceptResult)
                        {
                            case EResult.k_EResultOK:
                                if (connection == null)
                                {
                                    connection = new GameClientConnectionSteam(Game, status.m_hConn, status.m_info);
                                    _GameClientConnections.Add(connection);
                                    Log.Write($"{logPrefix}: Game client connection created: {connection.Name}");
                                }
                                else
                                {
                                    // Should not happen:
                                    Log.Write($"{logPrefix}: Error! Connection status connecting, but book keepping shows this connection is already active! connection: {connection.Name}");
                                }
                                break;
                            default:
                                Log.Write($"{logPrefix}: {status.m_hConn} Accepting client connection failed. result: {acceptResult}");
                                break;
                        }
                    }
                    else
                    {
                        if (connection == null)
                        {
                            connection = new GameClientConnectionSteam(Game, status.m_hConn, status.m_info);
                        }
                        var mess = new ConnectionClosingMessage { Info = "game server refused joining" };
                        connection.Send(mess);
                        Log.Write($"{logPrefix}: Server refused connection.");
                        connection.Dispose();
                    }
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                    if (connection == null)
                    {
                        // Should not happen:
                        Log.Write($"{logPrefix}: Terminal error state for unknown client connection. state: {SteamApiService.NetStateToString(status.m_info.m_eState)}");
                        SteamGameServerNetworkingSockets.CloseConnection(status.m_hConn, (int)status.m_info.m_eState, "Unknown connection closed", true);
                    }
                    else
                    {
                        Log.Write($"{logPrefix}: Terminal error state for client connection, closing {connection.Name}. state: {SteamApiService.NetStateToString(status.m_info.m_eState)}");
                        connection.QueueError("Client connection lost");
                    }
                    break;

            }
        }

        private void RemoveClosedConnections()
        {
            foreach (var connection in _removedClientConnections)
            {
                connection.Dispose();
                _GameClientConnections.Remove(connection);
            }
            _removedClientConnections.Clear();
            if (_GameServerConnection != null && _GameServerConnection.IsDisposed)
            {
                _GameServerConnection = null;
            }

            ProcessDisposedConnections();

            _GameClientConnections.RemoveAll(c => c.IsDisposed);
        }


        public override void StopClient()
        {
            Log.Write("Client closes connection");
            SteamApiService.DisposeCallbackBundle<ClientCallbacks>();
            MessageHandlers.Clear();
            DisposeConnections();
        }

        public override void StopServer()
        {
            Log.Write("Stopping game server");


            foreach (var s in ListenSockets)
            {
                SteamGameServerNetworkingSockets.CloseListenSocket(s);
            }

            SteamApiService.DisposeCallbackBundle<ServerCallbacks>();
            MessageHandlers.Clear();
            var shutdownNotice = new ConnectionClosingMessage { Info = "server shut down" };
            foreach (var conn in GameClientConnections) conn.Send(shutdownNotice);
            DisposeConnections();
        }

        private void DisposeConnections()
        {
            foreach (var conn in AllConnections) conn.Dispose();
            _GameClientConnections.Clear();
            _GameServerConnection = null;
        }

        public override void Dispose()
        {
            DisposeConnections();
            base.Dispose();
        }

        public override void Update()
        {
            foreach (var conn in AllConnections)
            {
                conn.ReceiveMessages();
            }

            HandleClientState();

            foreach (var conn in AllConnections)
            {
                conn.PingInfo.Update(); // send pings, they are unreliable and we don't use Nagle for unreliables, so they should be sent quickly
            }

            // enumerate over a copy to allow adding MessageHandlers during enumeration
            foreach (var handler in MessageHandlers.ToList())
            {
                if (!handler.Disposed) handler.HandleMessages();
            }

            RemoveDisposedMessageHandlers();

            // TODO: Assuming we don't need connection handshaking like the NetworkEngineRaw does.
            // Assuming the Steam Network code keeps the connection alive.

            DetectSilentConnections();

            foreach (var conn in AllConnections) conn.HandleErrors();

            RemoveClosedConnections();
            PurgeUnhandledMessages();
        }

        override public string GetPilotId(int connectionId)
        {
            CSteamID? steamId = GetSteamId(connectionId);

            if (steamId is null)
            {
                throw new NullReferenceException("Steam mode connection, but no steam id found!");
            }
            else
            {
                var id = secureId.MakeId(steamId.Value.ToString());
                Log.Write($"steamId: {steamId} hashed to {id}");
                return id;
            }
        }

    }
}
