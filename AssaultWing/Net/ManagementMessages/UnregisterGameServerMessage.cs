namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A message from a game server to a management server, requesting to get
    /// unregistered from the list of known game servers.
    /// </summary>
    [ManagementMessage("removeserver")]
    public class UnregisterGameServerMessage : ManagementMessage
    {
        protected override void Deserialize(System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>> tokenizedLines)
        {
            throw new System.NotImplementedException();
        }
    }
}
