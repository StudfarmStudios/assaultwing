using AW2.Core;
using AW2.Net.Connections;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;
using AW2.Helpers;

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
        }
    
        /// <summary>
        /// The handlers of network messages.
        /// </summary>
        public abstract List<MessageHandlerBase> MessageHandlers { get; }
    
    
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
        public abstract void DropClient(int connectionID);
    
        /// <summary>
        /// Send dummy UDP packets to probable UDP end points of the client to increase
        /// probability of our NAT forwarding UDP packets from the client to us.
        /// </summary>
        public abstract void DoClientUdpHandshake(GameServerHandshakeRequestTCP mess);
    
        /// <summary>
        /// Sends a message to all game clients. Use this method instead of enumerating
        /// over <see cref="GameClientConnections"/> and sending to each separately.
        /// </summary>
        public abstract void SendToGameClients(Message message);
    
        /// <summary>
        /// Round-trip ping time to the game server.
        /// </summary>
        public abstract TimeSpan ServerPingTime { get; }
    
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
        public abstract bool IsConnectedToGameServer { get; }
    
        public abstract GameClientConnection GetGameClientConnection(int connectionID);
    
        public abstract Connection GetConnection(int connectionID);
    
        public abstract string GetConnectionAddressString(int connectionID);
    
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
    
        public abstract byte[] GetAssaultWingInstanceKey();
    }
}
