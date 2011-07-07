using System;
using System.Collections.Generic;

namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A message from a game server to a management server, requesting to get
    /// registered to the list of known game servers.
    /// </summary>
    [ManagementMessage("addserver")]
    public class RegisterGameServerMessage : ManagementMessage
    {
        public string GameServerName { get; set; }
        public int MaxClients { get; set; }
        public int TCPPort { get; set; }
        public AWEndPoint LocalEndPoint { get; set; }
        public Version AWVersion { get; set; }

        protected override string[] Parameters
        {
            get
            {
                return new string[]
                {
                    "name=" + GameServerName,
                    "maxclients=" + MaxClients,
                    "tcpport=" + TCPPort,
                    "localendpoint=" + LocalEndPoint,
                    "awversion=" + AWVersion,
                };
            }
        }

        protected override void Deserialize(List<Dictionary<string, string>> tokenizedLines)
        {
            throw new NotImplementedException();
        }
    }
}
