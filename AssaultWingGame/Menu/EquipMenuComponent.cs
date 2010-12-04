using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Net.Connections;
using AW2.Net.Messages;
using AW2.UI;
using AW2.Game.Gobs;

namespace AW2.Menu
{
    /// <summary>
    /// The equip menu component where players can choose their ships and weapons.
    /// </summary>
    /// The equip menu consists of four panes, one for each player.
    /// Each pane consists of a top that indicates the player and the main body where
    /// the menu content lies.
    /// Each pane, in its main mode, displays the player's selection of equipment.
    /// Each player can control their menu individually, and their current position
    /// in the menu main display is indicated by a cursor and a highlight.
    public class EquipMenuComponent : MenuComponent
    {
        /// <summary>
        /// An item in a pane main display in the equip menu.
        /// </summary>
        private enum EquipMenuItem { Name, Ship, Extra, Weapon2 }
        private enum EquipMenuTab { Equipment = 1, Players = 2, Chat = 3, GameSettings = 4 }
        private enum EquipMenuGameSettings { Type = 0, Arena = 1 }
        private EquipMenuTab _currentTab;
        private int _playerListIndex; // access through property PlayerListIndex
        private int _gameSettingsListIndex;
        private bool _playerNameChanged;
        private bool _readyPressed;
        private TimeSpan _listCursorFadeStartTime;
        private TimeSpan _tabFadeStartTime;
        private TimeSpan _readyFadeStartTime;

        private const int MAX_MENU_PANES = 4;

        private Control _controlBack, _controlActivate, _controlTab, _controlListUp, _controlListDown, _controlStartGame;
        private Vector2 _pos; // position of the component's background texture in menu system coordinates
        private SpriteFont _menuBigFont, _menuSmallFont;
        private Texture2D _backgroundTexture;
        private Texture2D _cursorMainTexture, _highlightMainTexture;
        private Texture2D _playerPaneTexture, _playerNameBackgroundTexture, _playerNameBorderTexture;
        private Texture2D _listHiliteTexture, _listCursorTexture, _listTextCursorTexture;
        private Texture2D _statusPaneTexture;
        private Texture2D _tabEquipmentTexture, _tabPlayersTexture, _tabGameSettingsTexture, _tabChatTexture, _tabHilite;
        private Texture2D _buttonReadyTexture, _buttonReadyHiliteTexture;

        /// <summary>
        /// Cursor fade curve as a function of time in seconds.
        /// Values range from 0 (transparent) to 255 (opaque).
        /// </summary>
        private Curve _cursorFade;
        private Curve _tabFade;
        private Curve _readyFade;
        private Curve _nameInfoMove;

        /// <summary>
        /// Time at which the cursor started fading for each player.
        /// </summary>
        private TimeSpan[] _cursorFadeStartTimes;

        /// <summary>
        /// Index of current item in each player's pane main display.
        /// </summary>
        private EquipMenuItem[] _currentItems;

        /// <summary>
        /// Equipment selectors for each player and each aspect of the player's equipment.
        /// Indexed as [playerI, aspectI].
        /// </summary>
        private EquipmentSelector[,] _equipmentSelectors;

        /// <summary>
        /// Text fields containing editable player names.
        /// </summary>
        private EditableText[] _playerNames;

        #region Private coordinate data

        private static readonly Vector2 PLAYER1_PANE_POS = new Vector2(334, 164);
        private static readonly Vector2 PLAYER_PANE_NAME_POS = new Vector2(104, 38);
        private static readonly Vector2 PLAYER_PANE_DELTA_POS = new Vector2(203, 0);
        private static readonly Vector2 PLAYER_PANE_ROW_DELTA_POS = new Vector2(0, 91);

        private Vector2 PlayerPaneMainDeltaPos { get { return new Vector2(0, _playerNameBackgroundTexture.Height); } }
        private Vector2 PlayerPaneCursorDeltaPos { get { return PlayerPaneMainDeltaPos + new Vector2(22, 3); } }
        private Vector2 PlayerPaneIconDeltaPos { get { return PlayerPaneMainDeltaPos + new Vector2(21, 1); } }

        private Vector2 GetPlayerPanePos(int playerI)
        {
            return _pos + PLAYER1_PANE_POS + playerI * PLAYER_PANE_DELTA_POS;
        }
        private Vector2 GetPlayerCursorPos(int playerI)
        {
            return GetPlayerPanePos(playerI) + PlayerPaneCursorDeltaPos + ((int)_currentItems[playerI] - 1) * PLAYER_PANE_ROW_DELTA_POS;
        }
        private Vector2 GetPlayerNamePos(int playerI, string name)
        {
            return GetPlayerPanePos(playerI) + new Vector2((int)(106 - _menuSmallFont.MeasureString(name).X / 2), 36);
        }
        private Vector2 GetPlayerItemNamePos(int playerI, string name)
        {
            return GetPlayerPanePos(playerI) + new Vector2((int)(104 - _menuSmallFont.MeasureString(name).X / 2), 355);
        }
        private Vector2 GetShipSelectorPos(int playerI)
        {
            return GetPlayerPanePos(playerI) + PlayerPaneCursorDeltaPos;
        }
        private Vector2 GetExtraDeviceSelectorPos(int playerI)
        {
            return GetPlayerPanePos(playerI) + PlayerPaneCursorDeltaPos + PLAYER_PANE_ROW_DELTA_POS;
        }
        private Vector2 GetWeapon2SelectorPos(int playerI)
        {
            return GetPlayerPanePos(playerI) + PlayerPaneCursorDeltaPos + 2 * PLAYER_PANE_ROW_DELTA_POS;
        }

        #endregion

        /// <summary>
        /// Does the menu component react to input.
        /// </summary>
        public override bool Active
        {
            set
            {
                base.Active = value;
                if (value)
                {
                    _readyPressed = false;
                    MenuEngine.IsProgressBarVisible = false;
                    MenuEngine.IsHelpTextVisible = true;
                    CreateSelectors();
                }
            }
        }

        /// <summary>
        /// The center of the menu component in menu system coordinates.
        /// </summary>
        /// This is a good place to center the menu view to when the menu component
        /// is to be seen well on the screen.
        public override Vector2 Center { get { return _pos + new Vector2(750, 460); } }

