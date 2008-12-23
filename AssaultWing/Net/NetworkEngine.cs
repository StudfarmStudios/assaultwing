using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Net.Messages;
using AW2.Game;

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
        #region Fields

        /// <summary>
        /// TCP connection port.
        /// </summary>
        int port = 'A' * 256 + 'W';

        /// <summary>
        /// Network connection to the management server, 
        /// or <c>null</c> if no such live connection exists.
        /// </summary>
        Connection managementServerConnection;

        /// <summary>
        /// Network connection to the game server of the current game session, 
        /// or <c>null</c> if no such live connection exists 
        /// (including the case that we are the game server).
        /// </summary>
        Connection gameServerConnection;

        /// <summary>
        /// Network connections to game clients. Nonempty only when 
        /// we are a game server.
        /// </summary>
        LinkedList<Connection> clientConnections;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Creates a network engine for a game.
        /// </summary>
        /// <param name="game">The game.</param>
        public NetworkEngine(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            clientConnections = new LinkedList<Connection>();
        }

        #endregion Constructor

        #region Public interface

        /// <summary>
        /// Turns this game instance into a game server to whom other game instances
        /// can connect as game clients.
        /// </summary>
        public void StartServer()
        {
            Log.Write("Server starts listening");
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
        /// <param name="serverAddress">Network address of the server.</param>
        public void StartClient(string serverAddress)
        {
            Log.Write("Client starts connecting");
            Connection.Connect(IPAddress.Parse(serverAddress), port, "I connect");
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
        /// Sends a message to the game server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToServer(Message message)
        {
            if (gameServerConnection == null)
                throw new InvalidOperationException("Cannot send without connection to server");
            gameServerConnection.Send(message);
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
                return gameServerConnection.Messages.Dequeue<T>();
            return null;
        }

        /// <summary>
        /// Sends a message to all connected game clients.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToClients(Message message)
        {
            foreach (Connection connection in clientConnections)
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
                    return connection.Messages.Dequeue<T>();
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
                        return connection.Messages.Dequeue<T>();
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
                    Result<Connection> result = queue.Dequeue();
                    if (result.Successful)
                    {
                        switch (AssaultWing.Instance.NetworkMode)
                        {
                            case NetworkMode.Server:
                                clientConnections.AddLast(result.Value);
                                Log.Write("Server obtained connection from " + result.Value.RemoteEndPoint);
                                break;
                            case NetworkMode.Client:
                                gameServerConnection = result.Value;
                                Log.Write("Client connected to " + result.Value.RemoteEndPoint);
                                JoinGameRequest joinGameRequest = new JoinGameRequest();
                                joinGameRequest.PlayerInfos = new List<PlayerInfo>();
                                data.ForEachPlayer(player => joinGameRequest.PlayerInfos.Add(new PlayerInfo(player)));
                                gameServerConnection.Send(joinGameRequest);
                                break;
                            default: throw new InvalidOperationException("Cannot handle new network connection in " + AssaultWing.Instance.NetworkMode + " state");
                        }
                    }
                    else
                    {
                        switch (AssaultWing.Instance.NetworkMode)
                        {
                            case NetworkMode.Server:
                                Log.Write("Server saw a client fail to connect: " + result.Error);
                                break;
                            case NetworkMode.Client:
                                Log.Write("Client failed to connect: " + result.Error);
                                break;
                            default: throw new InvalidOperationException("Cannot handle failed network connection in " + AssaultWing.Instance.NetworkMode + " state");
                        }
                    }
                }
            });

            // Manage existing connections.
            // TODO: Move message handling to LogicEngine and other more appropriate places
            switch (AssaultWing.Instance.NetworkMode)
            {
                case NetworkMode.Server:
                    // Handle JoinGameRequests from game clients.
                    JoinGameRequest message = null;
                    while ((message = ReceiveFromClients<JoinGameRequest>()) != null)
                    {
                        foreach (PlayerInfo info in message.PlayerInfos)
                        {
                            Player player = new Player(info.name, info.shipTypeName, info.weapon1TypeName, info.weapon2TypeName, message.ConnectionId);
                            data.AddPlayer(player);
                            // TODO: Send reply that contains the players' IDs as they are on the server. 
                            // TODO: Think of the network game start procedure. What messages when?
                            // TODO: And can this be reused when connecting via management server?
                        }
                    }
                    break;
                case NetworkMode.Client:
                    // TODO!!!!
                    break;
                case NetworkMode.Standalone:
                    // Do nothing
                    break;
            }
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
    }
}
