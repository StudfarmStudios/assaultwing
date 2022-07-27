using AW2.Game.Logic;
namespace AW2.Net
{
    /// <summary>
    /// General information about an Assault Wing game server.
    /// </summary>
    public class GameServerInfo
    {
        public string Name { get; init; }

        public string ArenaName {get; init; }

        public GameplayMode GameplayMode {get; init; }
        public int MaxPlayers { get; init; }
        public int CurrentPlayers { get; init; }
        public int Bots { get; init; }

        public int Ping { get; init; }

        public int WaitingPlayers { get; init; }

        public Version AWVersion { get; init; }
    }
}