        private IEnumerable<Tuple<Player, int>> MenuPanePlayers
        {
            get
            {
                return MenuEngine.Game.DataEngine.Players
                    .Where(p => !p.IsRemote)
                    .Select((p, i) => Tuple.Create(p, i));
            }
        }

        private int PlayerListIndex
        {
            get
            {
                if (_playerListIndex >= MenuEngine.Game.DataEngine.Players.Count()) _playerListIndex = 0;
                if (_playerListIndex < 0) _playerListIndex = MenuEngine.Game.DataEngine.Players.Count() - 1;
                return _playerListIndex;
            }
            set { _playerListIndex = value; }
        }

        /// <summary>
        /// Creates an equip menu component for a menu system.
        /// </summary>
        /// <param name="menuEngine">The menu system.</param>
        public EquipMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            _readyPressed = false;
            _controlActivate = new KeyboardKey(Keys.Enter);
            _controlBack = new KeyboardKey(Keys.Escape);
            _controlTab = new KeyboardKey(Keys.Tab);
            _controlListUp = new KeyboardKey(Keys.Up);
            _controlListDown = new KeyboardKey(Keys.Down);
            _controlStartGame = new KeyboardKey(Keys.F10);
            _currentTab = EquipMenuTab.Equipment;
            PlayerListIndex = 0;
            _gameSettingsListIndex = 0;
            _listCursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            _playerNameChanged = false;
            _tabFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            _readyFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            _pos = new Vector2(0, 0);
            _currentItems = new EquipMenuItem[MAX_MENU_PANES];
            _cursorFadeStartTimes = new TimeSpan[MAX_MENU_PANES];
            _cursorFade = new Curve();
            _cursorFade.Keys.Add(new CurveKey(0, 255, 0, 0, CurveContinuity.Step));
            _cursorFade.Keys.Add(new CurveKey(0.5f, 0, 0, 0, CurveContinuity.Step));
            _cursorFade.Keys.Add(new CurveKey(1, 255, 0, 0, CurveContinuity.Step));
            _cursorFade.PreLoop = CurveLoopType.Cycle;
            _cursorFade.PostLoop = CurveLoopType.Cycle;
            _tabFade = new Curve();
            _tabFade.Keys.Add(new CurveKey(0f, 255));
            _tabFade.Keys.Add(new CurveKey(1f, 150));
            _tabFade.Keys.Add(new CurveKey(2.2f, 255));
            _tabFade.PreLoop = CurveLoopType.Cycle;
            _tabFade.PostLoop = CurveLoopType.Cycle;
            _readyFade = new Curve();
            _readyFade.Keys.Add(new CurveKey(0f, 240));
            _readyFade.Keys.Add(new CurveKey(1f, 30));
            _readyFade.Keys.Add(new CurveKey(2f, 240));
            _readyFade.PreLoop = CurveLoopType.Cycle;
            _readyFade.PostLoop = CurveLoopType.Cycle;
            _nameInfoMove = new Curve();
            _nameInfoMove.Keys.Add(new CurveKey(0f, 0));
            _nameInfoMove.Keys.Add(new CurveKey(0.6f, 15));
            _nameInfoMove.Keys.Add(new CurveKey(1.2f, 0));
            _nameInfoMove.PreLoop = CurveLoopType.Cycle;
            _nameInfoMove.PostLoop = CurveLoopType.Cycle;

            CreateSelectors();
        }

        public override void LoadContent()
        {
            var content = MenuEngine.Game.Content;
            _menuBigFont = content.Load<SpriteFont>("MenuFontBig");
            _menuSmallFont = content.Load<SpriteFont>("MenuFontSmall");
            _backgroundTexture = content.Load<Texture2D>("menu_equip_bg");
            _cursorMainTexture = content.Load<Texture2D>("menu_equip_cursor_large");
            _highlightMainTexture = content.Load<Texture2D>("menu_equip_hilite_large");
            _playerPaneTexture = content.Load<Texture2D>("menu_equip_player_bg");
            _playerNameBackgroundTexture = content.Load<Texture2D>("menu_equip_player_name_bg");
            _playerNameBorderTexture = content.Load<Texture2D>("menu_equip_player_name_border");
            _listHiliteTexture = content.Load<Texture2D>("menu_equip_player_name_hilite");
            _listCursorTexture = content.Load<Texture2D>("menu_equip_player_name_cursor");
            _listTextCursorTexture = content.Load<Texture2D>("menu_equip_player_name_textcursor");
            _statusPaneTexture = content.Load<Texture2D>("menu_equip_status_display");

            _tabEquipmentTexture = content.Load<Texture2D>("menu_equip_tab_equipment");
            _tabPlayersTexture = content.Load<Texture2D>("menu_equip_tab_players");
            _tabGameSettingsTexture = content.Load<Texture2D>("menu_equip_tab_gamesettings");
            _tabChatTexture = content.Load<Texture2D>("menu_equip_tab_chat");
            _tabHilite = content.Load<Texture2D>("menu_equip_tab_hilite");

            _buttonReadyTexture = content.Load<Texture2D>("menu_equip_btn_ready");
            _buttonReadyHiliteTexture = content.Load<Texture2D>("menu_equip_btn_ready_hilite");
        }

        private void ResetPlayerList()
        {
            PlayerListIndex = 0;
        }

        private void ResetEquipMenu()
        {
            ResetPlayerList();
            _currentTab = EquipMenuTab.Equipment;
        }

        public override void UnloadContent()
        {
            // The textures and fonts we reference will be disposed by GraphicsEngine.
        }

        public override void Update()
        {
            if (MenuPanePlayers.Count() != _playerNames.Count()) CreateSelectors();
            if (Active)
            {
                CheckGeneralControls();

                if (_currentTab == EquipMenuTab.Equipment)
                    CheckEquipTabPlayerControls();
                if (_currentTab == EquipMenuTab.Players)
                    CheckPlayersTabControls();
                if (_currentTab == EquipMenuTab.GameSettings)
                    CheckGameSettingsTabControls();
                switch (MenuEngine.Game.NetworkMode)
                {
                    case NetworkMode.Client:
                        SendPlayerSettingsToRemote(
                            p => !p.IsRemote && p.ServerRegistration != Spectator.ServerRegistrationType.Requested,
                            new Connection[] { MenuEngine.Game.NetworkEngine.GameServerConnection });
                        break;
                    case NetworkMode.Server:
                        SendPlayerSettingsToRemote(
                            p => true,
                            MenuEngine.Game.NetworkEngine.GameClientConnections);
                        SendGameSettingsToRemote(MenuEngine.Game.NetworkEngine.GameClientConnections);
                        break;
                }
            }
        }

