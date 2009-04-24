using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Microsoft.Xna.Framework;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.Messages;

namespace AW2.Net
{
    /// <summary>
    /// Network engine. Takes care of communications between several
    /// Assault Wing instances over the Internet.
    /// </summary>
    /// A game server can communicate with its game clients by sending
    /// multicast messages via <c>SendToClients</c> and receiving
    /// messages via <c>ReceiveFromClients</c>. Messages can be
    /// received by type, so each part of the game logic can poll for 
    /// messages that are relevant to it without interfering with
    /// other parts of the game logic. Each received message
    /// contains an identifier of the connection to the client who
    /// sent that message.
    /// 
    /// A game client can communicate with its game server by sending
    /// messages via <c>SendToServer</c> and receiving messages via
    /// <c>ReceiveFromServer</c>.
    /// 
    /// All game instances can have a connection to a game management
    /// server. This hasn't been implemented yet.
    /// 
    /// <seealso cref="Message.ConnectionId"/>
    public class NetworkEngine : GameComponent
    {
        #region Type definitions

        enum ConnectionType
        {
            ManagementServer,
            GameServer,
            GameClient
        }

        #endregion Type definitions

        #region Fields

        /// <summary>
        /// TCP connection port.
        /// </summary>
        int port = 'A' * 256 + 'W';

        /// <summary>
        /// Network connection to the management server, 
        /// or <c>null</c> if no such live connection exists.
        /// </summary>
        ManagementServerConnection managementServerConnection;

        /// <summary>
        /// Network connection to the game server of the current game session, 
        /// or <c>null</c> if no such live connection exists 
        /// (including the case that we are the game server).
        /// </summary>
        GameServerConnection gameServerConnection;

        /// <summary>
        /// Network connections to game clients. Nonempty only when 
        /// we are a game server.
        /// </summary>
        LinkedList<GameClientConnection> clientConnections;

        /// <summary>
        /// Clients to be removed from <c>clientConnections</c>.
        /// </summary>
        List<GameClientConnection> removedClientConnections;

        /// <summary>
        /// Handler of connection results for client that is connecting to a game server.
        /// </summary>
        Action<Result<Connection>> startClientConnectionHandler;

        /// <summary>
        /// Handler of connection results for server that is listening for game client connections.
        /// </summary>
        Action<Result<Connection>> startServerConnectionHandler;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Creates a network engine for a game.
        /// </summary>
        /// <param name="game">The game.</param>
        public NetworkEngine(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            clientConnections = new LinkedList<GameClientConnection>();
            removedClientConnections = new List<GameClientConnection>();
        }

        #endregion Constructor

        #region Public interface

        /// <summary>
        /// Are we connected to a game server.
        /// </summary>
        public bool IsConnectedToGameServer { get { return gameServerConnection != null; } }

        /// <summary>
        /// Are we connected to the management server.
        /// </summary>
        public bool IsConnectedToManagementServer { get { return managementServerConnection != null; } }

        /// <summary>
        /// Turns this game instance into a game server to whom other game instances
        /// can connect as game clients.
        /// </summary>
        /// <param name="connectionHandler">Handler of connection result.</param>
        public void StartServer(Action<Result<Connection>> connectionHandler)
        {
            Log.Write("Server starts listening");
            startServerConnectionHandler = connectionHandler;
            Connection.StartListening(port, "I listen");
        }

        /// <summary>
        /// Turns this game server into a standalone game instance and disposes of
        /// any connections to game clients.
        /// </summary>
        public void StopServer()
        {
            Log.Write("Server stops listening");
            Connection.StopListening();
            foreach (Connection connection in clientConnections)
                connection.Dispose();
            clientConnections.Clear();
        }

        /// <summary>
        /// Turns this game instance into a game client by connecting to a game server.
        /// </summary>
        /// Poll <c>Connection.ConnectionResults</c> to find out when and if
        /// the connection was successfully estblished.
        /// <param name="serverAddress">Network address of the server.</param>
        /// <param name="connectionHandler">Handler of connection result.</param>
        public void StartClient(string serverAddress, Action<Result<Connection>> connectionHandler)
        {
            Log.Write("Client starts connecting");
            startClientConnectionHandler = connectionHandler;
            IPAddress serverIp;
            if (!System.Net.IPAddress.TryParse(serverAddress, out serverIp))
                throw new ArgumentException("Not a valid IP address: " + serverAddress);
            Connection.Connect(serverIp, port, "I connect");
        }

        /// <summary>
        /// Turns this game client into a standalone game instance by disconnecting
        /// from the game server.
        /// </summary>
        public void StopClient()
        {
            Log.Write("Client closes connection");
            if (gameServerConnection != null)
            {
                gameServerConnection.Dispose();
                gameServerConnection = null;
            }
        }

