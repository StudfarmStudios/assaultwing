using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string INCOMPATIBLE_SERVERS_FOUND = "Some incompatible servers";
        private static readonly TimeSpan GAME_SERVER_LIST_REPLY_TIMEOUT = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan GAME_SERVER_LIST_REQUEST_INTERVAL = TimeSpan.FromSeconds(15); // must be larger than GAME_SERVER_LIST_REPLY_TIMEOUT

        private MainMenuComponent _menuComponent;
        private MenuEngineImpl MenuEngine { get { return _menuComponent.MenuEngine; } }
        private TimeSpan _lastNetworkItemsUpdate;
        private TimeSpan? _gameServerListReplyDeadline;

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

        public MainMenuItemCollections(MainMenuComponent menuComponent)
        {
            _menuComponent = menuComponent;
            InitializeStartItems();
            NetworkItems = new MainMenuItemCollection("Play at the Battlefront");
            NetworkItems.Update = () =>
            {
                CheckGameServerListReplyTimeout();
                RefreshNetworkItems();
            };
            SetupItems = new MainMenuItemCollection("General Setup");
            RefreshSetupItems(MenuEngine);
        }

        private void InitializeStartItems()
        {
            StartItems = new MainMenuItemCollection("Start Menu");
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Play Local",
                component =>
                {
                    component.MenuEngine.Activate(MenuComponentType.Equip);
                    MenuEngine.Game.InitializePlayers(2);
                }));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Play at the Battlefront",
                component =>
                {
                    MenuEngine.Game.InitializePlayers(1);
                    if (!TryConnectToManagementServer()) return;
                    MenuEngine.Game.WebData.RequestData();
                    RefreshNetworkItems(force: true);
                    component.SetItems(NetworkItems);
                    MenuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                    MessageHandlers.ActivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(HandleGameServerListReply));
                }));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Setup",
                component =>
                {
                    component.SetItems(SetupItems);
                    MenuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                }));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Read Instructions Online",
                component => MenuEngine.Game.OpenURL("http://www.assaultwing.com/quickinstructions")));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Quit",
                component =>
                {
                    AssaultWingProgram.Instance.Exit();
                    MenuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
                }));
        }

        /// <summary>
        /// Returns true on success.
        /// </summary>
        private bool TryConnectToManagementServer()
        {
            try
            {
                MenuEngine.Game.NetworkEngine.ConnectToManagementServer();
                return true;
            }
            catch (ArgumentException e)
            {
                var infoText = e.Message.Replace(" '", "\n'"); // TODO: Generic line wrapping in the dialog
                MenuEngine.Game.ShowInfoDialog(infoText);
                MenuEngine.Game.ShowMainMenuAndResetGameplay();
                return false;
            }
        }

        private void RefreshSetupItems(MenuEngineImpl menuEngine)
        {
            SetupItems.Clear();
            SetupItems.Add(GetSetupItemBase(menuEngine, () => "Reset all settings to defaults",
                component => MenuEngine.Game.ShowDialog(new CustomOverlayDialogData(MenuEngine.Game,
                    "Are you sure to reset all settings\nto their defaults? (Yes/No)",
                    new TriggeredCallback(TriggeredCallback.YES_CONTROL, menuEngine.Game.Settings.Reset),
                    new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { })))));
            SetupItems.Add(GetSetupItemBase(menuEngine, () => "Audio setup",
                component => component.SetItems(GetAudioItems(menuEngine))));
            SetupItems.Add(GetSetupItemBase(menuEngine, () => "Graphics setup",
                component => component.SetItems(GetGraphicsItems(menuEngine))));
            SetupItems.Add(GetSetupItemBase(menuEngine, () => "Controls setup",
                component => component.SetItems(GetControlsItems(menuEngine))));
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

        private void RefreshNetworkItems(bool force = false)
        {
            if (!force && _lastNetworkItemsUpdate + GAME_SERVER_LIST_REQUEST_INTERVAL > MenuEngine.Game.GameTime.TotalRealTime) return;
            _lastNetworkItemsUpdate = MenuEngine.Game.GameTime.TotalRealTime;
            NetworkItems.Clear();
            NetworkItems.Add(new MainMenuItem(MenuEngine, () => NO_SERVERS_FOUND, component => { }));
            NetworkItems.Add(new MainMenuItem(MenuEngine, () => "Find More in Forums", component => MenuEngine.Game.OpenURL("http://www.assaultwing.com/letsplay")));
            NetworkItems.Add(new MainMenuItem(MenuEngine, () => "Create a Server",
                component =>
                {
                    if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone) return;
                    if (!MenuEngine.Game.StartServer()) return;
                    component.MenuEngine.Activate(MenuComponentType.Equip);
                }));
            RequestGameServerList();
        }

        private void RequestGameServerList()
        {
            MenuEngine.Game.NetworkEngine.ManagementServerConnection.Send(new GameServerListRequest());
            _gameServerListReplyDeadline = MenuEngine.Game.GameTime.TotalRealTime + GAME_SERVER_LIST_REPLY_TIMEOUT;
        }

        private void CheckGameServerListReplyTimeout()
        {
            if (!_gameServerListReplyDeadline.HasValue) return;
            if (MenuEngine.Game.GameTime.TotalRealTime <= _gameServerListReplyDeadline.Value) return;
            MenuEngine.Game.ShowInfoDialog(
@"No reply from management server.
Cannot refresh game server list.
Either your firewall blocks the
traffic or the server is down.");
            _menuComponent.SetItems(StartItems);
            _gameServerListReplyDeadline = null;
        }

        private void HandleGameServerListReply(GameServerListReply mess)
        {
            _gameServerListReplyDeadline = null;
            foreach (var server in mess.GameServers)
            {
                if (NetworkItems[0].Name() == NO_SERVERS_FOUND) NetworkItems.RemoveAt(0);
                if (server.AWVersion.IsCompatibleWith(MenuEngine.Game.Version))
                {
                    var shortServerName = server.Name.Substring(0, Math.Min(12, server.Name.Length));
                    var menuItemText = string.Format("Join {0}\t\x10[{1}/{2}]", shortServerName, server.CurrentPlayers, server.MaxPlayers);
                    var joinRequest = new JoinGameServerRequest { GameServerManagementID = server.ManagementID };
                    NetworkItems.Insert(0, new MainMenuItem(MenuEngine,
                        () => menuItemText,
                        component =>
                        {
                            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone) return;
                            MenuEngine.Game.NetworkEngine.ManagementServerConnection.Send(joinRequest);
                        }));
                }
                else
                {
                    if (!NetworkItems.Any(item => item.Name() == INCOMPATIBLE_SERVERS_FOUND))
                        NetworkItems.Insert(Math.Max(0, NetworkItems.Count - 2), new MainMenuItem(MenuEngine, () => INCOMPATIBLE_SERVERS_FOUND, component => { }));
                }
            }
        }

        private MainMenuItemCollection GetControlsItems(MenuEngineImpl menuEngine)
        {
            var items = new MainMenuItemCollection("Controls Setup");
            items.Add(GetSetupItemBase(menuEngine, () => "Player 1 controls...",
                component => component.SetItems(GetPlayerControlsItems(menuEngine, menuEngine.Game.Settings.Controls.Player1))));
            items.Add(GetSetupItemBase(menuEngine, () => "Player 2 controls...",
                component => component.SetItems(GetPlayerControlsItems(menuEngine, menuEngine.Game.Settings.Controls.Player2))));
            items.Add(GetControlsItem(menuEngine, "Chat key", () => menuEngine.Game.Settings.Controls.Chat, ctrl => menuEngine.Game.Settings.Controls.Chat = ctrl));
            return items;
        }

        private MainMenuItemCollection GetPlayerControlsItems(MenuEngineImpl menuEngine, PlayerControlsSettings controls)
        {
            var items = new MainMenuItemCollection("Controls Setup");
            items.Add(GetControlsItem(menuEngine, "Thrust/Up", () => controls.Thrust, ctrl => controls.Thrust = ctrl));
            items.Add(GetControlsItem(menuEngine, "Left", () => controls.Left, ctrl => controls.Left = ctrl));
            items.Add(GetControlsItem(menuEngine, "Right", () => controls.Right, ctrl => controls.Right = ctrl));
            items.Add(GetControlsItem(menuEngine, "Ship modification", () => controls.Extra, ctrl => controls.Down = controls.Extra = ctrl));
            items.Add(GetControlsItem(menuEngine, "Primary weapon", () => controls.Fire1, ctrl => controls.Fire1 = ctrl));
            items.Add(GetControlsItem(menuEngine, "Extra weapon", () => controls.Fire2, ctrl => controls.Fire2 = ctrl));
            return items;
        }

        private MainMenuItem GetControlsItem(MenuEngineImpl menuEngine, string name, Func<IControlType> get, Action<IControlType> set)
        {
            return new MainMenuItem(menuEngine, () => string.Format("{0}\t\xf{1}", name, get()),
                component => menuEngine.Game.ShowDialog(
                    new KeypressOverlayDialogData(menuEngine.Game, "Hit key for " + name,
                        key => set(new KeyControlType(key)))));
        }

        private MainMenuItemCollection GetAudioItems(MenuEngineImpl menuEngine)
        {
            var items = new MainMenuItemCollection("Audio Setup");
            Func<string, Func<float>, Action<float>, MainMenuItem> getVolumeSetupItem = (name, get, set) => GetSetupItem(menuEngine,
                () => string.Format("{0}\t\xf{1:0} %", name, get() * 100),
                Enumerable.Range(0, 21).Select(x => x * 0.05f),
                get, set);
            items.Add(getVolumeSetupItem("Music volume",
                () => menuEngine.Game.Settings.Sound.MusicVolume,
                volume => menuEngine.Game.Settings.Sound.MusicVolume = volume));
            items.Add(getVolumeSetupItem("Sound volume",
                () => menuEngine.Game.Settings.Sound.SoundVolume,
                volume => menuEngine.Game.Settings.Sound.SoundVolume = volume));
            items.Add(GetSetupItem(menuEngine,
                () => string.Format("Audio engine\t\xf{0}", menuEngine.Game.Settings.Sound.AudioEngineType.ToString()),
                Enum.GetValues(typeof(SoundSettings.EngineType)).Cast<SoundSettings.EngineType>(),
                () => menuEngine.Game.Settings.Sound.AudioEngineType,
                audioEngine => menuEngine.Game.Settings.Sound.AudioEngineType = audioEngine));
            items.Add(new MainMenuItem(menuEngine, () => "Restart to change engine.", component => { }));
            return items;
        }

        private MainMenuItemCollection GetGraphicsItems(MenuEngineImpl menuEngine)
        {
            var items = new MainMenuItemCollection("Graphics Setup");
            Func<int> curWidth = () => menuEngine.Game.Settings.Graphics.FullscreenWidth;
            Func<int> curHeight = () => menuEngine.Game.Settings.Graphics.FullscreenHeight;
            items.Add(GetSetupItem(menuEngine,
                () => string.Format("Fullscreen\t\xc{0}", menuEngine.Game.Settings.Graphics.InGameFullscreen ? "Enabled" : "Disabled"),
                new[] { false, true },
                () => menuEngine.Game.Settings.Graphics.InGameFullscreen,
                inGameFullscreen => menuEngine.Game.Settings.Graphics.InGameFullscreen = inGameFullscreen));
            items.Add(GetSetupItem(menuEngine,
                () => string.Format("Resolution\t\xc{0}x{1}", curWidth(), curHeight()),
                GraphicsSettings.GetDisplayModes(),
                () => Tuple.Create(curWidth(), curHeight()),
                size =>
                {
                    menuEngine.Game.Settings.Graphics.FullscreenWidth = size.Item1;
                    menuEngine.Game.Settings.Graphics.FullscreenHeight = size.Item2;
                }));
            items.Add(GetSetupItem(menuEngine,
                () => string.Format("Vertical sync\t\xc{0}", menuEngine.Game.Settings.Graphics.IsVerticalSynced ? "Enabled" : "Disabled"),
                new[] { false, true },
                () => menuEngine.Game.Settings.Graphics.IsVerticalSynced,
                vsync => menuEngine.Game.Settings.Graphics.IsVerticalSynced = vsync));
            return items;
        }
    }
}
