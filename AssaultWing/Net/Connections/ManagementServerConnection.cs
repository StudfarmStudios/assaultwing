using System;
using System.Net;
using AW2.Core;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A network connection to a management server.
    /// Note!!! Management server uses a different protocol than other Assault Wing connections.
    /// </summary>
    public class ManagementServerConnection : Connection
    {
        /// <summary>
        /// Creates a new connection to a management server that works solely on UDP.
        /// </summary>
        public ManagementServerConnection(AssaultWing game, IPEndPoint managementServerEndPoint)
            : base(game)
        {
            Name = "Management Server Connection " + ID;
            RemoteUDPEndPoint = managementServerEndPoint;
        }

        public override void Send(Message message)
        {
            var managementMessage = message as ManagementMessage;
            if (managementMessage == null) throw new ArgumentException("Only ManagementMessage instances can be sent to management server", "message");
            Game.NetworkEngine.UDPSocket.Send(managementMessage.Serialize, RemoteUDPEndPoint);
        }

        public override void UpdatePingInfo()
        {
            // Not updating ping
        }
    }
}
