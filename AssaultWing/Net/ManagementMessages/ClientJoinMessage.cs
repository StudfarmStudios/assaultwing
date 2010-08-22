using System.Collections.Generic;
using System.Linq;
using System.Net;
using AW2.Helpers;

namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A message from the management server to a game server, informing that 
    /// a game client wants to join the server.
    /// </summary>
    [ManagementMessage("clientjoin")]
    public class ClientJoinMessage : ManagementMessage
    {
        public IPEndPoint[] ClientUDPEndPoints { get; private set; }

        protected override void Deserialize(List<Dictionary<string, string>> tokenizedLines)
        {
            ClientUDPEndPoints = new IPEndPoint[]
            {
                MiscHelper.ParseIPEndPoint(tokenizedLines[0]["udpendpoint"]),
                MiscHelper.ParseIPEndPoint(tokenizedLines[0]["udpendpoint2"]),
            };
        }
    }
}
