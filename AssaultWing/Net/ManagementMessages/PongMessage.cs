using System.Collections.Generic;

namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A ping reply from a management server to a game server.
    /// </summary>
    [ManagementMessage("pong")]
    public class PongMessage : ManagementMessage
    {
        protected override void Deserialize(List<Dictionary<string, string>> tokenizedLines)
        {
            throw new System.NotImplementedException();
        }
    }
}
