using System;
using System.Collections.Generic;

namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A message from a game client to a management server, requesting to join a game server.
    /// </summary>
    [ManagementMessage("joinserver")]
    public class JoinGameServerRequest : ManagementMessage
    {
        public int GameServerManagementID { get; set; }

        protected override string[] Parameters
        {
            get
            {
                return new string[] { "serverid=" + GameServerManagementID };
            }
        }

        protected override void Deserialize(List<Dictionary<string, string>> tokenizedLines)
        {
            throw new NotImplementedException();
        }
    }
}