        /// <summary>
        /// Drops the connection to a game client. To be called only
        /// as the game server.
        /// </summary>
        public void DropClient(int connectionId)
        {
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Server)
                throw new InvalidOperationException("Cannot drop client in mode " + AssaultWing.Instance.NetworkMode);
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

            GameClientConnection connection = GetClientConnection(connectionId);
            removedClientConnections.Add(connection);

            // Remove the client's players.
            data.ForEachPlayer(player =>
            {
                List<string> droppedPlayerNames = new List<string>();
                data.ForEachPlayer(plr =>
                {
                    if (plr.ConnectionId == connection.Id)
                        droppedPlayerNames.Add(plr.Name);
                });
                string message = string.Join(" and ", droppedPlayerNames.ToArray()) + " dropped out";
                if (!player.IsRemote)
                    player.SendMessage(message);
            });
            data.RemovePlayers(player => player.ConnectionId == connection.Id);
        }

        /// <summary>
        /// Sends a message to the game server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToServer(Message message)
        {
            if (gameServerConnection == null)
                throw new InvalidOperationException("Cannot send without connection to server");
            try
            {
#if NETWORK_DEBUG
                Log.Write("DEBUG: sending to server: " + message);
#endif
                gameServerConnection.Send(message);
            }
            catch (SocketException e)
            {
                gameServerConnection.Errors.Do(queue => queue.Enqueue(e));
            }
        }

        /// <summary>
        /// Receives a message from the game server.
        /// </summary>
        /// <typeparam name="T">Type of message to receive.</typeparam>
        /// <returns>The oldest received message of the type received from the game server,
        /// or <c>null</c> if no messages of the type were unreceived from the game server.</returns>
        public T ReceiveFromServer<T>() where T : Message
        {
            if (gameServerConnection == null)
                throw new InvalidOperationException("Cannot receive without connection to server");
            if (gameServerConnection.Messages.Count<T>() > 0)
            {
                T message = gameServerConnection.Messages.Dequeue<T>();
#if NETWORK_DEBUG
                Log.Write("DEBUG: received from server: " + message);
#endif
                return message;
            }
            return null;
        }

        /// <summary>
        /// Receives messages from the game server while a condition holds.
        /// </summary>
        /// <typeparam name="T">Type of message to receive.</typeparam>
        /// <param name="handler">Handler of received messages. If the handler returns
        /// <c>true</c> then another message is received, if there is any.
        /// If the handler returns <c>false</c>, then the currently handled message is returned
        /// back to the front of the queue and no more messages will be received.
        /// The requeued message will be the first one to receive next time.</param>
        public void ReceiveFromServerWhile<T>(Predicate<T> handler) where T : Message
        {
            if (gameServerConnection == null)
                throw new InvalidOperationException("Cannot receive without connection to server");
            while (gameServerConnection.Messages.Count<T>() > 0)
            {
                T message = gameServerConnection.Messages.Dequeue<T>();
                if (!handler(message))
                {
                    gameServerConnection.Messages.Requeue(message);
                    break;
                }
#if NETWORK_DEBUG
                else
                    Log.Write("DEBUG: received from server: " + message);
#endif
            }
        }

        /// <summary>
        /// Sends a message to all connected game clients.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToClients(Message message)
        {
#if NETWORK_DEBUG
            Log.Write("DEBUG: sending to clients: " + message);
#endif
            foreach (Connection connection in clientConnections)
                connection.Send(message);
        }

        /// <summary>
        /// Sends a message to a game client.
        /// </summary>
        /// <param name="connectionId">Identifier of the connection to the game client.</param>
        /// <param name="message">The message to send.</param>
        public void SendToClient(int connectionId, Message message)
        {
            Connection connection = GetClientConnection(connectionId);
#if NETWORK_DEBUG
            Log.Write("DEBUG: sending to client " + connectionId + ": " + message);
#endif
            connection.Send(message);
        }

        /// <summary>
        /// Receives a message from any game client.
        /// </summary>
        /// This method receives messages from all game clients in
        /// unspecified order.
        /// <typeparam name="T">Type of message to receive.</typeparam>
        /// <returns>The oldest received message of the type received from a game client,
        /// or <c>null</c> if no messages of the type were unreceived from game clients.</returns>
        public T ReceiveFromClients<T>() where T : Message
        {
            foreach (Connection connection in clientConnections)
                if (connection.Messages.Count<T>() > 0)
                {
                    T message = connection.Messages.Dequeue<T>();
#if NETWORK_DEBUG
                    Log.Write("DEBUG: received from client connection " + connection.Id + ": " + message);
#endif
                    return message;
                }
            return null;
        }