        private void CreateSelectors()
        {
            if (MenuPanePlayers.Count() > MAX_MENU_PANES) throw new ApplicationException("Too many players want menu panes");
            int aspectCount = Enum.GetValues(typeof(EquipMenuItem)).Length;
            _equipmentSelectors = new EquipmentSelector[MenuPanePlayers.Count(), aspectCount];
            _playerNames = new EditableText[MenuPanePlayers.Count()];
            foreach (var indexedPlayer in MenuPanePlayers)
            {
                var player = indexedPlayer.Item1;
                int playerI = indexedPlayer.Item2;
                _currentItems[playerI] = EquipMenuItem.Ship;
                _playerNames[playerI] = new EditableText(player.Name, 20, EditableText.Keysets.PlayerNameSet);
                _equipmentSelectors[playerI, (int)EquipMenuItem.Ship] = new ShipSelector(MenuEngine.Game, player, GetShipSelectorPos(playerI));
                _equipmentSelectors[playerI, (int)EquipMenuItem.Extra] = new ExtraDeviceSelector(MenuEngine.Game, player, GetExtraDeviceSelectorPos(playerI));
                _equipmentSelectors[playerI, (int)EquipMenuItem.Weapon2] = new Weapon2Selector(MenuEngine.Game, player, GetWeapon2SelectorPos(playerI));
            }
        }

        private void CheckGeneralControls()
        {
            if (_controlTab.Pulse) ChangeTab();
            else if (_controlBack.Pulse) BackOutFromMenu();
            else if (_controlStartGame.Pulse) StartGame();
        }

        private void CheckGameSettingsTabControls()
        {
            if (_controlListDown.Pulse)
            {
                ++_gameSettingsListIndex;

                if (_gameSettingsListIndex > (int)EquipMenuGameSettings.Arena)
                    _gameSettingsListIndex = 0;

                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
                _listCursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;

                return;
            }
            if (_controlListUp.Pulse)
            {
                --_gameSettingsListIndex;

                if (_gameSettingsListIndex < 0)
                    _gameSettingsListIndex = (int)EquipMenuGameSettings.Arena;

                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
                _listCursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;

                return;
            }
            if (_controlActivate.Pulse && MenuEngine.Game.NetworkMode != NetworkMode.Client)
            {
                if (_gameSettingsListIndex == (int)EquipMenuGameSettings.Arena)
                {
                    MenuEngine.ActivateComponent(MenuComponentType.Arena);
                    return;
                }
            }
        }

        private void CheckPlayersTabControls()
        {
            if (_controlListDown.Pulse)
            {
                ++PlayerListIndex;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
                _listCursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            }
            if (_controlListUp.Pulse)
            {
                --PlayerListIndex;
                MenuEngine.Game.SoundEngine.PlaySound("MenuBrowseItem");
                _listCursorFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
            }
        }

        private void CheckEquipTabPlayerControls()
        {
            foreach (var indexedPlayer in MenuPanePlayers)
            {
                var player = indexedPlayer.Item1;
                int playerI = indexedPlayer.Item2;
                ConditionalPlayerAction(player.Controls.Thrust.Pulse, playerI, "MenuBrowseItem", () =>
                {
                    var minItem = MenuEngine.Game.NetworkMode == NetworkMode.Standalone
                        ? EquipMenuItem.Ship
                        : EquipMenuItem.Name;
                    if (_currentItems[playerI] > minItem)
                        --_currentItems[playerI];
                });
                ConditionalPlayerAction(player.Controls.Down.Pulse, playerI, "MenuBrowseItem", () =>
                {
                    if ((int)_currentItems[playerI] < Enum.GetValues(typeof(EquipMenuItem)).Length - 1)
                        ++_currentItems[playerI];
                });

                if (_currentItems[playerI] == EquipMenuItem.Name)
                {
                    _playerNames[playerI].Update(() =>
                    {
                        player.Name = _playerNames[playerI].Content.CapitalizeWords();
                        if (playerI == 0) _playerNameChanged = true;
                        else throw new ApplicationException("Unexpected player index " + playerI);
                    });
                }
                else
                {
                    int selectionChange = 0;
                    ConditionalPlayerAction(player.Controls.Left.Pulse,
                        playerI, "MenuChangeItem", () => selectionChange = -1);
                    ConditionalPlayerAction(player.Controls.Fire1.Pulse || player.Controls.Right.Pulse,
                        playerI, "MenuChangeItem", () => selectionChange = 1);
                    if (selectionChange != 0)
                        _equipmentSelectors[playerI, (int)_currentItems[playerI]].CurrentValue += selectionChange;
                }
            }
        }

        private void SendPlayerSettingsToRemote(Func<Player, bool> sendCriteria, IEnumerable<Connection> connections)
        {
            foreach (var player in MenuEngine.Game.DataEngine.Players.Where(sendCriteria))
            {
                var mess = new PlayerSettingsRequest
                {
                    IsRegisteredToServer = player.ServerRegistration == Spectator.ServerRegistrationType.Yes,
                    PlayerID = player.ID
                };
                mess.Write(player, SerializationModeFlags.ConstantData);
                if (player.ServerRegistration == Spectator.ServerRegistrationType.No)
                    player.ServerRegistration = Spectator.ServerRegistrationType.Requested;
                foreach (var conn in connections) conn.Send(mess);
            }
        }

        private void SendGameSettingsToRemote(IEnumerable<Connection> connections)
        {
            var mess = new GameSettingsRequest
            {
                ArenaPlaylist = MenuEngine.Game.DataEngine.ArenaPlaylist
            };
            foreach (var conn in connections) conn.Send(mess);
        }

