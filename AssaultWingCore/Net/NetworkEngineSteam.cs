using System.Net;
using System.Net.Sockets;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;
using AW2.Net.Connections;
using AW2.Net.ConnectionUtils;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;

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
        public NetworkEngineSteam(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        public override IEnumerable<GameClientConnection> GameClientConnections => throw new NotImplementedException();

        private ConnectionSteam _GameServerConnection;

        public override Connection GameServerConnection { get { return _GameServerConnection; } }

        protected override IEnumerable<ConnectionBase> AllConnections => throw new NotImplementedException();

        public override void DoClientUdpHandshake(GameServerHandshakeRequestTCP mess)
        {
            throw new NotImplementedException();
        }

        public override void DropClient(int connectionID)
        {
            throw new NotImplementedException();
        }

        public override byte[] GetAssaultWingInstanceKey()
        {
            throw new NotImplementedException();
        }

        public override Connection GetConnection(int connectionID)
        {
            throw new NotImplementedException();
        }

        public override string GetConnectionAddressString(int connectionID)
        {
            throw new NotImplementedException();
        }

        public override GameClientConnection GetGameClientConnection(int connectionID)
        {
            throw new NotImplementedException();
        }

        public override void StartClient(AssaultWingCore game, AWEndPoint[] serverEndPoints, Action<IResult<Connection>> connectionHandler)
        {
            var rawEndPoints = serverEndPoints.OfType<AWEndPointSteam>().ToArray() ?? Array.Empty<AWEndPointSteam>();
            var endPointsString = string.Join(", ", serverEndPoints.Select(e => e.ToString()));
            if (rawEndPoints.Length != serverEndPoints.Length) {
                throw new ArgumentException("NetworkEngineSteam can only handle end points of the format ip:host:port and other steam network identity formats.\n" + 
                    $"Some of these are not compatible '{endPointsString}'");
            }
            Log.Write($"Client starts connecting to {endPointsString}");
            throw new NotImplementedException();
        }

        public override void StartServer(Func<bool> allowNewConnection)
        {
            throw new NotImplementedException();
        }

        public override void StopClient()
        {
            throw new NotImplementedException();
        }

        public override void StopServer()
        {
            throw new NotImplementedException();
        }
    }
}