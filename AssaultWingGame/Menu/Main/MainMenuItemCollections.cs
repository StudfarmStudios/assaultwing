using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Helpers;
using AW2.Net.ManagementMessages;
using AW2.Net.MessageHandling;

namespace AW2.Menu.Main
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

        /// <summary>
        /// Menu for choosing general settings.
        /// </summary>
        public MainMenuItemCollection SetupItems { get; private set; }

        public MainMenuItemCollections(MenuEngineImpl menuEngine)
        {
            _menuEngine = menuEngine;
            InitializeStartItems(menuEngine);
            var lastUpdate = TimeSpan.Zero;
            NetworkItems = new MainMenuItemCollection("Battlefront Menu");
            NetworkItems.Update = () =>
            {
                if (lastUpdate.SecondsAgoRealTime() < 15) return;
                lastUpdate = _menuEngine.Game.GameTime.TotalRealTime;
                RefreshNetworkItems();
            };
            SetupItems = new MainMenuItemCollection("Setup Menu");
            RefreshSetupItems(menuEngine);
        }

        private void InitializeStartItems(MenuEngineImpl menuEngine)
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
                    menuEngine.Game.NetworkEngine.ConnectToManagementServer();
                    RefreshNetworkItems();
                    component.SetItems(NetworkItems);
                    menuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                    MessageHandlers.ActivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(HandleGameServerListReply));
                }
            });
            StartItems.Add(new MainMenuItem(menuEngine)
            {
                Name = "Setup",
                Action = component => component.SetItems(SetupItems)
            });
            StartItems.Add(new MainMenuItem(menuEngine)
            {
                Name = "Quit",
                Action = component => AssaultWingProgram.Instance.Exit()
            });
        }

        private void RefreshSetupItems(MenuEngineImpl menuEngine)
        {
            SetupItems.Clear();
            Func<string, Action, MainMenuItem> getSetupItem = (name, action) => new MainMenuItem(menuEngine)
            {
                Name = name,
                Action = component =>
                {
                    action();
                    RefreshSetupItems(menuEngine);
                    menuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                }
            };
            Func<string, Func<float>, Action<float>, MainMenuItem> getVolumeSetupItem = (name, get, set) => getSetupItem(
                string.Format("{0} {1:0} %", name, get() * 100),
                () => set(get() >= 1 ? 0 : Math.Min(1, get() + 0.05f)));
            SetupItems.Add(getVolumeSetupItem(
                "Music volume",
                () => menuEngine.Game.Settings.Sound.MusicVolume,
                volume => menuEngine.Game.Settings.Sound.MusicVolume = volume));
            SetupItems.Add(getVolumeSetupItem(
                "Sound effect volume",
                () => menuEngine.Game.Settings.Sound.SoundVolume,
                volume => menuEngine.Game.Settings.Sound.SoundVolume = volume));
            Func<int> curWidth = () => menuEngine.Game.Settings.Graphics.FullscreenWidth;
            Func<int> curHeight = () => menuEngine.Game.Settings.Graphics.FullscreenHeight;
            SetupItems.Add(getSetupItem(
                string.Format("Fullscreen resolution {0}x{1}", curWidth(), curHeight()),
                () =>
                {
                    var modes = GetDisplayModes();
                    var newMode = modes.SkipWhile(m => m.Width != curWidth() || m.Height != curHeight()).Skip(1).FirstOrDefault()
                        ?? modes.First();
                    menuEngine.Game.Settings.Graphics.FullscreenWidth = newMode.Width;
                    menuEngine.Game.Settings.Graphics.FullscreenHeight = newMode.Height;
                }));
        }

        private static IEnumerable<DisplayMode> GetDisplayModes()
        {
            var goodAspectRatio = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.AspectRatio;
            return GraphicsAdapter.DefaultAdapter.SupportedDisplayModes[SurfaceFormat.Color]
                .Where(mode => mode.Height >= 600 && mode.Width >= 1024 && Math.Abs(goodAspectRatio - mode.AspectRatio) < 0.1);
        }

        private void RefreshNetworkItems()
        {
            NetworkItems.Clear();
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
            _menuEngine.Game.NetworkEngine.ManagementServerConnection.Send(new GameServerListRequest());
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
    }
}
