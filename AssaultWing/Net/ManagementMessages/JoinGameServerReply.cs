using System.Collections.Generic;
using System.Linq;

namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A message from a game instance to a management server, requesting a list
    /// of available game servers.
    /// </summary>
    [ManagementMessage("serveraddress")]
    public class JoinGameServerReply : ManagementMessage
    {
        public bool Success { get { return GameServerEndPoint != null; } }
        public AWEndPoint GameServerEndPoint { get; private set; }
        public string FailMessage { get; private set; }

        protected override void Deserialize(List<Dictionary<string, string>> tokenizedLines)
        {
            var tokens = tokenizedLines[0];
            if (tokens.ContainsKey("fail"))
            {
                FailMessage = tokens["fail"];
            }
            else
            {
                string endPointString = tokens["server"];
                GameServerEndPoint = AWEndPoint.Parse(endPointString);
            }
        }
    }
}
