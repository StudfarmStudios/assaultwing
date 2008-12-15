using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using System.Net;
using AW2.Net.Messages;
using AW2.Game;

namespace AW2.Net
{
    /// <summary>
    /// Network engine. Takes care of communications between several
    /// Assault Wing instances over the Internet.
    /// </summary>
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
                                clientConnections.Add(result.Value);
                                Log.Write("Server obtained connection from " + result.Value.RemoteEndPoint);
                                break;
                            case NetworkMode.Client:
                                gameServerConnection = result.Value;
                                Log.Write("Client connected to " + result.Value.RemoteEndPoint);
                                JoinGameRequest joinGameRequest = new JoinGameRequest();
                                joinGameRequest.PlayerInfos = new List<JoinGameRequest.PlayerInfo>();
                                data.ForEachPlayer(player => joinGameRequest.PlayerInfos.Add(new JoinGameRequest.PlayerInfo(player)));
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
            switch (AssaultWing.Instance.NetworkMode)
            {
                case NetworkMode.Server:
                    foreach (Connection connection in clientConnections)
                    {
                        // Handle JoinGameRequest from a game client
                        if (connection.Messages.Count<JoinGameRequest>() > 0)
                        {
                            JoinGameRequest message = connection.Messages.Dequeue<JoinGameRequest>();
                            /// TODOO
                        }
                    }
                    break;
                case NetworkMode.Client:
                    // TODO!!!!
                    break;
                case NetworkMode.Standalone:
                    // Do nothing!
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
