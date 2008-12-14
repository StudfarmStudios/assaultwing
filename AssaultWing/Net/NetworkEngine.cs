using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using System.Net;

namespace AW2.Net
{
    /// <summary>
    /// Network engine. Takes care of communications between several
    /// Assault Wing instances over the Internet.
    /// </summary>
    public class NetworkEngine : GameComponent
    {
        #region Fields

        int port = 'A' * 256 + 'W';
        Connection managementServerConnection;
        Connection gameServerConnection;
        List<Connection> clientConnections;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Creates a network engine for a game.
        /// </summary>
        /// <param name="game">The game.</param>
        public NetworkEngine(Microsoft.Xna.Framework.Game game)
            : base(game)
        {
            clientConnections = new List<Connection>();
        }

        #endregion Constructor

        #region Public interface

        /// <summary>
        /// Network connection to the management server, 
        /// or <c>null</c> if no such live connection exists.
        /// </summary>
        public Connection ManagementServerConnection { get { return managementServerConnection; } set { managementServerConnection = value; } }

        /// <summary>
        /// Network connection to the game server of the current game session, 
        /// or <c>null</c> if no such live connection exists 
        /// (including the case that we are the game server).
        /// </summary>
        public Connection GameServerConnection { get { return gameServerConnection; } set { gameServerConnection = value; } }

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
            gameServerConnection.Dispose();
            gameServerConnection = null;
        }



        /// <summary>
        /// Adds a network connection to a game client.
        /// </summary>
        /// <param name="connection">The connection to add.</param>
        public void AddClientConnection(Connection connection) // TODO: This is not part of the public interface
        {
            clientConnections.Add(connection);
        }

        /// <summary>
        /// Closes and removes a network connection to a game client.
        /// </summary>
        /// <param name="connection">The connection to remove.</param>
        public void RemoveClientConnection(Connection connection)
        {
            connection.Dispose();
            clientConnections.Remove(connection);
        }

        /// <summary>
        /// Performs the specified action on each network connection to a game client.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each connection.</param>
        public void ForEachClientConnection(Action<Connection> action)
        {
            foreach (Connection connection in clientConnections)
                action(connection);
        }

        #endregion Public interface

        #region GameComponent methods

        /// <summary>
        /// Performs game logic.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update</param>
        public override void Update(GameTime gameTime)
        {
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
                                AddClientConnection(result.Value);
                                Log.Write("Server obtained connection from " + result.Value.RemoteEndPoint);
                                break;
                            case NetworkMode.Client:
                                GameServerConnection = result.Value;
                                Log.Write("Client connected to " + result.Value.RemoteEndPoint);
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
        }

        #endregion GameComponent methods
    }
}
