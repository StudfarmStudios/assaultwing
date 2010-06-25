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
    /// The known choices of content for <see cref="MainMenuComponent"/>.
    /// </summary>
    public class MainMenuContents
    {
        /// <summary>
        /// IP address of server to connect. Used by <see cref="NetworkContent"/>.
        /// </summary>
        private EditableText _connectAddress;

        /// <summary>
        /// The very first menu when the game starts.
        /// </summary>
        public MainMenuItemCollection StartContent { get; private set; }

        /// <summary>
        /// Menu for establishing a network game.
        /// </summary>
        public MainMenuItemCollection NetworkContent { get; private set; }

        public MainMenuContents(MenuEngineImpl menuEngine)
        {
            StartContent = new MainMenuItemCollection("Start Menu");
            StartContent.Add(new MainMenuItem(menuEngine)
            {
                Name = "Play Local",
                Action = component => component.MenuEngine.ActivateComponent(MenuComponentType.Equip)
            });
            StartContent.Add(new MainMenuItem(menuEngine)
            {
                Name = "Play at the Battlefront",
                Action = component =>
                {
                    component.SetContent(NetworkContent);
                    AssaultWing.Instance.SoundEngine.PlaySound("MenuChangeItem");
                }
            });
            StartContent.Add(new MainMenuItem(menuEngine)
            {
                Name = "Setup",
                Action = component => Log.Write("NOTE: Main menu item 'Setup' is not implemented")
            });
            StartContent.Add(new MainMenuItem(menuEngine)
            {
                Name = "Quit",
                Action = component => AssaultWing.Instance.Exit()
            });

            NetworkContent = new MainMenuItemCollection("Battlefront Menu");
            NetworkContent.Add(new MainMenuItem(menuEngine)
            {
                Name = "Play as Server",
                Action = component =>
                {
                    if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone) return;
                    if (!AssaultWing.Instance.StartServer(IncomingClientConnectionHandler)) return;
                    component.MenuEngine.ActivateComponent(MenuComponentType.Equip);

                    // HACK: Force one local player and Amazonas as the only arena.
                    AssaultWing.Instance.DataEngine.Spectators.Remove(player => AssaultWing.Instance.DataEngine.Spectators.Count > 1);
                    AssaultWing.Instance.DataEngine.ArenaPlaylist = new AW2.Helpers.Collections.Playlist(new string[] { "Amazonas" });
                }
            });

            _connectAddress = new EditableText(AssaultWing.Instance.Settings.Net.ConnectAddress, 15, EditableText.Keysets.IPAddressSet);
            NetworkContent.Add(new MainMenuTextField(menuEngine, _connectAddress)
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

        private static void IncomingClientConnectionHandler(Result<Connection> result)
        {
            if (!result.Successful)
                Log.Write("Some client failed to connect: " + result.Error);
            else
                Log.Write("Server obtained connection from " + result.Value.RemoteEndPoint);
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
