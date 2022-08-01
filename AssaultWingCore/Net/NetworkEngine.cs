using AW2.Core;
using AW2.Net.Connections;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;
using AW2.Helpers;
using AW2.Net.ConnectionUtils;
using AW2.Game;

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
    public abstract class NetworkEngine : AWGameComponent
    {
        private RunningSequenceSingle _gobUpdateLags = new RunningSequenceSingle(TimeSpan.FromSeconds(1));

        public NetworkEngine(AssaultWingCore game, int updateOrder) : base(game, updateOrder)
        {
            MessageHandlers = new List<MessageHandlerBase>();
        }
    
        /// <summary>
        /// Handler of connection results for client that is connecting to a game server.
        /// </summary>
        protected Action<IResult<Connection>>? StartClientConnectionHandler { get; set; }

        /// <summary>
        /// The handlers of network messages.
        /// </summary>
        public List<MessageHandlerBase> MessageHandlers { get; private set; }
    
    
        /// <summary>
        /// Turns this game instance into a game server to whom other game instances
        /// can connect as game clients.
        /// </summary>
        /// <param name="connectionHandler">Handler of connection result.</param>
        public abstract void StartServer(Func<bool> allowNewConnection);
    
        /// <summary>
        /// Turns this game server into a standalone game instance and disposes of
        /// any connections to game clients.
        /// </summary>
        public abstract void StopServer();
    
        /// <summary> 
        /// Turns this game instance into a game client by connecting to a game server.
        /// Poll <c>Connection.ConnectionResults</c> to find out when and if
        /// the connection was successfully estblished.
        /// </summary>
        public abstract void StartClient(AssaultWingCore game, AWEndPoint[] serverEndPoints, Action<IResult<Connection>> connectionHandler);
    
        /// <summary>
        /// Turns this game client into a standalone game instance by disconnecting
        /// from the game server.
        /// </summary>
        public abstract void StopClient();
    
        /// <summary>
        /// Drops the connection to a game client. To be called only as the game server.
        /// </summary>
        public void DropClient(GameClientConnection connection)
        {
            if (Game.NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot drop client in mode " + Game.NetworkMode);

            if (IsRemovedClientConnection(connection)) return;
            if (!GameClientConnections.Contains(connection)) {
                Log.Write($"DropClient called for unknown client connection {connection.Name}");
                return;
            }
            Log.Write("Dropping " + connection.Name);
            connection.ConnectionStatus.State = GameClientStatus.StateType.Dropped;
            AddRemovedClientConnection(connection);

            var droppedPlayers = Game.DataEngine.Spectators.Where(plr => plr.ConnectionID == connection.ID);
            foreach (var plr in droppedPlayers) plr.Disconnect();
            if (droppedPlayers.Any())
            {
                var message = string.Join(" and ", droppedPlayers.Select(plr => plr.Name).ToArray()) + " left the game";
                foreach (var player in Game.DataEngine.Players)
                    player.Messages.Add(new PlayerMessage(message, PlayerMessage.DEFAULT_COLOR));
            }
        }

        /// <summary>
        /// Called from Update when connections to be removed have been disposed, but
        /// not yet removed from the list of game client connections.
        /// </summary>
        protected void ProcessDisposedConnections() {
            if (Game.DataEngine.Arena != null)
            {
                foreach (var conn in GameClientConnections)
                    if (conn.IsDisposed)
                        foreach (var gob in Game.DataEngine.Arena.GobsInRelevantLayers)
                            gob.ClientStatus[1 << conn.ID] = false;
            }
        }

    
        /// <summary>
        /// Is already in the list of removed connections.
        /// </summary>
        public abstract bool IsRemovedClientConnection(GameClientConnection connection);

        /// <summary>
        /// Add to the list of removed client connections to be actually cleaned up handled later.
        /// </summary>
        public abstract void AddRemovedClientConnection(GameClientConnection connection);
    
        /// <summary>
        /// Send dummy UDP packets to probable UDP end points of the client to increase
        /// probability of our NAT forwarding UDP packets from the client to us.
        /// </summary>
        public abstract void DoClientUdpHandshake(GameServerHandshakeRequestTCP mess);
    
        /// <summary>
        /// Sends a message to all game clients. Use this method instead of enumerating
        /// over <see cref="GameClientConnections"/> and sending to each separately.
        /// </summary>
        public void SendToGameClients(Message message)
        {
            foreach (var conn in GameClientConnections) conn.Send(message);
        }

        /// <summary>
        /// Round-trip ping time to the game server.
        /// </summary>
        public TimeSpan ServerPingTime
        {
            get
            {
                if (!IsConnectedToGameServer)
                    throw new InvalidOperationException("Cannot ping server without connection");
                return GameServerConnection.PingInfo.PingTime;
            }
        }

        /// <summary>
        /// Round-trip ping time to a game client.
        /// </summary>
        public TimeSpan GetClientPingTime(int connectionID)
        {
            return GetGameClientConnection(connectionID).PingInfo.PingTime;
        }
    
        /// <summary>
        /// Network connections to game clients. Nonempty only on a game server.
        /// </summary>
        public abstract IEnumerable<GameClientConnection> GameClientConnections { get; }
    
        /// <summary>
        /// Network connection to the game server of the current game session,
        /// or <c>null</c> if no such live connection exists
        /// (including the case that we are the game server).
        /// </summary>
        public abstract Connection GameServerConnection { get; }
    
        /// <summary>
        /// Are we connected to a game server.
        /// </summary>
        public bool IsConnectedToGameServer { get { return GameServerConnection != null; } }
    
        public abstract GameClientConnection GetGameClientConnection(int connectionID);
    
        public abstract Connection GetConnection(int connectionID);
    
        public abstract string GetConnectionAddressString(int connectionID);
    
        protected abstract IEnumerable<ConnectionBase> AllConnections { get; }

        /// <summary>
        /// Returns the number of frames elapsed since the message was sent.
        /// </summary>
        public int GetMessageAge(GameplayMessage message)
        {
          var messageAgeInFrames = Game.DataEngine.ArenaFrameCount - message.FrameNumber;
          // Crude assumption for LagLog: GetMessageAge is called only once for each received message.
          if (Game.Settings.Net.LagLog) _gobUpdateLags.Add(messageAgeInFrames, Game.GameTime.TotalRealTime);
          return Math.Max(0, messageAgeInFrames);
        }
    
        public string GetDebugPrintLagStringOrNull()
        {
          var gobUpdateLags = _gobUpdateLags.Prune(Game.GameTime.TotalRealTime);
          return gobUpdateLags.Count == 0 ? null : string.Format("{0} updates' frame lag Min={1} Max={2} Avg={3}",
              gobUpdateLags.Count, gobUpdateLags.Min, gobUpdateLags.Max, gobUpdateLags.Average);
        }
    
        /// <summary>
        /// Returns the total game time at which the message was current.
        /// </summary>
        public TimeSpan GetMessageGameTime(GameplayMessage message)
        {
            return AssaultWingCore.TargetElapsedTime.Multiply(message.FrameNumber);
        }


        [System.Diagnostics.Conditional("DEBUG")]
        protected void PurgeUnhandledMessages()
        {
            Type lastMessageType = null; // to avoid flooding log messages
            Connection lastConnection = null;
            foreach (var connection in AllConnections)
                connection.Messages.Do(queue => queue.Prune(
                    message => message.CreationTime < Game.GameTime.TotalRealTime - TimeSpan.FromSeconds(30),
                    message =>
                    {
                        if (lastMessageType != message.GetType() || lastConnection != connection)
                        {
                            lastMessageType = message.GetType();
                            lastConnection = connection;
                            Log.Write("WARNING: Purging messages of type " + message.Type + " received from " + connection.Name);
                        }
                    }));
        }        

        protected void DetectSilentConnections()
        {
            foreach (var conn in AllConnections)
                if (conn.PingInfo.IsMissingReplies)
                    conn.QueueError("Ping replies missing.");
        }        
    
        public abstract byte[] GetAssaultWingInstanceKey();

        protected void RemoveDisposedMessageHandlers()
        {
            MessageHandlers = MessageHandlers.Except(MessageHandlers.Where(handler => handler.Disposed)).ToList();
        }

        protected void HandleClientState()
        {
            if (Game.NetworkMode != NetworkMode.Server) return;
            var serverIsPlayingArena = Game.DataEngine.Arena != null && Game.LogicEngine.Enabled;
            foreach (var conn in GameClientConnections)
            {
                var clientIsPlayingArena = conn.ConnectionStatus.IsRunningArena;
                // Note: Some gobs refer to players, so start arena not until player info has been sent.
                if (!clientIsPlayingArena && serverIsPlayingArena && conn.ConnectionStatus.HasPlayerSettings)
                    MakeClientStartArena(conn);
                else if (clientIsPlayingArena && !serverIsPlayingArena)
                    MakeClientStopArena(conn);
            }
        }

        protected void MakeClientStartArena(GameClientConnection conn)
        {
            var arenaName = Game.SelectedArenaName;
            var startGameMessage = new StartGameMessage
            {
                GameplayMode = Game.DataEngine.GameplayMode.Name,
                ArenaID = Game.DataEngine.Arena.ID,
                ArenaToPlay = arenaName,
                ArenaTimeLeft = Game.DataEngine.ArenaFinishTime == TimeSpan.Zero ? TimeSpan.Zero : Game.DataEngine.ArenaFinishTime - Game.GameTime.TotalRealTime,
                WallCount = Game.DataEngine.Arena.Gobs.All<AW2.Game.Gobs.Wall>().Count()
            };
            conn.Send(startGameMessage);
            conn.ConnectionStatus.CurrentArenaName = arenaName;
        }

        protected void MakeClientStopArena(GameClientConnection conn)
        {
            conn.Send(new ArenaFinishMessage());
            conn.ConnectionStatus.CurrentArenaName = null;
        }

    }
}
