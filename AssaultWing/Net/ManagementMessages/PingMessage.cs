using System.Collections.Generic;

namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A ping request from a management server to a game server.
    /// </summary>
    [ManagementMessage("ping")]
    public class PingMessage : ManagementMessage
    {
        protected override void Deserialize(List<Dictionary<string, string>> tokenizedLines)
        {
        }
    }
}
