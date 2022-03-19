using System;
using System.Collections.Generic;

namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A message from a game server to a management server, requesting to update
    /// the game server's status.
    /// </summary>
    [ManagementMessage("updateserver")]
    public class UpdateGameServerMessage : ManagementMessage
    {
        public int CurrentClients { get; set; }

        protected override string[] Parameters
        {
            get
            {
                return new[]
                {
                    "currentclients=" + CurrentClients,
                };
            }
        }

        protected override void Deserialize(List<Dictionary<string, string>> tokenizedLines)
        {
            throw new NotImplementedException();
        }
    }
}
