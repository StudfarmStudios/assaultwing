using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Core.OverlayComponents;
using AW2.Helpers;
using AW2.Net.ManagementMessages;
using AW2.Net.MessageHandling;
using AW2.Settings;
using AW2.UI;

namespace AW2.Menu.Main
{
    /// <summary>
    /// All possible item collections for <see cref="MainMenuComponent"/>.
    /// </summary>
    public class MainMenuItemCollections
    {
        private const string NO_SERVERS_FOUND = "No servers found";

        private MenuEngineImpl _menuEngine;
        private TimeSpan _lastNetworkItemsUpdate;

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
            NetworkItems = new MainMenuItemCollection("Play at the Battlefront");
            NetworkItems.Update = () =>
            {
                if (_lastNetworkItemsUpdate.SecondsAgoRealTime() < 15) return;
                RefreshNetworkItems();
            };
            SetupItems = new MainMenuItemCollection("General Setup");
            RefreshSetupItems(menuEngine);
        }

        private void InitializeStartItems(MenuEngineImpl menuEngine)
        {
            StartItems = new MainMenuItemCollection("Start Menu");
            StartItems.Add(new MainMenuItem(menuEngine, () => "Play Local",
                component => component.MenuEngine.ActivateComponent(MenuComponentType.Equip)));
            StartItems.Add(new MainMenuItem(menuEngine, () => "Play at the Battlefront",
                component =>
                {
                    menuEngine.Game.NetworkEngine.ConnectToManagementServer();
                    RefreshNetworkItems();
                    component.SetItems(NetworkItems);
                    menuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                    MessageHandlers.ActivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(HandleGameServerListReply));
                }));
            StartItems.Add(new MainMenuItem(menuEngine, () => "Setup",
                component =>
                {
                    component.SetItems(SetupItems);
                    menuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                }));

