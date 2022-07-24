using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Core.OverlayComponents;
using AW2.Game;
using AW2.Helpers;
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
        private static readonly TimeSpan GAME_SERVER_LIST_REQUEST_INTERVAL = TimeSpan.FromSeconds(15);

        private MainMenuComponent _menuComponent;
        private MenuEngineImpl MenuEngine { get { return _menuComponent.MenuEngine; } }
        private AssaultWing<ClientEvent> Game { get { return MenuEngine.Game; } }
        private TimeSpan _lastNetworkItemsUpdate;
        private EditableText _loginName;
        private EditableText _loginPassword;

        /// <summary>
        /// The very first menu when the game starts.
        /// </summary>
        public MainMenuItemCollection StartItems { get; private set; }

        /// <summary>
        /// Menu for establishing a network game.
        /// </summary>
        public MainMenuItemCollection NetworkItems { get; private set; }

        /// <summary>
        /// Menu for registered pilots.
        /// </summary>
        public MainMenuItemCollection LoginItems { get; private set; }

        /// <summary>
        /// Menu for choosing general settings.
        /// </summary>
        public MainMenuItemCollection SetupItems { get; private set; }

        private string InitialLoginName
        {
            get
            {
                return Game.DataEngine.LocalPlayer != null
                    ? Game.DataEngine.LocalPlayer.Name
                    : Game.Settings.Players.Player1.Name;
            }
        }

        public MainMenuItemCollections(MainMenuComponent menuComponent)
        {
            _menuComponent = menuComponent;
            InitializeStartItems();
            NetworkItems = new MainMenuItemCollection("Battlefront");
            NetworkItems.Update = () =>
            {
                EnsureStandaloneMessageHandlersActivated();
                RefreshNetworkItems();
            };

            InitializeSetupItems();
        }

        public void Click_LocalGame()
        {
            MenuEngine.Activate(MenuComponentType.Equip);
            Game.InitializePlayers(2);
            Game.RefreshGameSettings();
        }

        public void Click_NetworkGame(bool loginPilots)
        {
            Game.InitializePlayers(1);
            
            RefreshNetworkItems(force: true);
            _menuComponent.PushItems(NetworkItems);
            Game.SoundEngine.PlaySound("menuChangeItem");
        }

        public void Click_ConnectToGameServer(string gameServerManagementID, string shortServerName)
        {
            // TODO: Peter: Steam network, connecting to selected server
            Game.ShowConnectingToGameServerDialog(shortServerName);
        }

        private void EnsureStandaloneMessageHandlersActivated()
        {
            if (Game.NetworkEngine.MessageHandlers.Any()) return; // FIXME: Oversimplified check; are the handlers the right ones?
            // TODO: Peter: Steam network, do we need something like the GetStandaloneMenuHandlers that was here
        }

        private void InitializeStartItems()
        {
            StartItems = new MainMenuItemCollection("");
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Play Local", Click_LocalGame));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Play at the Battlefront", () => Click_NetworkGame(loginPilots: true)));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "See Pilot Rankings Online",
                () => Game.OpenURL("http://www.assaultwing.com/battlefront")));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Read Instructions Online",
                () => Game.OpenURL("http://www.assaultwing.com/quickinstructions")));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Setup",
                () =>
                {
                    _menuComponent.PushItems(SetupItems);
                    Game.SoundEngine.PlaySound("menuChangeItem");
                }));
            StartItems.Add(new MainMenuItem(MenuEngine, () => "Quit",
                () =>
                {
                    AssaultWingProgram.Instance.Exit();
                    Game.SoundEngine.PlaySound("menuChangeItem");
                }));
        }

        private void InitializeSetupItems()
        {
            SetupItems = new MainMenuItemCollection("Setup");
            SetupItems.Add(GetSetupItemBase(() => "Reset All Settings to Defaults",
                () => Game.ShowCustomDialog("Are you sure to reset all settings\nto their defaults? (Yes/No)", null,
                    new TriggeredCallback(TriggeredCallback.YES_CONTROL, () => Game.Settings.Reset(Game)),
                    new TriggeredCallback(TriggeredCallback.NO_CONTROL, () => { }))));
            SetupItems.Add(GetSetupItemBase(() => "Audio Setup", () => _menuComponent.PushItems(GetAudioItems())));
            SetupItems.Add(GetSetupItemBase(() => "Graphics Setup", () => _menuComponent.PushItems(GetGraphicsItems())));
            SetupItems.Add(GetSetupItemBase(() => "Controls Setup", () => _menuComponent.PushItems(GetControlsItems())));
            SetupItems.Add(GetSetupItem(
                () => string.Format("Bots\t\xe{0}", Game.Settings.Players.BotsEnabled ? "Included" : "Excluded"),
                new[] { false, true },
                () => Game.Settings.Players.BotsEnabled,
                botsEnabled => Game.Settings.Players.BotsEnabled = botsEnabled));
        }

        private MainMenuItem GetSetupItemBase(Func<string> getName, Action action, Action actionLeft = null, Action actionRight = null)
        {
            Func<Action, Action> decorateAction = plainAction => () =>
            {
                plainAction();
                Game.SoundEngine.PlaySound("menuChangeItem");
            };
            return new MainMenuItem(MenuEngine, getName, decorateAction(action),
                decorateAction(actionLeft ?? (() => { })),
                decorateAction(actionRight ?? (() => { })));
        }

        private MainMenuItem GetSetupItem<T>(Func<string> getName, IEnumerable<T> items, Func<T> get, Action<T> set)
        {
            Action<IEnumerable<T>> chooseNext = orderedItems =>
            {
                var remainingItems = orderedItems.SkipWhile(x => !x.Equals(get())).Skip(1);
                set(remainingItems.Any() ? remainingItems.First() : orderedItems.Last());
            };
            return GetSetupItemBase(getName, () => { }, () => chooseNext(items.Reverse()), () => chooseNext(items));
        }

        private void RefreshNetworkItems(bool force = false)
        {
            if (!force && _lastNetworkItemsUpdate + GAME_SERVER_LIST_REQUEST_INTERVAL > Game.GameTime.TotalRealTime) return;
            _lastNetworkItemsUpdate = Game.GameTime.TotalRealTime;
            NetworkItems.Clear();
            NetworkItems.Add(new MainMenuItem(MenuEngine, () => "Log in with Your Pilot",
                () =>
                {
                    _loginName.Content = InitialLoginName;
                    _menuComponent.PushItems(LoginItems);
                }));
            NetworkItems.Add(new MainMenuItem(MenuEngine, () => NO_SERVERS_FOUND, () => { }));
            NetworkItems.Add(new MainMenuItem(MenuEngine, () => "Find More in Forums", () => Game.OpenURL("http://www.assaultwing.com/letsplay")));
            NetworkItems.Add(new MainMenuItem(MenuEngine, () => "Create a Server",
                () =>
                {
                    var error = Game.StartServer();
                    if (error != null)
                        Game.NetworkingErrors.Enqueue("Couldn't start game server.\n" + error + ".");
                    else
                        _menuComponent.MenuEngine.Activate(MenuComponentType.Equip);
                }));
            RequestGameServerList();
        }

        private void RequestGameServerList()
        {
            // TODO: Peter: Steam network, connecting to selected server
        }

        private void HandleGameServerListReply(object mess)
        {
            // TODO: Peter: Steam network, connecting to selected server            
        }

        private MainMenuItemCollection GetControlsItems()
        {
            var items = new MainMenuItemCollection("Controls");
            items.Add(GetSetupItemBase(() => "Player 1 Controls...",
                () => _menuComponent.PushItems(GetPlayerControlsItems("Player 1", Game.Settings.Controls.Player1))));
            items.Add(GetSetupItemBase(() => "Player 2 Controls...",
                () => _menuComponent.PushItems(GetPlayerControlsItems("Player 2", Game.Settings.Controls.Player2))));
            items.Add(GetControlsItem("Chat Key", () => Game.Settings.Controls.Chat, ctrl => Game.Settings.Controls.Chat = ctrl));
            return items;
        }

        private MainMenuItemCollection GetPlayerControlsItems(string collectionName, PlayerControlsSettings controls)
        {
            var items = new MainMenuItemCollection(collectionName);
            items.Add(GetSetupItemBase(() => "Preset Keyboard Right", () => controls.CopyFrom(ControlsSettings.PRESET_KEYBOARD_RIGHT)));
            items.Add(GetSetupItemBase(() => "Preset Keyboard Left", () => controls.CopyFrom(ControlsSettings.PRESET_KEYBOARD_LEFT)));
            items.Add(GetSetupItemBase(() => "Preset Game Pad 1", () => controls.CopyFrom(ControlsSettings.PRESET_GAMEPAD1)));
            items.Add(GetSetupItemBase(() => "Preset Game Pad 2", () => controls.CopyFrom(ControlsSettings.PRESET_GAMEPAD2)));
            items.Add(GetControlsItem("Thrust", () => controls.Thrust, ctrl => controls.Thrust = ctrl));
            items.Add(GetControlsItem("Left", () => controls.Left, ctrl => controls.Left = ctrl));
            items.Add(GetControlsItem("Right", () => controls.Right, ctrl => controls.Right = ctrl));
            items.Add(GetControlsItem("Ship Mod", () => controls.Extra, ctrl => controls.Extra = ctrl));
            items.Add(GetControlsItem("Weapon 1", () => controls.Fire1, ctrl => controls.Fire1 = ctrl));
            items.Add(GetControlsItem("Weapon 2", () => controls.Fire2, ctrl => controls.Fire2 = ctrl));
            return items;
        }

        private MainMenuItem GetControlsItem(string name, Func<IControlType> get, Action<IControlType> set)
        {
            return new MainMenuItem(MenuEngine, () => string.Format("{0}\t\x9{1}", name, get()),
                () => Game.ExternalProgramLogicEvent( new ClientEvent {
                    dialogData = new ControlSelectionOverlayDialogData(MenuEngine, "Press control for " + name,
                        control => set(control)) }));
        }

        private MainMenuItemCollection GetAudioItems()
        {
            var items = new MainMenuItemCollection("Audio");
            Func<string, Func<float>, Action<float>, MainMenuItem> getVolumeSetupItem = (name, get, set) => GetSetupItem(
                () => string.Format("{0}\t\xf{1:0} %", name, get() * 100),
                Enumerable.Range(0, 21).Select(x => x * 0.05f),
                get, set);
            items.Add(getVolumeSetupItem("Music Volume",
                () => Game.Settings.Sound.MusicVolume,
                volume => Game.Settings.Sound.MusicVolume = volume));
            items.Add(getVolumeSetupItem("Sound Volume",
                () => Game.Settings.Sound.SoundVolume,
                volume => Game.Settings.Sound.SoundVolume = volume));
            return items;
        }

        private MainMenuItemCollection GetGraphicsItems()
        {
            var items = new MainMenuItemCollection("Graphics");
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
                () => string.Format("Vertical Sync\t\xc{0}", Game.Settings.Graphics.IsVerticalSynced ? "Enabled" : "Disabled"),
                new[] { false, true },
                () => Game.Settings.Graphics.IsVerticalSynced,
                vsync => Game.Settings.Graphics.IsVerticalSynced = vsync));
            return items;
        }
    }
}
