using System;
using AW2.Game.Logic;
using Steamworks;
namespace AW2.Net
{
    /// <summary>
    /// General information about an Assault Wing game server.
    /// </summary>
    public class GameServerInfo
    {
        public int ServerIndex {get; init;}
        public gameserveritem_t SteamDetails { get; init; }
        public string Name => SteamDetails.GetServerName();

        public string ArenaName => SteamDetails.GetMap();

        public string GameplayMode => ""; // TODO: Peter: Server rules query?

        public int MaxPlayers => SteamDetails.m_nMaxPlayers;
        public int CurrentPlayers => SteamDetails.m_nPlayers;
        public int Bots => SteamDetails.m_nBotPlayers;

        public int Ping => SteamDetails.m_nPing;

        public int WaitingPlayers { get; init; } // TODO: Peter: Server rules query?

        public System.Version AWVersion { get; init; } // TODO: Peter: Server rules query?

        override public string ToString() {
            var details = SteamDetails;
            return $"GameServerInfo index:{ServerIndex} steam version:{details.m_nServerVersion} map:{details.GetMap()} name:{details.GetServerName()} addr:{details.m_NetAdr.GetConnectionAddressString()}";
        }

    }
}