            StartItems.Add(new MainMenuItem(menuEngine, () => "Quit",
                component =>
                {
                    AssaultWingProgram.Instance.Exit();
                    menuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                }));
        }

        private void RefreshSetupItems(MenuEngineImpl menuEngine)
        {
            SetupItems.Clear();
            SetupItems.Add(GetSetupItemBase(menuEngine, () => "Reset all settings to defaults",
                component => _menuEngine.Game.ShowDialog(new CustomOverlayDialogData(_menuEngine.Game,
                    "Are you sure to reset all settings\nto their defaults? (Yes/No)",
                    new TriggeredCallback(TriggeredCallback.YES_CONTROL, menuEngine.Game.Settings.Reset),
                    new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { })))));
            Func<string, Func<float>, Action<float>, MainMenuItem> getVolumeSetupItem = (name, get, set) => GetSetupItem(menuEngine,
                () => string.Format("{0} {1:0} %", name, get() * 100),
                Enumerable.Range(0, 21).Select(x => x * 0.05f),
                get, set);
            SetupItems.Add(getVolumeSetupItem("Music volume",
                () => menuEngine.Game.Settings.Sound.MusicVolume,
                volume => menuEngine.Game.Settings.Sound.MusicVolume = volume));
            SetupItems.Add(getVolumeSetupItem("Sound effect volume",
                () => menuEngine.Game.Settings.Sound.SoundVolume,
                volume => menuEngine.Game.Settings.Sound.SoundVolume = volume));
            Func<int> curWidth = () => menuEngine.Game.Settings.Graphics.FullscreenWidth;
            Func<int> curHeight = () => menuEngine.Game.Settings.Graphics.FullscreenHeight;
            SetupItems.Add(GetSetupItem(menuEngine,
                () => string.Format("Fullscreen resolution {0}x{1}", curWidth(), curHeight()),
                GraphicsSettings.GetDisplayModes(),
                () => Tuple.Create(curWidth(), curHeight()),
                size =>
                {
                    menuEngine.Game.Settings.Graphics.FullscreenWidth = size.Item1;
                    menuEngine.Game.Settings.Graphics.FullscreenHeight = size.Item2;
                }));
            SetupItems.Add(GetSetupItem(menuEngine,
                () => string.Format("Vertical sync {0}", menuEngine.Game.Settings.Graphics.IsVerticalSynced ? "Enabled" : "Disabled"),
                new[] { false, true },
                () => menuEngine.Game.Settings.Graphics.IsVerticalSynced,
                vsync => menuEngine.Game.Settings.Graphics.IsVerticalSynced = vsync));
            SetupItems.Add(GetSetupItemBase(menuEngine, () => "Player 1 controls...",
                component => component.SetItems(GetControlsItems(menuEngine, menuEngine.Game.Settings.Controls.Player1))));
            SetupItems.Add(GetSetupItemBase(menuEngine, () => "Player 2 controls...",
                component => component.SetItems(GetControlsItems(menuEngine, menuEngine.Game.Settings.Controls.Player2))));
        }

        private MainMenuItem GetSetupItemBase(MenuEngineImpl menuEngine, Func<string> getName, Action<MainMenuComponent> action, Action<MainMenuComponent> actionLeft = null)
        {
            Func<Action<MainMenuComponent>, Action<MainMenuComponent>> decorateAction = plainAction => component =>
            {
                plainAction(component);
                menuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
            };
            return new MainMenuItem(menuEngine, getName, decorateAction(action), decorateAction(actionLeft ?? (x => { })));
        }

        private MainMenuItem GetSetupItem<T>(MenuEngineImpl menuEngine, Func<string> getName, IEnumerable<T> items, Func<T> get, Action<T> set)
        {
            Action<IEnumerable<T>> chooseNext = orderedItems =>
            {
                var remainingItems = orderedItems.SkipWhile(x => !x.Equals(get())).Skip(1);
                set(remainingItems.Any() ? remainingItems.First() : orderedItems.Last());
            };
            return GetSetupItemBase(menuEngine, getName, component => chooseNext(items), component => chooseNext(items.Reverse()));
        }

        private void RefreshNetworkItems()
        {
            _lastNetworkItemsUpdate = _menuEngine.Game.GameTime.TotalRealTime;
            NetworkItems.Clear();
            NetworkItems.Add(new MainMenuItem(_menuEngine, () => NO_SERVERS_FOUND, component => { }));
            NetworkItems.Add(new MainMenuItem(_menuEngine, () => "Create a server",
                component =>
                {
                    if (_menuEngine.Game.NetworkMode != NetworkMode.Standalone) return;
                    if (!_menuEngine.Game.StartServer(result => MessageHandlers.IncomingConnectionHandlerOnServer(result, () => true))) return;
                    component.MenuEngine.ActivateComponent(MenuComponentType.Equip);

                    // HACK: Force one local player
                    _menuEngine.Game.DataEngine.Spectators.Remove(player => _menuEngine.Game.DataEngine.Spectators.Count > 1);
                }));
            _menuEngine.Game.NetworkEngine.ManagementServerConnection.Send(new GameServerListRequest());
        }

        private void HandleGameServerListReply(GameServerListReply mess)
        {
            foreach (var server in mess.GameServers)
            {
                if (NetworkItems[0].Name() == NO_SERVERS_FOUND) NetworkItems.RemoveAt(0);
                var menuItemText = string.Format("Connect to {0} [{1}/{2}]", server.Name, server.CurrentPlayers, server.MaxPlayers);
                var joinRequest = new JoinGameServerRequest { GameServerManagementID = server.ManagementID };
                NetworkItems.Insert(0, new MainMenuItem(_menuEngine,
                    () => menuItemText,
                    component =>
                    {
                        if (_menuEngine.Game.NetworkMode != NetworkMode.Standalone) return;
                        _menuEngine.Game.NetworkEngine.ManagementServerConnection.Send(joinRequest);
                    }));
            }
        }

        private MainMenuItemCollection GetControlsItems(MenuEngineImpl menuEngine, PlayerControlsSettings controls)
        {
            var items = new MainMenuItemCollection("Controls Setup");
            Func<string, Func<IControlType>, Action<IControlType>, MainMenuItem> getControlsItem = (name, get, set) =>
                new MainMenuItem(menuEngine, () => string.Format("{0}    {1}", name, get()),
                    component => menuEngine.Game.ShowDialog(
                        new KeypressOverlayDialogData(menuEngine.Game, "Hit key for " + name,
                            key => set(new KeyControlType(key)))));
            items.Add(getControlsItem("Thrust", () => controls.Thrust, ctrl => controls.Thrust = ctrl));
            items.Add(getControlsItem("Left", () => controls.Left, ctrl => controls.Left = ctrl));
            items.Add(getControlsItem("Right", () => controls.Right, ctrl => controls.Right = ctrl));
            items.Add(getControlsItem("Down", () => controls.Down, ctrl => controls.Down = ctrl));
            items.Add(getControlsItem("Fire1", () => controls.Fire1, ctrl => controls.Fire1 = ctrl));
            items.Add(getControlsItem("Fire2", () => controls.Fire2, ctrl => controls.Fire2 = ctrl));
            items.Add(getControlsItem("Extra", () => controls.Extra, ctrl => controls.Extra = ctrl));
            return items;
        }
    }
}