        /// <summary>
        /// Helper for <seealso cref="CheckPlayerControls"/>
        /// </summary>
        private void ConditionalPlayerAction(bool condition, int playerI, string soundName, Action action)
        {
            if (!condition) return;
            _cursorFadeStartTimes[playerI] = MenuEngine.Game.GameTime.TotalRealTime;
            if (soundName != null) MenuEngine.Game.SoundEngine.PlaySound(soundName);
            action();
        }

        private void ChangeTab()
        {
            if (_currentTab == EquipMenuTab.GameSettings)
                _currentTab = EquipMenuTab.Equipment;
            else
            {
                ++_currentTab;

                // There is no chat in standalone mode
                if (MenuEngine.Game.NetworkMode == NetworkMode.Standalone && _currentTab == EquipMenuTab.Chat)
                    ++_currentTab;
            }
            // If someone drops of or whatever, set the playerListIndex to Zero for safety
            if (_currentTab == EquipMenuTab.Players) ResetPlayerList();
            MenuEngine.Game.SoundEngine.PlaySound("MenuChangeItem");
            _tabFadeStartTime = MenuEngine.Game.GameTime.TotalRealTime;
        }

        private void BackOutFromMenu()
        {
            ResetEquipMenu();
            MenuEngine.ActivateComponent(MenuComponentType.Main);
        }

        private void StartGame()
        {
            if (MenuEngine.Game.DataEngine.ArenaPlaylist.Count == 0) return;
            switch (MenuEngine.Game.NetworkMode)
            {
                case NetworkMode.Server:
                    ResetEquipMenu();
                    _readyPressed = true;
                    MenuEngine.ProgressBarAction(MenuEngine.Game.PrepareFirstArena, MenuEngine.Game.StartArenaOnServer);
                    MenuEngine.Deactivate();
                    break;
                case NetworkMode.Client:
                    // Client advances only when the server says so.
                    break;
                case NetworkMode.Standalone:
                    ResetEquipMenu();
                    _readyPressed = true;
                    MenuEngine.ProgressBarAction(MenuEngine.Game.PrepareFirstArena, MenuEngine.Game.StartArena);
                    MenuEngine.Deactivate();
                    break;
                default: throw new Exception("Unexpected network mode " + MenuEngine.Game.NetworkMode);
            }
        }

        #region Drawing methods

