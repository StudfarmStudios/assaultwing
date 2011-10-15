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
        private AssaultWing Game { get { return MenuEngine.Game; } }
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
            RefreshSetupItems();
        }

        private void InitializeStartItems()
        {
            StartItems = new MainMenuItemCollection("Start Menu");
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Play Local",
                () =>
                {
                    _menuComponent.MenuEngine.Activate(MenuComponentType.Equip);
                    Game.InitializePlayers(2);
                }));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Play at the Battlefront",
                () =>
                {
                    Game.InitializePlayers(1);
                    if (!TryConnectToManagementServer()) return;
                    Game.WebData.RequestData();
                    RefreshNetworkItems(force: true);
                    _menuComponent.SetItems(NetworkItems);
                    Game.SoundEngine.PlaySound("MenuChangeItem");
                    MessageHandlers.ActivateHandlers(MessageHandlers.GetStandaloneMenuHandlers(HandleGameServerListReply));
                }));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Setup",
                () =>
                {
                    _menuComponent.SetItems(SetupItems);
                    Game.SoundEngine.PlaySound("MenuChangeItem");
                }));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Read Instructions Online",
                () => Game.OpenURL("http://www.assaultwing.com/quickinstructions")));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Quit",
                () =>
                {
                    AssaultWingProgram.Instance.Exit();
                    Game.SoundEngine.PlaySound("MenuChangeItem");
                }));
        }

        /// <summary>
        /// Returns true on success.
        /// </summary>
        private bool TryConnectToManagementServer()
        {
            try
            {
                Game.NetworkEngine.ConnectToManagementServer();
                return true;
            }
            catch (ArgumentException e)
            {
                var infoText = e.Message.Replace(" '", "\n'"); // TODO: Generic line wrapping in the dialog
                Game.ShowInfoDialog(infoText);
                Game.ShowMainMenuAndResetGameplay();
                return false;
            }
        }

        private void RefreshSetupItems()
        {
            SetupItems.Clear();
            SetupItems.Add(GetSetupItemBase(() => "Reset all settings to defaults",
                () => Game.ShowDialog(new CustomOverlayDialogData(Game,
                    "Are you sure to reset all settings\nto their defaults? (Yes/No)",
                    new TriggeredCallback(TriggeredCallback.YES_CONTROL, Game.Settings.Reset),
                    new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { })))));
            SetupItems.Add(GetSetupItemBase(() => "Audio setup", () => _menuComponent.SetItems(GetAudioItems())));
            SetupItems.Add(GetSetupItemBase(() => "Graphics setup", () => _menuComponent.SetItems(GetGraphicsItems())));
            SetupItems.Add(GetSetupItemBase(() => "Controls setup", () => _menuComponent.SetItems(GetControlsItems())));
        }

        private MainMenuItem GetSetupItemBase(Func<string> getName, Action action, Action actionLeft = null)
        {
            Func<Action, Action> decorateAction = plainAction => () =>
            {
                plainAction();
                Game.SoundEngine.PlaySound("MenuChangeItem");
            };
            return new MainMenuItem(MenuEngine, getName, decorateAction(action), decorateAction(actionLeft ?? (() => { })));
        }

        private MainMenuItem GetSetupItem<T>(Func<string> getName, IEnumerable<T> items, Func<T> get, Action<T> set)
        {
            Action<IEnumerable<T>> chooseNext = orderedItems =>
            {
                var remainingItems = orderedItems.SkipWhile(x => !x.Equals(get())).Skip(1);
                set(remainingItems.Any() ? remainingItems.First() : orderedItems.Last());
            };
            return GetSetupItemBase(getName, () => chooseNext(items), () => chooseNext(items.Reverse()));
        }

        private void RefreshNetworkItems(bool force = false)
        {
            if (!force && _lastNetworkItemsUpdate + GAME_SERVER_LIST_REQUEST_INTERVAL > Game.GameTime.TotalRealTime) return;
            _lastNetworkItemsUpdate = Game.GameTime.TotalRealTime;
            NetworkItems.Clear();
            NetworkItems.Add(new MainMenuItem(MenuEngine, () => NO_SERVERS_FOUND, () => { }));
            NetworkItems.Add(new MainMenuItem(MenuEngine, () => "Find More in Forums", () => Game.OpenURL("http://www.assaultwing.com/letsplay")));
            NetworkItems.Add(new MainMenuItem(MenuEngine, () => "Create a Server",
                () =>
                {
                    if (Game.NetworkMode != NetworkMode.Standalone) return;
                    if (!Game.StartServer()) return;
                    _menuComponent.MenuEngine.Activate(MenuComponentType.Equip);
                }));
            RequestGameServerList();
        }

        private void RequestGameServerList()
        {
            Game.NetworkEngine.ManagementServerConnection.Send(new GameServerListRequest());
            _gameServerListReplyDeadline = Game.GameTime.TotalRealTime + GAME_SERVER_LIST_REPLY_TIMEOUT;
        }

        private void CheckGameServerListReplyTimeout()
        {
            if (!_gameServerListReplyDeadline.HasValue) return;
            if (Game.GameTime.TotalRealTime <= _gameServerListReplyDeadline.Value) return;
            Game.ShowInfoDialog(
@"No reply from management server.
Cannot refresh game server list.
Either your firewall blocks the
traffic or the server is down.");
            // FIXME !!! This condition is triggered ALSO when the client clicks to join a server but the server never
            // replies. Consider keeping on asking refreshes from the management server.
            Game.ShowMainMenuAndResetGameplay();
            _gameServerListReplyDeadline = null;
        }

        private void HandleGameServerListReply(GameServerListReply mess)
        {
            _gameServerListReplyDeadline = null;
            foreach (var server in mess.GameServers)
            {
                if (NetworkItems[0].Name() == NO_SERVERS_FOUND) NetworkItems.RemoveAt(0);
                if (server.AWVersion.IsCompatibleWith(Game.Version))
                {
                    var shortServerName = server.Name.Substring(0, Math.Min(12, server.Name.Length));
                    var menuItemText = string.Format("Join {0}\t\x10[{1}/{2}]", shortServerName, server.CurrentPlayers, server.MaxPlayers);
                    var joinRequest = new JoinGameServerRequest { GameServerManagementID = server.ManagementID };
                    NetworkItems.Insert(0, new MainMenuItem(MenuEngine,
                        () => menuItemText,
                        () =>
                        {
                            if (Game.NetworkMode != NetworkMode.Standalone) return;
                            Game.NetworkEngine.ManagementServerConnection.Send(joinRequest);
                        }));
                }
                else
                {
                    if (!NetworkItems.Any(item => item.Name() == INCOMPATIBLE_SERVERS_FOUND))
                        NetworkItems.Insert(Math.Max(0, NetworkItems.Count - 2), new MainMenuItem(MenuEngine, () => INCOMPATIBLE_SERVERS_FOUND, () => { }));
                }
            }
        }

        private MainMenuItemCollection GetControlsItems()
        {
            var items = new MainMenuItemCollection("Controls Setup");
            items.Add(GetSetupItemBase(() => "Player 1 controls...",
                () => _menuComponent.SetItems(GetPlayerControlsItems(Game.Settings.Controls.Player1))));
            items.Add(GetSetupItemBase(() => "Player 2 controls...",
                () => _menuComponent.SetItems(GetPlayerControlsItems(Game.Settings.Controls.Player2))));
            items.Add(GetControlsItem("Chat key", () => Game.Settings.Controls.Chat, ctrl => Game.Settings.Controls.Chat = ctrl));
            return items;
        }

        private MainMenuItemCollection GetPlayerControlsItems(PlayerControlsSettings controls)
        {
            var items = new MainMenuItemCollection("Controls Setup");
            items.Add(GetControlsItem("Thrust/Up", () => controls.Thrust, ctrl => controls.Thrust = ctrl));
            items.Add(GetControlsItem("Left", () => controls.Left, ctrl => controls.Left = ctrl));
            items.Add(GetControlsItem("Right", () => controls.Right, ctrl => controls.Right = ctrl));
            items.Add(GetControlsItem("Ship modification", () => controls.Extra, ctrl => controls.Down = controls.Extra = ctrl));
            items.Add(GetControlsItem("Primary weapon", () => controls.Fire1, ctrl => controls.Fire1 = ctrl));
            items.Add(GetControlsItem("Extra weapon", () => controls.Fire2, ctrl => controls.Fire2 = ctrl));
            return items;
        }

        private MainMenuItem GetControlsItem(string name, Func<IControlType> get, Action<IControlType> set)
        {
            return new MainMenuItem(MenuEngine, () => string.Format("{0}\t\xf{1}", name, get()),
                () => Game.ShowDialog(
                    new KeypressOverlayDialogData(Game, "Hit key for " + name,
                        key => set(new KeyControlType(key)))));
        }

        private MainMenuItemCollection GetAudioItems()
        {
            var items = new MainMenuItemCollection("Audio Setup");
            Func<string, Func<float>, Action<float>, MainMenuItem> getVolumeSetupItem = (name, get, set) => GetSetupItem(
                () => string.Format("{0}\t\xf{1:0} %", name, get() * 100),
                Enumerable.Range(0, 21).Select(x => x * 0.05f),
                get, set);
            items.Add(getVolumeSetupItem("Music volume",
                () => Game.Settings.Sound.MusicVolume,
                volume => Game.Settings.Sound.MusicVolume = volume));
            items.Add(getVolumeSetupItem("Sound volume",
                () => Game.Settings.Sound.SoundVolume,
                volume => Game.Settings.Sound.SoundVolume = volume));
            items.Add(GetSetupItem(
                () => string.Format("Audio engine\t\xf{0}", Game.Settings.Sound.AudioEngineType.ToString()),
                Enum.GetValues(typeof(SoundSettings.EngineType)).Cast<SoundSettings.EngineType>(),
                () => Game.Settings.Sound.AudioEngineType,
                audioEngine => Game.Settings.Sound.AudioEngineType = audioEngine));
            items.Add(new MainMenuItem(MenuEngine, () => "Restart to change engine.", () => { }));
            return items;
        }

        private MainMenuItemCollection GetGraphicsItems()
        {
            var items = new MainMenuItemCollection("Graphics Setup");
            Func<int> curWidth = () => Game.Settings.Graphics.FullscreenWidth;
            Func<int> curHeight = () => Game.Settings.Graphics.FullscreenHeight;
            items.Add(GetSetupItem(
                () => string.Format("Fullscreen\t\xc{0}", Game.Settings.Graphics.InGameFullscreen ? "Enabled" : "Disabled"),
                new[] { false, true },
                () => Game.Settings.Graphics.InGameFullscreen,
                inGameFullscreen => Game.Settings.Graphics.InGameFullscreen = inGameFullscreen));
            items.Add(GetSetupItem(
                () => string.Format("Resolution\t\xc{0}x{1}", curWidth(), curHeight()),
                GraphicsSettings.GetDisplayModes(),
                () => Tuple.Create(curWidth(), curHeight()),
                size =>
                {
                    Game.Settings.Graphics.FullscreenWidth = size.Item1;
                    Game.Settings.Graphics.FullscreenHeight = size.Item2;
                }));
            items.Add(GetSetupItem(
                () => string.Format("Vertical sync\t\xc{0}", Game.Settings.Graphics.IsVerticalSynced ? "Enabled" : "Disabled"),
                new[] { false, true },
                () => Game.Settings.Graphics.IsVerticalSynced,
                vsync => Game.Settings.Graphics.IsVerticalSynced = vsync));
            return items;
        }
    }
}