        /// <summary>
        /// Receives a message from a specific game client.
        /// </summary>
        /// <param name="connectionId">Identifier of the connection to the game client to receive from.</param>
        /// <typeparam name="T">Type of message to receive.</typeparam>
        /// <returns>The oldest received message of the type received from the game client,
        /// or <c>null</c> if no messages of the type were unreceived from the game client.</returns>
        public T ReceiveFromClient<T>(int connectionId) where T : Message
        {
            foreach (Connection connection in clientConnections)
                if (connection.Id == connectionId)
                {
                    if (connection.Messages.Count<T>() > 0)
                    {
                        T message = connection.Messages.Dequeue<T>();
#if NETWORK_DEBUG
                        Log.Write("DEBUG: received from client connection " + connection.Id + ": " + message);
#endif
                        return message;
                    }
                    return null;
                }
            throw new ArgumentException("Invalid connection ID");
        }

        #endregion Public interface

        #region GameComponent methods

        /// <summary>
        /// Performs game logic.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update</param>
        public override void Update(GameTime gameTime)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

            // Handle established connections.
            Connection.ConnectionResults.Do(queue =>
            {
                while (queue.Count > 0)
                {
                    var result = queue.Dequeue();
                    if (result.Id == "I connect")
                    {
                        if (result.Successful)
                            gameServerConnection = (GameServerConnection)result.Value;
                        startClientConnectionHandler(result);
                    }
                    if (result.Id == "I listen")
                    {
                        if (result.Successful)
                            clientConnections.AddLast((GameClientConnection)result.Value);
                        startServerConnectionHandler(result);
                    }
                }
            });

            // Manage existing connections.
            // TODO: Move message handling to LogicEngine and other more appropriate places
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                // Handle JoinGameRequests from game clients.
                JoinGameRequest message = null;
                while ((message = ReceiveFromClients<JoinGameRequest>()) != null)
                {
                    JoinGameReply reply = new JoinGameReply();
                    reply.PlayerIdChanges = new JoinGameReply.IdChange[message.PlayerInfos.Count];
                    int changeI = 0;
                    foreach (PlayerInfo info in message.PlayerInfos)
                    {
                        Player player = new Player(info.name, info.shipTypeName, info.weapon1TypeName, info.weapon2TypeName, message.ConnectionId);
                        data.AddPlayer(player);
                        reply.PlayerIdChanges[changeI].oldId = info.id;
                        reply.PlayerIdChanges[changeI].newId = player.Id;
                        changeI++;
                    }
                    SendToClient(message.ConnectionId, reply);
                }
            }
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client && gameServerConnection != null)
            {
                // Handle JoinGameReplies from the game server.
                JoinGameReply message = null;
                while ((message = ReceiveFromServer<JoinGameReply>()) != null)
                {
                    foreach (JoinGameReply.IdChange change in message.PlayerIdChanges)
                    {
                        data.GetPlayer(change.oldId).Id = change.newId;
                    }
                }
            }

            // Handle occurred errors.
            ForEachConnection(connection => connection.HandleErrors());

            // Finish removing dropped client connections.
            foreach (GameClientConnection connection in removedClientConnections)
                clientConnections.Remove(connection);

#if DEBUG
            // Look for unhandled messages.
            Type lastMessageType = null; // to avoid flooding log messages
            Connection lastConnection = null;
            ForEachConnection(connection => connection.Messages.Prune(TimeSpan.FromSeconds(10), message =>
            {
                if (lastMessageType != message.GetType() || lastConnection != connection)
                {
                    lastMessageType = message.GetType();
                    lastConnection = connection;
                    Log.Write("WARNING: Purging messages of type " + message.Type + " received from " + connection.Name);
                }
            }));
#endif
        }

        /// <summary>
        /// Releases the unmanaged resources used by the GameComponent 
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            foreach (Connection connection in clientConnections)
                connection.Dispose();
            base.Dispose(disposing);
        }

        #endregion GameComponent methods

        #region Private methods

        /// <summary>
        /// Returns a client connection by its connection identifier.
        /// </summary>
        /// <param name="connectionId">The identifier of the client connection.</param>
        /// <returns>The client connection.</returns>
        GameClientConnection GetClientConnection(int connectionId)
        {
            foreach (GameClientConnection connection in clientConnections)
                if (connection.Id == connectionId)
                    return connection;
            throw new ArgumentException("No client connection with ID " + connectionId);
        }

        /// <summary>
        /// Performs an operation on each established connection.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        void ForEachConnection(Action<Connection> action)
        {
            if (managementServerConnection != null)
                action(managementServerConnection);
            if (gameServerConnection != null)
                action(gameServerConnection);
            foreach (Connection clientConnection in clientConnections)
                action(clientConnection);
        }

        #endregion Private methods
    }
}
