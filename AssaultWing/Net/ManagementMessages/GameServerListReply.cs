using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;

namespace AW2.Net.ManagementMessages
{
    /// <summary>
    /// A message from a management server to a game instance, providing a list
    /// of available game servers.
    /// </summary>
    [ManagementMessage("serverlist")]
    public class GameServerListReply : ManagementMessage
    {
        public List<GameServerInfo> GameServers { get; private set; }

        protected override void Deserialize(List<Dictionary<string, string>> tokenizedLines)
        {
            GameServers = tokenizedLines
                .Skip(1)
                .Select(line => GetGameServerInfo(line))
                .Where(info => info != null)
                .ToList();
        }

        private static GameServerInfo GetGameServerInfo(Dictionary<string, string> line)
        {
            try
            {
                return new GameServerInfo
                {
                    Name = line["name"],
                    CurrentPlayers = int.Parse(line["currentclients"]),
                    MaxPlayers = int.Parse(line["maxclients"]),
                    ManagementID = int.Parse(line["id"]),
                    AWVersion = Version.Parse(line["awversion"]),
                };
            }
            catch (Exception e)
            {
                Log.Write("Warning: Skipping invalid game server info", e);
                return null;
            }
        }
    }
}
