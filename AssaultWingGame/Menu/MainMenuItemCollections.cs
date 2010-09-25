using AW2.Core;
using AW2.Helpers;
using AW2.Net.ManagementMessages;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;

namespace AW2.Menu
{
    /// <summary>
    /// All possible item collections for <see cref="MainMenuComponent"/>.
    /// </summary>
    public class MainMenuItemCollections
    {
        /// <summary>
        /// The very first menu when the game starts.
        /// </summary>
        public MainMenuItemCollection StartItems { get; private set; }

        /// <summary>
        /// Menu for establishing a network game.
        /// </summary>
        public MainMenuItemCollection NetworkItems { get; private set; }

        public MainMenuItemCollections(MenuEngineImpl menuEngine)
        {
            StartItems = new MainMenuItemCollection("Start Menu");
            StartItems.Add(new MainMenuItem(menuEngine)
            {
                Name = "Play Local",
                Action = component => component.MenuEngine.ActivateComponent(MenuComponentType.Equip)
            });
            StartItems.Add(new MainMenuItem(menuEngine)
            {
                Name = "Play at the Battlefront",
                Action = component =>
                {
                    InitializeNetworkItems(menuEngine);
                    component.SetItems(NetworkItems);
                    menuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                    menuEngine.Game.NetworkEngine.ConnectToManagementServer();
                    MessageHandlers.ActivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(
                        mess => HandleGameServerListReply(mess, menuEngine),
                        mess => HandleJoinGameServerReply(mess, menuEngine)));
                    menuEngine.Game.NetworkEngine.ManagementServerConnection.Send(new GameServerListRequest());
                }
            });
            StartItems.Add(new MainMenuItem(menuEngine)
            {
                Name = "Setup",
                Action = component => Log.Write("NOTE: Main menu item 'Setup' is not implemented")
            });
            StartItems.Add(new MainMenuItem(menuEngine)
            {
                Name = "Quit",
                Action = component => AssaultWingProgram.Instance.Exit()
            });
        }

        private void InitializeNetworkItems(MenuEngineImpl menuEngine)
        {
            NetworkItems = new MainMenuItemCollection("Battlefront Menu");
            NetworkItems.Add(new MainMenuItem(menuEngine)
            {
                Name = "Play as Server",
                Action = component =>
                {
                    if (menuEngine.Game.NetworkMode != NetworkMode.Standalone) return;
                    if (!menuEngine.Game.StartServer(result => MessageHandlers.IncomingConnectionHandlerOnServer(result, AllowNewConnection))) return;
                    component.MenuEngine.ActivateComponent(MenuComponentType.Equip);

                    // HACK: Force one local player and Amazonas as the only arena.
                    menuEngine.Game.DataEngine.Spectators.Remove(player => menuEngine.Game.DataEngine.Spectators.Count > 1);
                    menuEngine.Game.DataEngine.ArenaPlaylist = new AW2.Helpers.Collections.Playlist(new string[] { "Amazonas" });
                }
            });
        }

        private bool AllowNewConnection()
        {
            return ((AssaultWing)AssaultWing.Instance).GameState == AW2.Core.GameState.Menu;
        }

        private void HandleGameServerListReply(GameServerListReply mess, MenuEngineImpl menuEngine)
        {
            foreach (var server in mess.GameServers)
                NetworkItems.Add(new MainMenuItem(menuEngine)
                {
                    Name = string.Format("Connect to {0} [{1}/{2}]", server.Name, server.CurrentPlayers, server.MaxPlayers),
                    Action = component =>
                    {
                        if (menuEngine.Game.NetworkMode != NetworkMode.Standalone) return;
                        var joinRequest = new JoinGameServerRequest
                        {
                            GameServerManagementID = server.ManagementID,
                            PrivateUDPEndPoint = menuEngine.Game.NetworkEngine.UDPSocket.PrivateLocalEndPoint,
                        };
                        menuEngine.Game.NetworkEngine.ManagementServerConnection.Send(joinRequest);
                    }
                });
        }

        private static void HandleJoinGameServerReply(JoinGameServerReply mess, MenuEngineImpl menuEngine)
        {
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(null, null));
            menuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
            menuEngine.Game.StartClient(mess.GameServerEndPoints, result => ClientConnectedCallback(result, menuEngine));
        }

        private static void ClientConnectedCallback(AW2.Net.Result<AW2.Net.Connections.Connection> result, MenuEngineImpl menuEngine)
        {
            if (!result.Successful)
            {
                Log.Write("Failed to connect to server: " + result.Error);
                menuEngine.Game.StopClient();
                return;
            }
            MessageHandlers.ActivateHandlers(MessageHandlers.GetClientMenuHandlers(() => menuEngine.ActivateComponent(MenuComponentType.Equip), mess => HandleStartGameMessage(mess, menuEngine)));

            // HACK: Force one local player.
            menuEngine.Game.DataEngine.Spectators.Remove(player => menuEngine.Game.DataEngine.Spectators.Count > 1);

            var joinRequest = new JoinGameRequest { CanonicalStrings = CanonicalString.CanonicalForms };
            menuEngine.Game.NetworkEngine.GameServerConnection.Send(joinRequest);
        }

        private static void HandleStartGameMessage(StartGameMessage mess, MenuEngineImpl menuEngine)
        {
            menuEngine.Game.DataEngine.ArenaPlaylist = new AW2.Helpers.Collections.Playlist(mess.ArenaPlaylist);
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetClientMenuHandlers(null, null));

            // Prepare and start playing the game.
            menuEngine.ProgressBarAction(menuEngine.Game.PrepareFirstArena,
                () => MessageHandlers.ActivateHandlers(MessageHandlers.GetClientGameplayHandlers()));
            menuEngine.Deactivate();
        }
    }
}
