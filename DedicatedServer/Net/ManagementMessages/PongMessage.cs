using System.Collections.Generic;

namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A ping reply from a game server to a management server.
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
