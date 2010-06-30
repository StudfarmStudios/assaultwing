using System;
using AW2.Core;
using AW2.UI;
using AW2.Helpers;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;
using AW2.Net;

namespace AW2.Menu
{
    /// <summary>
    /// All possible item collections for <see cref="MainMenuComponent"/>.
    /// </summary>
    public class MainMenuItemCollections
    {
        /// <summary>
        /// IP address of server to connect. Used by <see cref="NetworkItems"/>.
        /// </summary>
        private EditableText _connectAddress;

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
                    component.SetItems(NetworkItems);
                    AssaultWing.Instance.SoundEngine.PlaySound("MenuChangeItem");
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
                Action = component => AssaultWing.Instance.Exit()
            });

            NetworkItems = new MainMenuItemCollection("Battlefront Menu");
            NetworkItems.Add(new MainMenuItem(menuEngine)
            {
                Name = "Play as Server",
                Action = component =>
                {
                    if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone) return;
                    if (!AssaultWing.Instance.StartServer(MessageHandlers.IncomingConnectionHandlerOnServer)) return;
                    component.MenuEngine.ActivateComponent(MenuComponentType.Equip);

                    // HACK: Force one local player and Amazonas as the only arena.
                    AssaultWing.Instance.DataEngine.Spectators.Remove(player => AssaultWing.Instance.DataEngine.Spectators.Count > 1);
                    AssaultWing.Instance.DataEngine.ArenaPlaylist = new AW2.Helpers.Collections.Playlist(new string[] { "Amazonas" });
                }
            });

            _connectAddress = new EditableText(AssaultWing.Instance.Settings.Net.ConnectAddress, 15, EditableText.Keysets.IPAddressSet);
            NetworkItems.Add(new MainMenuTextField(menuEngine, _connectAddress)
            {
                Name = "Connect to ",
                Action = component =>
                {
                    if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone) return;
                    AssaultWing.Instance.Settings.Net.ConnectAddress = _connectAddress.Content;
                    AssaultWing.Instance.SoundEngine.PlaySound("MenuChangeItem");
                    AssaultWing.Instance.StartClient(_connectAddress.Content, result => ClientConnectedCallback(result, component));
                }
            });
        }

        private static void ClientConnectedCallback(Result<Connection> result, MainMenuComponent component)
        {
            if (!result.Successful)
            {
                Log.Write("Failed to connect to server: " + result.Error);
                AssaultWing.Instance.StopClient();
                return;
            }
            Log.Write("Client connected to " + result.Value.RemoteEndPoint);

            var net = AssaultWing.Instance.NetworkEngine;
            MessageHandlers.ActivateHandlers(MessageHandlers.GetClientMenuHandlers());

            // HACK: Force one local player.
            AssaultWing.Instance.DataEngine.Spectators.Remove(player => AssaultWing.Instance.DataEngine.Spectators.Count > 1);

            net.GameServerConnection.Send(new JoinGameRequest());
            component.MenuEngine.ActivateComponent(MenuComponentType.Equip);
        }
    }
}
