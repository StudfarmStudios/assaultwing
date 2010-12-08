﻿using AW2.Core;
using AW2.Core.OverlayDialogs;
using AW2.Helpers;
using AW2.Net.ManagementMessages;
using AW2.Net.MessageHandling;
using AW2.Net.Messages;
using AW2.UI;

namespace AW2.Menu
{
    /// <summary>
    /// All possible item collections for <see cref="MainMenuComponent"/>.
    /// </summary>
    public class MainMenuItemCollections
    {
        private MenuEngineImpl _menuEngine;

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
            _menuEngine = menuEngine;
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
                    InitializeNetworkItems();
                    component.SetItems(NetworkItems);
                    menuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                    menuEngine.Game.NetworkEngine.ConnectToManagementServer();
                    MessageHandlers.ActivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(
                        HandleGameServerListReply,
                        HandleJoinGameServerReply));
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

        private void InitializeNetworkItems()
        {
            NetworkItems = new MainMenuItemCollection("Battlefront Menu");
            NetworkItems.Add(new MainMenuItem(_menuEngine)
            {
                Name = "Play as Server",
                Action = component =>
                {
                    if (_menuEngine.Game.NetworkMode != NetworkMode.Standalone) return;
                    if (!_menuEngine.Game.StartServer(result => MessageHandlers.IncomingConnectionHandlerOnServer(result, () => true))) return;
                    component.MenuEngine.ActivateComponent(MenuComponentType.Equip);

                    // HACK: Force one local player
                    _menuEngine.Game.DataEngine.Spectators.Remove(player => _menuEngine.Game.DataEngine.Spectators.Count > 1);
                }
            });
        }

        private void HandleGameServerListReply(GameServerListReply mess)
        {
            foreach (var server in mess.GameServers)
                NetworkItems.Add(new MainMenuItem(_menuEngine)
                {
                    Name = string.Format("Connect to {0} [{1}/{2}]", server.Name, server.CurrentPlayers, server.MaxPlayers),
                    Action = component =>
                    {
                        if (_menuEngine.Game.NetworkMode != NetworkMode.Standalone) return;
                        var joinRequest = new JoinGameServerRequest
                        {
                            GameServerManagementID = server.ManagementID,
                            PrivateUDPEndPoint = _menuEngine.Game.NetworkEngine.UDPSocket.PrivateLocalEndPoint,
                        };
                        _menuEngine.Game.NetworkEngine.ManagementServerConnection.Send(joinRequest);
                    }
                });
        }

        private void HandleJoinGameServerReply(JoinGameServerReply mess)
        {
            MessageHandlers.DeactivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(null, null));
            _menuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
            _menuEngine.Game.StartClient(mess.GameServerEndPoints, ClientConnectedCallback);
        }

        private void ClientConnectedCallback(AW2.Net.Result<AW2.Net.Connections.Connection> result)
        {
            if (!result.Successful)
            {
                Log.Write("Failed to connect to server: " + result.Error);
                _menuEngine.Game.StopClient("Failed to connect to server");
                return;
            }
            MessageHandlers.ActivateHandlers(MessageHandlers.GetClientMenuHandlers(
                () => _menuEngine.ActivateComponent(MenuComponentType.Equip),
                HandleStartGameMessage, _menuEngine.Game.HandleConnectionClosingMessage));

            // HACK: Force one local player.
            _menuEngine.Game.DataEngine.Spectators.Remove(player => _menuEngine.Game.DataEngine.Spectators.Count > 1);

            var joinRequest = new JoinGameRequest { CanonicalStrings = CanonicalString.CanonicalForms };
            _menuEngine.Game.NetworkEngine.GameServerConnection.Send(joinRequest);
        }

        private void HandleStartGameMessage(StartGameMessage mess)
        {
            var game = _menuEngine.Game;
            game.SelectedArenaName = mess.ArenaToPlay;
            _menuEngine.ProgressBarAction(game.PrepareArena, () =>
            {
                MessageHandlers.ActivateHandlers(MessageHandlers.GetClientGameplayHandlers(game.HandleConnectionClosingMessage, game.HandleGobCreationMessage));
                game.IsClientAllowedToStartArena = true;
            });
        }
    }
}