        /// <summary>
        /// Draws the menu component.
        /// </summary>
        /// <param name="view">Top left corner of the menu view in menu system coordinates.</param>
        /// <param name="spriteBatch">The sprite batch to use. <c>Begin</c> is assumed
        /// to have been called and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_backgroundTexture, _pos - view, Color.White);
            DrawTabsAndButtons(view, spriteBatch);
            DrawStatusDisplay(view, spriteBatch);
            switch (_currentTab)
            {
                case EquipMenuTab.Equipment:
                    if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone)
                        DrawLargeStatusBackground(view, spriteBatch);
                    DrawPlayerPanes(view, spriteBatch);
                    DrawShipInfoDisplay(view, spriteBatch);
                    DrawShipDeviceInfoDisplay(view, spriteBatch);
                    DrawWeaponInfoDisplay(view, spriteBatch);
                    DrawNameChangeInfo(view, spriteBatch);
                    break;
                case EquipMenuTab.Players:
                    DrawLargeStatusBackground(view, spriteBatch);
                    DrawPlayerListDisplay(view, spriteBatch);
                    DrawPlayerInfoDisplay(view, spriteBatch, MenuEngine.Game.DataEngine.Players.ElementAt(PlayerListIndex));
                    break;
                case EquipMenuTab.Chat:
                    DrawLargeStatusBackground(view, spriteBatch);
                    DrawChatTextInputBox(view, spriteBatch);
                    break;
                case EquipMenuTab.GameSettings:
                    DrawLargeStatusBackground(view, spriteBatch);
                    DrawGameSettingsList(view, spriteBatch);
                    switch ((EquipMenuGameSettings)_gameSettingsListIndex)
                    {
                        case EquipMenuGameSettings.Type:
                            DrawGameModeInfo(view, spriteBatch);
                            break;
                        case EquipMenuGameSettings.Arena:
                            DrawArenaInfo(view, spriteBatch);
                            break;
                        default: throw new ApplicationException("Unexpected EquipMenuGameSettings " + _gameSettingsListIndex);
                    }
                    break;
                default: throw new ApplicationException("Unexpected EquipMenuTab " + _currentTab);
            }
        }

        private void DrawChatTextInputBox(Vector2 view, SpriteBatch spriteBatch)
        {
            var background = MenuEngine.Game.Content.Load<Texture2D>("menu_equip_player_name_bg_empty");
            spriteBatch.Draw(background, GetPlayerPanePos(0) - view, Color.White);
        }

        private void DrawArenaInfo(Vector2 view, SpriteBatch spriteBatch)
        {
            Vector2 infoDisplayPos = _pos - view + new Vector2(595, 220);
            Vector2 currentPos = infoDisplayPos;
            Vector2 lineHeight = new Vector2(0, 20);
            Vector2 infoWidth = new Vector2(320, 0);
            string arenaName = MenuEngine.Game.DataEngine.ArenaPlaylist[0];
            ArenaInfo arenaInfo = MenuEngine.Game.DataEngine.ArenaInfos.FirstOrDefault(info => info.Name == arenaName);
            var content = MenuEngine.Game.Content;
            string previewName = content.Exists<Texture2D>(arenaInfo.PreviewName) ? arenaInfo.PreviewName : "no_preview";
            var previewTexture = content.Load<Texture2D>(previewName);

            spriteBatch.DrawString(_menuBigFont, "Arena info", currentPos, Color.White);
            currentPos += new Vector2(0, 50);
            spriteBatch.DrawString(_menuSmallFont, "Current arena:", currentPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, arenaName, currentPos + new Vector2(_menuSmallFont.MeasureString("Current arena:  ").X, 0), Color.GreenYellow);
            spriteBatch.DrawString(_menuSmallFont, "Arena list", currentPos + infoWidth + new Vector2(10, 0), Color.White);
            currentPos += lineHeight;
            spriteBatch.DrawString(_menuSmallFont, "Gametype settings don't\n" +
                                                   "contain a list of arenas\n" +
                                                   "so the game host can\n" +
                                                   "change the arena.", currentPos + infoWidth + new Vector2(10, 0), new Color(218, 159, 33));
            spriteBatch.Draw(previewTexture, currentPos, null, Color.White, 0,
                new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
        }

        private void DrawGameModeInfo(Vector2 view, SpriteBatch spriteBatch)
        {
            Vector2 infoDisplayPos = _pos - view + new Vector2(595, 220);
            Vector2 lineHeight = new Vector2(0, 20);
            Vector2 infoWidth = new Vector2(320, 0);
            Vector2 currentPos = infoDisplayPos;
            string arenaName = MenuEngine.Game.DataEngine.ArenaPlaylist[0];

            spriteBatch.DrawString(_menuBigFont, "Gametype Settings", currentPos, Color.White);
            currentPos += new Vector2(0, 50);

            spriteBatch.DrawString(_menuSmallFont, "Enemy", currentPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, "Everyone", currentPos + (infoWidth - new Vector2(_menuSmallFont.MeasureString("Everyone").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(_menuSmallFont, "Players", currentPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, "max 16", currentPos + (infoWidth - new Vector2(_menuSmallFont.MeasureString("max 16").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(_menuSmallFont, "Time limit", currentPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, "none", currentPos + (infoWidth - new Vector2(_menuSmallFont.MeasureString("none").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(_menuSmallFont, "Life limit", currentPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, "none", currentPos + (infoWidth - new Vector2(_menuSmallFont.MeasureString("none").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(_menuSmallFont, "Score limit", currentPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, "none", currentPos + (infoWidth - new Vector2(_menuSmallFont.MeasureString("none").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(_menuSmallFont, "Arena count", currentPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, "1", currentPos + (infoWidth - new Vector2(_menuSmallFont.MeasureString("1").X, 0)), Color.GreenYellow);
            currentPos += lineHeight;

            spriteBatch.DrawString(_menuSmallFont, "Arenas", currentPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, "Selectable <" + arenaName + ">", currentPos + (infoWidth - new Vector2(_menuSmallFont.MeasureString("Selectable <" + arenaName + ">").X, 0)), Color.GreenYellow);
            currentPos += new Vector2(0, 72);

            spriteBatch.DrawString(_menuSmallFont, "If you want to change these gametype\n" +
                                                   "settings, please create a pilot in\n" +
                                                   "assault wing website which will allow\n" +
                                                   "you to create your own gametype settings'.", currentPos, new Color(218, 159, 33));
        }

        private void DrawGameSettingsList(Vector2 view, SpriteBatch spriteBatch)
        {
            Vector2 listPos = _pos - view + new Vector2(360, 201);
            Vector2 currentPos = listPos;
            Vector2 lineHeight = new Vector2(0, 56);
            Vector2 cursorPos = currentPos + (lineHeight * _gameSettingsListIndex) + new Vector2(-27, -17);

            var background = MenuEngine.Game.Content.Load<Texture2D>("menu_equip_player_name_bg_empty");
            spriteBatch.Draw(background, GetPlayerPanePos(0) - view, Color.White);

            spriteBatch.DrawString(_menuSmallFont, EquipMenuGameSettings.Type.ToString(), currentPos, Color.GreenYellow);
            spriteBatch.DrawString(_menuSmallFont, "Mayhem", currentPos + new Vector2(0, 20), Color.White);
            currentPos += lineHeight;

            string arenaName = MenuEngine.Game.DataEngine.ArenaPlaylist[0];
            spriteBatch.DrawString(_menuSmallFont, EquipMenuGameSettings.Arena.ToString(), currentPos, Color.GreenYellow);
            spriteBatch.DrawString(_menuSmallFont, arenaName, currentPos + new Vector2(0, 20), Color.White);

            float cursorTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - _listCursorFadeStartTime).TotalSeconds;
            spriteBatch.Draw(_listCursorTexture, cursorPos, Color.White);
            spriteBatch.Draw(_listHiliteTexture, cursorPos, new Color(255, 255, 255, (byte)_cursorFade.Evaluate(cursorTime)));
        }

        private void DrawNameChangeInfo(Vector2 view, SpriteBatch spriteBatch)
        {
            if (_playerNameChanged || MenuEngine.Game.NetworkMode == NetworkMode.Standalone) return;
            var moveTime = (float)MenuEngine.Game.GameTime.TotalRealTime.TotalSeconds;
            var nameChangeInfoPos = _pos - view + new Vector2(250 + _nameInfoMove.Evaluate(moveTime), 180);
            var nameChangeInfoTexture = MenuEngine.Game.Content.Load<Texture2D>("menu_equip_player_name_changeinfo");
            spriteBatch.Draw(nameChangeInfoTexture, nameChangeInfoPos, MenuPanePlayers.ElementAt(0).Item1.PlayerColor);
        }

        private void DrawPlayerListDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            var playerListPos = _pos - view + new Vector2(360, 201);
            var currentPlayerPos = playerListPos;
            var lineHeight = new Vector2(0, 30);
            var cursorPos = playerListPos + (lineHeight * PlayerListIndex) + new Vector2(-27, -37);

            float cursorTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - _listCursorFadeStartTime).TotalSeconds;
            var playerNameEmptyTexture = MenuEngine.Game.Content.Load<Texture2D>("menu_equip_player_name_bg_empty");
            spriteBatch.Draw(playerNameEmptyTexture, GetPlayerPanePos(0) - view, Color.White);
            spriteBatch.Draw(_listCursorTexture, cursorPos, Color.White);
            spriteBatch.Draw(_listHiliteTexture, cursorPos, new Color(255, 255, 255, (byte)_cursorFade.Evaluate(cursorTime)));

            foreach (var plr in MenuEngine.Game.DataEngine.Players)
            {
                spriteBatch.DrawString(_menuSmallFont, plr.Name, currentPlayerPos, plr.PlayerColor);
                currentPlayerPos += lineHeight;
            }
        }

        private void DrawPlayerInfoDisplay(Vector2 view, SpriteBatch spriteBatch, Player player)
        {
            var weapon = (Weapon)MenuEngine.Game.DataEngine.GetTypeTemplate(player.Weapon2Name);
            var weaponInfo = weapon.DeviceInfo;
            var device = (ShipDevice)MenuEngine.Game.DataEngine.GetTypeTemplate(player.ExtraDeviceName);
            var deviceInfo = device.DeviceInfo;
            var ship = (Ship)MenuEngine.Game.DataEngine.GetTypeTemplate(player.ShipName);
            var shipInfo = ship.ShipInfo;
            var infoDisplayPos = _pos - view + new Vector2(570, 191);

            var shipPicture = MenuEngine.Game.Content.Load<Texture2D>(shipInfo.PictureName);
            var shipTitlePicture = MenuEngine.Game.Content.Load<Texture2D>(shipInfo.TitlePictureName);
            var weaponPicture = MenuEngine.Game.Content.Load<Texture2D>(weaponInfo.PictureName);
            var weaponTitlePicture = MenuEngine.Game.Content.Load<Texture2D>(weaponInfo.TitlePictureName);
            var devicePicture = MenuEngine.Game.Content.Load<Texture2D>(deviceInfo.PictureName);
            var deviceTitlePicture = MenuEngine.Game.Content.Load<Texture2D>(deviceInfo.TitlePictureName);

            spriteBatch.Draw(shipPicture, infoDisplayPos, null, Color.White, 0,
                new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
            spriteBatch.DrawString(_menuBigFont, "Ship", infoDisplayPos + new Vector2(149, 15), Color.White);
            spriteBatch.Draw(shipTitlePicture, infoDisplayPos + new Vector2(140, 37), Color.White);

            spriteBatch.Draw(devicePicture, infoDisplayPos + new Vector2(0, 120), null, Color.White, 0,
                new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
            spriteBatch.DrawString(_menuBigFont, "Ship Modification", infoDisplayPos + new Vector2(0, 120) + new Vector2(149, 15), Color.White);
            spriteBatch.Draw(deviceTitlePicture, infoDisplayPos + new Vector2(0, 120) + new Vector2(140, 37), Color.White);

            spriteBatch.Draw(weaponPicture, infoDisplayPos + new Vector2(0, 240), null, Color.White, 0,
                new Vector2(0, 0), 0.6f, SpriteEffects.None, 0);
            spriteBatch.DrawString(_menuBigFont, "Special Weapon", infoDisplayPos + new Vector2(0, 240) + new Vector2(149, 15), Color.White);
            spriteBatch.Draw(weaponTitlePicture, infoDisplayPos + new Vector2(0, 240) + new Vector2(140, 37), Color.White);
        }

        private void DrawWeaponInfoDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone &&
                MenuPanePlayers.ElementAt(0) != null &&
                _currentItems[MenuPanePlayers.ElementAt(0).Item2] == EquipMenuItem.Weapon2)
            {
                Player player = MenuPanePlayers.ElementAt(0).Item1;
                Weapon weapon = (Weapon)MenuEngine.Game.DataEngine.GetTypeTemplate(player.Weapon2Name);
                WeaponInfo info = weapon.WeaponInfo;
                Vector2 infoDisplayPos = _pos - view + new Vector2(560, 186);
                Vector2 infoDataPos = infoDisplayPos + new Vector2(200, 164);
                Vector2 infoDataValuePos = infoDataPos + new Vector2(350, 8);
                Vector2 infoDataValueLineHeight = new Vector2(0, 21);

                var weaponHeaders = MenuEngine.Game.Content.Load<Texture2D>("menu_equip_weaponinfo_headers");
                spriteBatch.Draw(weaponHeaders, infoDataPos, Color.White);

                spriteBatch.DrawString(_menuSmallFont, info.SingleShotDamage.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.SingleShotDamage.ToString()).X, 0), EquipInfo.GetColorForAmountType(info.SingleShotDamage));
                spriteBatch.DrawString(_menuSmallFont, info.ShotSpeed.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.ShotSpeed.ToString()).X, 0) + (infoDataValueLineHeight), EquipInfo.GetColorForAmountType(info.ShotSpeed));
                spriteBatch.DrawString(_menuSmallFont, info.RecoilMomentum.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.RecoilMomentum.ToString()).X, 0) + (infoDataValueLineHeight * 2), EquipInfo.GetReversedColorForAmountType(info.RecoilMomentum));
            }
        }

        private void DrawShipDeviceInfoDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone &&
                MenuPanePlayers.ElementAt(0) != null &&
                (_currentItems[MenuPanePlayers.ElementAt(0).Item2] == EquipMenuItem.Weapon2 ||
                _currentItems[MenuPanePlayers.ElementAt(0).Item2] == EquipMenuItem.Extra))
            {
                Player player = MenuPanePlayers.ElementAt(0).Item1;

                ShipDevice device = (ShipDevice)MenuEngine.Game.DataEngine.GetTypeTemplate(player.ExtraDeviceName);
                if (_currentItems[MenuPanePlayers.ElementAt(0).Item2] == EquipMenuItem.Weapon2) device = (ShipDevice)MenuEngine.Game.DataEngine.GetTypeTemplate(player.Weapon2Name);

                ShipDeviceInfo info = device.DeviceInfo;
                Vector2 infoDisplayPos = _pos - view + new Vector2(560, 186);
                Vector2 infoDataPos = infoDisplayPos + new Vector2(200, 100);
                Vector2 infoDataValuePos = infoDataPos + new Vector2(350, 8);
                Vector2 infoDataValueLineHeight = new Vector2(0, 21);
                Vector2 infoTextPos = infoDataPos + new Vector2(177, 194) - _menuSmallFont.MeasureString(info.InfoText) / 2;

                var devicePicture = MenuEngine.Game.Content.Load<Texture2D>(info.PictureName);
                var deviceTitlePicture = MenuEngine.Game.Content.Load<Texture2D>(info.TitlePictureName);
                var deviceHeaders = MenuEngine.Game.Content.Load<Texture2D>("menu_equip_deviceinfo_headers");

                spriteBatch.Draw(devicePicture, infoDisplayPos + new Vector2(-6, 0), Color.White);
                spriteBatch.Draw(deviceTitlePicture, infoDisplayPos + new Vector2(190, 18), Color.White);
                spriteBatch.Draw(deviceHeaders, infoDataPos, Color.White);

                spriteBatch.DrawString(_menuSmallFont, info.ReloadSpeed.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.ReloadSpeed.ToString()).X, 0), EquipInfo.GetColorForAmountType(info.ReloadSpeed));
                spriteBatch.DrawString(_menuSmallFont, info.EnergyUsage.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.EnergyUsage.ToString()).X, 0) + (infoDataValueLineHeight), EquipInfo.GetColorForAmountType(info.EnergyUsage));
                spriteBatch.DrawString(_menuSmallFont, info.UsageType.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.UsageType.ToString()).X, 0) + (infoDataValueLineHeight * 2), EquipInfo.GetColorForUsageType(info.UsageType));
                spriteBatch.DrawString(_menuSmallFont, info.InfoText, infoTextPos, new Color(218, 159, 33));
            }
        }


        private void DrawShipInfoDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone &&
                MenuPanePlayers.ElementAt(0) != null &&
                _currentItems[MenuPanePlayers.ElementAt(0).Item2] == EquipMenuItem.Ship)
            {
                Player player = MenuPanePlayers.ElementAt(0).Item1;
                Ship ship = (Ship)MenuEngine.Game.DataEngine.GetTypeTemplate(player.ShipName);
                ShipInfo info = ship.ShipInfo;
                Vector2 infoDisplayPos = _pos - view + new Vector2(560, 186);
                Vector2 infoDataPos = infoDisplayPos + new Vector2(200, 100);
                Vector2 infoDataValuePos = infoDataPos + new Vector2(350, 8);
                Vector2 infoDataValueLineHeight = new Vector2(0, 21);
                Vector2 infoTextPos = infoDataPos + new Vector2(177, 194) - _menuSmallFont.MeasureString(info.InfoText) / 2;

                var shipPicture = MenuEngine.Game.Content.Load<Texture2D>(info.PictureName);
                var shipTitlePicture = MenuEngine.Game.Content.Load<Texture2D>(info.TitlePictureName);
                var infoHeaders = MenuEngine.Game.Content.Load<Texture2D>("menu_equip_shipinfo_headers");

                spriteBatch.Draw(shipPicture, infoDisplayPos + new Vector2(-6, 0), Color.White);
                spriteBatch.Draw(shipTitlePicture, infoDisplayPos + new Vector2(190, 18), Color.White);
                spriteBatch.Draw(infoHeaders, infoDataPos, Color.White);

                spriteBatch.DrawString(_menuSmallFont, info.Hull.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.Hull.ToString()).X, 0), EquipInfo.GetColorForAmountType(info.Hull));
                spriteBatch.DrawString(_menuSmallFont, info.Armor.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.Armor.ToString()).X, 0) + (infoDataValueLineHeight), EquipInfo.GetColorForAmountType(info.Armor));
                spriteBatch.DrawString(_menuSmallFont, info.Speed.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.Speed.ToString()).X, 0) + (infoDataValueLineHeight * 2), EquipInfo.GetColorForAmountType(info.Speed));
                spriteBatch.DrawString(_menuSmallFont, info.Steering.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.Steering.ToString()).X, 0) + (infoDataValueLineHeight * 3), EquipInfo.GetColorForAmountType(info.Steering));
                spriteBatch.DrawString(_menuSmallFont, info.ModEnergy.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.ModEnergy.ToString()).X, 0) + (infoDataValueLineHeight * 4), EquipInfo.GetColorForAmountType(info.ModEnergy));
                spriteBatch.DrawString(_menuSmallFont, info.SpecialEnergy.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.SpecialEnergy.ToString()).X, 0) + (infoDataValueLineHeight * 5), EquipInfo.GetColorForAmountType(info.SpecialEnergy));
                spriteBatch.DrawString(_menuSmallFont, info.InfoText, infoTextPos, new Color(218, 159, 33));
            }
        }

        private void DrawTabsAndButtons(Vector2 view, SpriteBatch spriteBatch)
        {
            // Draw common tabs for both modes (network, standalone)
            Vector2 tabWidth = new Vector2(97, 0);
            Vector2 tab1Pos = _pos - view + new Vector2(341, 123);
            spriteBatch.Draw(_tabEquipmentTexture, tab1Pos, Color.White);
            spriteBatch.Draw(_tabPlayersTexture, tab1Pos + tabWidth, Color.White);

            // Draw chat tab
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone)
            {
                spriteBatch.Draw(_tabChatTexture, tab1Pos + (tabWidth * 2), Color.White);
            }

            // Draw game settings tab
            Vector2 tabGameSettingsPos = tab1Pos + (tabWidth * 3);
            spriteBatch.Draw(_tabGameSettingsTexture, tabGameSettingsPos, Color.White);

            // Draw tab hilite (texture is the same size as tabs so it can be placed to same position as the selected tab)
            float fadeTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - _tabFadeStartTime).TotalSeconds;
            spriteBatch.Draw(_tabHilite, tab1Pos + (tabWidth * ((int)_currentTab - 1)), new Color(255, 255, 255, (byte)_tabFade.Evaluate(fadeTime)));

            // Draw ready button
            spriteBatch.Draw(_buttonReadyTexture, tab1Pos + new Vector2(419, 0), Color.White);

            // Draw ready button hilite (same size as button)
            Color drawColor = Color.White;
            if (!_readyPressed)
            {
                float readyFadeTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - _readyFadeStartTime).TotalSeconds;
                drawColor = new Color(255, 255, 255, (byte)_readyFade.Evaluate(readyFadeTime));
            }
            spriteBatch.Draw(_buttonReadyHiliteTexture, tab1Pos + new Vector2(419, 0), drawColor);
        }

        private void DrawStatusDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            var data = MenuEngine.Game.DataEngine;

            // Setup positions for statusdisplay texts
            Vector2 statusDisplayTextPos = _pos - view + new Vector2(885, 618);
            Vector2 statusDisplayRowHeight = new Vector2(0, 12);
            Vector2 statusDisplayColumnWidth = new Vector2(75, 0);

            // Setup statusdisplay texts
            string statusDisplayPlayerAmount = "" + data.Players.Count();
            string statusDisplayArenaName = data.ArenaPlaylist[0];
            string statusDisplayStatus = MenuEngine.Game.NetworkMode == NetworkMode.Server
                ? "server"
                : "connected";

            // Draw common statusdisplay texts for all modes
            spriteBatch.DrawString(_menuSmallFont, "Players", statusDisplayTextPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, statusDisplayPlayerAmount, statusDisplayTextPos + statusDisplayColumnWidth, Color.GreenYellow);
            spriteBatch.DrawString(_menuSmallFont, "Arena", statusDisplayTextPos + statusDisplayRowHeight * 4, Color.White);
            spriteBatch.DrawString(_menuSmallFont, statusDisplayArenaName, statusDisplayTextPos + statusDisplayRowHeight * 5, Color.GreenYellow);

            // Draw network game statusdisplay texts
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone)
            {
                spriteBatch.DrawString(_menuSmallFont, "Status", statusDisplayTextPos + statusDisplayRowHeight, Color.White);
                spriteBatch.DrawString(_menuSmallFont, statusDisplayStatus, statusDisplayTextPos + statusDisplayColumnWidth + statusDisplayRowHeight, Color.GreenYellow);
            }

            // Draw client statusdisplay texts
            if (MenuEngine.Game.NetworkMode == NetworkMode.Client)
            {
                spriteBatch.DrawString(_menuSmallFont, "Ping", statusDisplayTextPos + statusDisplayRowHeight * 2, Color.White);
                var textAndColor = GetPingTextAndColor();
                spriteBatch.DrawString(_menuSmallFont, textAndColor.Item1, statusDisplayTextPos + statusDisplayColumnWidth + statusDisplayRowHeight * 2, textAndColor.Item2);
            }
        }

        private void DrawPlayerPanes(Vector2 view, SpriteBatch spriteBatch)
        {
            foreach (var indexedPlayer in MenuPanePlayers)
            {
                var player = indexedPlayer.Item1;
                int playerI = indexedPlayer.Item2;

                // Find out things.
                string playerItemName = "Write your name";
                switch (_currentItems[playerI])
                {
                    case EquipMenuItem.Ship: playerItemName = player.ShipName; break;
                    case EquipMenuItem.Extra: playerItemName = player.ExtraDeviceName; break;
                    case EquipMenuItem.Weapon2: playerItemName = player.Weapon2Name; break;
                }

                // Draw pane background.
                spriteBatch.Draw(_playerNameBackgroundTexture, GetPlayerPanePos(playerI) - view, player.PlayerColor);
                spriteBatch.Draw(_playerNameBorderTexture, GetPlayerPanePos(playerI) - view, Color.White);
                spriteBatch.Draw(_playerPaneTexture, GetPlayerPanePos(playerI) - view + PlayerPaneMainDeltaPos, Color.White);
                spriteBatch.DrawString(_menuSmallFont, player.Name, GetPlayerNamePos(playerI, player.Name) - view, Color.White);

                // Draw icons of selected equipment.
                _equipmentSelectors[playerI, (int)EquipMenuItem.Ship].Draw(view, spriteBatch);
                _equipmentSelectors[playerI, (int)EquipMenuItem.Extra].Draw(view, spriteBatch);
                _equipmentSelectors[playerI, (int)EquipMenuItem.Weapon2].Draw(view, spriteBatch);

                // Draw cursor, highlight and item name.
                float cursorTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - _cursorFadeStartTimes[playerI]).TotalSeconds;
                Texture2D hiliteTexture = _currentItems[playerI] == EquipMenuItem.Name ? _listHiliteTexture : _highlightMainTexture;
                Vector2 hiliteTexturePos = _currentItems[playerI] == EquipMenuItem.Name ? GetPlayerPanePos(playerI) - view : GetPlayerCursorPos(playerI) - view;
                spriteBatch.Draw(hiliteTexture, hiliteTexturePos, Color.White);

                // Draw player name textcursor if necessary
                if (_currentItems[playerI] == EquipMenuItem.Name)
                {
                    Vector2 partialTextSize = _menuSmallFont.MeasureString(_playerNames[playerI].Content.Substring(0, _playerNames[playerI].CaretPosition));
                    Vector2 totalTextSize = _menuSmallFont.MeasureString(_playerNames[playerI].Content);
                    partialTextSize.Y = 0;
                    // Pixel perfect, otherwise thin textures look stupid
                    partialTextSize.X = (float)Math.Round(partialTextSize.X - totalTextSize.X / 2);
                    Vector2 textCursorPos = hiliteTexturePos + partialTextSize + new Vector2(91, 24);
                    spriteBatch.Draw(_listTextCursorTexture, textCursorPos, new Color(255, 255, 255, (byte)_cursorFade.Evaluate(cursorTime)));
                }

                Texture2D cursorTexture = _currentItems[playerI] == EquipMenuItem.Name ? _listCursorTexture : _cursorMainTexture;
                Vector2 cursorTexturePos = _currentItems[playerI] == EquipMenuItem.Name ? GetPlayerPanePos(playerI) - view : GetPlayerCursorPos(playerI) - view;
                spriteBatch.Draw(cursorTexture, cursorTexturePos, new Color(255, 255, 255, (byte)_cursorFade.Evaluate(cursorTime)));
                spriteBatch.DrawString(_menuSmallFont, playerItemName, GetPlayerItemNamePos(playerI, playerItemName) - view, Color.White);
            }
        }

        private void DrawLargeStatusBackground(Vector2 view, SpriteBatch spriteBatch)
        {
            var data = MenuEngine.Game.DataEngine;

            // Draw pane background.
            Vector2 statusPanePos = _pos - view + new Vector2(537, 160);
            spriteBatch.Draw(_statusPaneTexture, statusPanePos, Color.White);
        }

        private Tuple<string, Color> GetPingTextAndColor()
        {
            if (MenuEngine.Game.NetworkMode != NetworkMode.Client ||
                !MenuEngine.Game.NetworkEngine.IsConnectedToGameServer)
                return Tuple.Create("???", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.Average));
            var ping = MenuEngine.Game.NetworkEngine.GameServerConnection.PingInfo.PingTime.TotalMilliseconds;
            if (ping < 35) return Tuple.Create("Excellent", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.Great));
            if (ping < 70) return Tuple.Create("Good", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.High));
            if (ping < 120) return Tuple.Create("Sufficient", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.Average));
            if (ping < 200) return Tuple.Create("Poor", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.Low));
            return Tuple.Create("Dreadful", EquipInfo.GetColorForAmountType(EquipInfo.EquipInfoAmountType.Poor));
        }

        #endregion Drawing methods
    }
}
