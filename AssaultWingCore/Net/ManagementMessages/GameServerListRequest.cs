using System;
using System.Collections.Generic;

namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A message from a game instance to a management server, requesting a list
    /// of available game servers.
    /// </summary>
    [ManagementMessage("listservers")]
    public class GameServerListRequest : ManagementMessage
    {
        protected override void Deserialize(List<Dictionary<string, string>> tokenizedLines)
        {
            throw new NotImplementedException();
        }
    }
}
