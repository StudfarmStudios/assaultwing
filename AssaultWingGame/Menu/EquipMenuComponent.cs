using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Core;
using AW2.Game;
using AW2.Game.GobUtils;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Net;
using AW2.Net.Messages;
using AW2.UI;

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

        private const int MAX_LOCAL_PLAYERS = 4;

        private Control _controlBack, _controlDone;
        private Vector2 _pos; // position of the component's background texture in menu system coordinates
        private SpriteFont _menuBigFont, _menuSmallFont;
        private Texture2D _backgroundTexture;
        private Texture2D _cursorMainTexture, _highlightMainTexture;
        private Texture2D _playerPaneTexture, _player1PaneTopTexture, _player2PaneTopTexture;
        private Texture2D _statusPaneTexture;
        private Texture2D _tabEquipmentTexture, _tabPlayersTexture, _tabGameSettingsTexture, _tabChatTexture, _tabHilite;
        private Texture2D _buttonReadyTexture, _buttonReadyHiliteTexture;

        /// <summary>
        /// Cursor fade curve as a function of time in seconds.
        /// Values range from 0 (transparent) to 255 (opaque).
        /// </summary>
        private Curve _cursorFade;

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

        private Vector2 PlayerPaneMainDeltaPos { get { return new Vector2(0, _player1PaneTopTexture.Height); } }
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
            return GetPlayerPanePos(playerI) + new Vector2((int)(104 - _menuSmallFont.MeasureString(name).X / 2), 38);
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
                    menuEngine.IsProgressBarVisible = false;
                    menuEngine.IsHelpTextVisible = true;
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

        /// <summary>
        /// Creates an equip menu component for a menu system.
        /// </summary>
        /// <param name="menuEngine">The menu system.</param>
        public EquipMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            _controlDone = new KeyboardKey(Keys.Enter);
            _controlBack = new KeyboardKey(Keys.Escape);
            _pos = new Vector2(0, 0);
            _currentItems = new EquipMenuItem[MAX_LOCAL_PLAYERS];
            for (int i = 0; i < MAX_LOCAL_PLAYERS; ++i) _currentItems[i] = EquipMenuItem.Ship;
            _cursorFadeStartTimes = new TimeSpan[MAX_LOCAL_PLAYERS];
            _cursorFade = new Curve();
            _cursorFade.Keys.Add(new CurveKey(0, 255, 0, 0, CurveContinuity.Step));
            _cursorFade.Keys.Add(new CurveKey(0.5f, 0, 0, 0, CurveContinuity.Step));
            _cursorFade.Keys.Add(new CurveKey(1, 255, 0, 0, CurveContinuity.Step));
            _cursorFade.PreLoop = CurveLoopType.Cycle;
            _cursorFade.PostLoop = CurveLoopType.Cycle;
            CreateSelectors();
        }

        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            _menuBigFont = content.Load<SpriteFont>("MenuFontBig");
            _menuSmallFont = content.Load<SpriteFont>("MenuFontSmall");
            _backgroundTexture = content.Load<Texture2D>("menu_equip_bg");
            _cursorMainTexture = content.Load<Texture2D>("menu_equip_cursor_large");
            _highlightMainTexture = content.Load<Texture2D>("menu_equip_hilite_large");
            _playerPaneTexture = content.Load<Texture2D>("menu_equip_player_bg");
            _player1PaneTopTexture = content.Load<Texture2D>("menu_equip_player_color_green");
            _player2PaneTopTexture = content.Load<Texture2D>("menu_equip_player_color_red");
            _statusPaneTexture = content.Load<Texture2D>("menu_equip_status_display");

            _tabEquipmentTexture = content.Load<Texture2D>("menu_equip_tab_equipment");
            _tabPlayersTexture = content.Load<Texture2D>("menu_equip_tab_players");
            _tabGameSettingsTexture = content.Load<Texture2D>("menu_equip_tab_gamesettings");
            _tabChatTexture = content.Load<Texture2D>("menu_equip_tab_chat");
            _tabHilite = content.Load<Texture2D>("menu_equip_tab_hilite");

            _buttonReadyTexture = content.Load<Texture2D>("menu_equip_btn_ready");
            _buttonReadyHiliteTexture = content.Load<Texture2D>("menu_equip_btn_ready_hilite");
        }

        public override void UnloadContent()
        {
            // The textures and fonts we reference will be disposed by GraphicsEngine.
        }

        public override void Update()
        {
            if (AssaultWing.Instance.DataEngine.Players.Count() != _playerNames.Count())
                CreateSelectors();
            if (Active)
            {
                CheckGeneralControls();
                CheckPlayerControls();
            }
        }

        private void CreateSelectors()
        {
            if (AssaultWing.Instance.DataEngine.Players.Where(p => !p.IsRemote).Count() > MAX_LOCAL_PLAYERS)
                throw new ApplicationException("Too many local players");
            int aspectCount = Enum.GetValues(typeof(EquipMenuItem)).Length;
            _equipmentSelectors = new EquipmentSelector[AssaultWing.Instance.DataEngine.Players.Count(), aspectCount];
            _playerNames = new EditableText[AssaultWing.Instance.DataEngine.Players.Count()];

            int playerI = 0;
            foreach (var player in AssaultWing.Instance.DataEngine.Players)
            {
                _playerNames[playerI] = new EditableText(player.Name, 20, EditableText.Keysets.PlayerNameSet);
                _equipmentSelectors[playerI, (int)EquipMenuItem.Ship] = new ShipSelector(player, GetShipSelectorPos(playerI));
                _equipmentSelectors[playerI, (int)EquipMenuItem.Extra] = new ExtraDeviceSelector(player, GetExtraDeviceSelectorPos(playerI));
                _equipmentSelectors[playerI, (int)EquipMenuItem.Weapon2] = new Weapon2Selector(player, GetWeapon2SelectorPos(playerI));
                ++playerI;
            }
        }

        private void CheckGeneralControls()
        {
            if (_controlBack.Pulse)
                menuEngine.ActivateComponent(MenuComponentType.Main);
            else if (_controlDone.Pulse)
            {
                switch (AssaultWing.Instance.NetworkMode)
                {
                    case NetworkMode.Server:
                        // HACK: Server has a fixed arena playlist
                        // Start loading the first arena and display its progress.
                        menuEngine.ProgressBarAction(
                            AssaultWing.Instance.PrepareFirstArena,
                            AssaultWing.Instance.StartArena);
                        menuEngine.Deactivate();
                        break;
                    case NetworkMode.Client:
                        // Client advances only when the server says so.
                        break;
                    case NetworkMode.Standalone:
                        menuEngine.ActivateComponent(MenuComponentType.Arena);
                        break;
                    default: throw new Exception("Unexpected network mode " + AssaultWing.Instance.NetworkMode);
                }
            }
        }

        private void CheckPlayerControls()
        {
            int playerI = -1;
            foreach (var player in AssaultWing.Instance.DataEngine.Players)
            {
                if (player.IsRemote) return;
                ++playerI;

                ConditionalPlayerAction(player.Controls.Thrust.Pulse, playerI, "MenuBrowseItem", () =>
                {
                    var minItem = AssaultWing.Instance.NetworkMode == NetworkMode.Standalone
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
                        player.Name = _playerNames[playerI].Content;
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
                    {
                        _equipmentSelectors[playerI, (int)_currentItems[playerI]].CurrentValue += selectionChange;

                        // Send new equipment choices to the game server.
                        if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
                        {
                            var equipUpdateRequest = new JoinGameRequest();
                            equipUpdateRequest.PlayerInfos = new List<PlayerInfo> { new PlayerInfo(player) };
                            AssaultWing.Instance.NetworkEngine.GameServerConnection.Send(equipUpdateRequest);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper for <seealso cref="CheckPlayerControls"/>
        /// </summary>
        private void ConditionalPlayerAction(bool condition, int playerI, string soundName, Action action)
        {
            if (!condition) return;
            _cursorFadeStartTimes[playerI] = AssaultWing.Instance.GameTime.TotalRealTime;
            if (soundName != null) AssaultWing.Instance.SoundEngine.PlaySound(soundName);
            action();
        }

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
            DrawPlayerPanes(view, spriteBatch);
            DrawNetworkPane(view, spriteBatch);
        }

        private void DrawTabsAndButtons(Vector2 view, SpriteBatch spriteBatch)
        {
            // Draw common tabs for both modes (network, standalone)
            Vector2 tab1Pos = _pos - view + new Vector2(341, 123);
            Vector2 tabWidth = new Vector2(97, 0);
            spriteBatch.Draw(_tabEquipmentTexture, tab1Pos, Color.White);
            spriteBatch.Draw(_tabPlayersTexture, tab1Pos + tabWidth, Color.White);

            // Draw tab hilite (texture is the same size as tabs so it can be placed to same position as the selected tab)
            spriteBatch.Draw(_tabHilite, tab1Pos, Color.White);

            // Draw chat tab
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone)
            {
                spriteBatch.Draw(_tabChatTexture, tab1Pos + (tabWidth * 2), Color.White);
            }

            // Draw game settings tab
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Standalone || AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                Vector2 tabGameSettingsPos = AssaultWing.Instance.NetworkMode == NetworkMode.Server
                    ? tab1Pos + (tabWidth * 3)
                    : tab1Pos + (tabWidth * 2);

                spriteBatch.Draw(_tabGameSettingsTexture, tabGameSettingsPos, Color.White);
            }

            // Draw ready button
            spriteBatch.Draw(_buttonReadyTexture, tab1Pos + new Vector2(419, 0), Color.White);

            // Draw ready button hilite (same size as button)
            spriteBatch.Draw(_buttonReadyHiliteTexture, tab1Pos + new Vector2(419, 0), Color.White);
        }

        private void DrawStatusDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            var data = AssaultWing.Instance.DataEngine;

            // Setup positions for statusdisplay texts
            Vector2 statusDisplayTextPos = _pos - view + new Vector2(885, 618);
            Vector2 statusDisplayRowHeight = new Vector2(0, 12);
            Vector2 statusDisplayColumnWidth = new Vector2(75, 0);

            // Setup statusdisplay texts
            string statusDisplayPlayerAmount = AssaultWing.Instance.NetworkMode == NetworkMode.Standalone
                ? "" + data.Players.Count()
                : "2-8";
            string statusDisplayArenaName = AssaultWing.Instance.NetworkMode == NetworkMode.Standalone
                ? data.ArenaPlaylist[0]
                : "to be announced";
            string statusDisplayStatus = AssaultWing.Instance.NetworkMode == NetworkMode.Server
                ? "server"
                : "connected";
            string statusDisplayPing = "good";

            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone)
            {
                bool unsureData = data.Spectators.Count == 1 && AssaultWing.Instance.NetworkMode == NetworkMode.Client;
                if (!unsureData)
                {
                    statusDisplayPlayerAmount = "" + data.Spectators.Count;
                    statusDisplayArenaName = "" + data.ArenaPlaylist[0];
                }
            }

            // Draw common statusdisplay texts for all modes
            spriteBatch.DrawString(_menuSmallFont, "Players", statusDisplayTextPos, Color.White);
            spriteBatch.DrawString(_menuSmallFont, statusDisplayPlayerAmount, statusDisplayTextPos + statusDisplayColumnWidth, Color.GreenYellow);
            spriteBatch.DrawString(_menuSmallFont, "Arena", statusDisplayTextPos + statusDisplayRowHeight * 4, Color.White);
            spriteBatch.DrawString(_menuSmallFont, statusDisplayArenaName, statusDisplayTextPos + statusDisplayRowHeight * 5, Color.GreenYellow);

            // Draw network game statusdisplay texts
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone)
            {
                spriteBatch.DrawString(_menuSmallFont, "Status", statusDisplayTextPos + statusDisplayRowHeight, Color.White);
                spriteBatch.DrawString(_menuSmallFont, statusDisplayStatus, statusDisplayTextPos + statusDisplayColumnWidth + statusDisplayRowHeight, Color.GreenYellow);
            }

            // Draw client statusdisplay texts
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                spriteBatch.DrawString(_menuSmallFont, "Ping", statusDisplayTextPos + statusDisplayRowHeight * 2, Color.White);
                spriteBatch.DrawString(_menuSmallFont, statusDisplayPing, statusDisplayTextPos + statusDisplayColumnWidth + statusDisplayRowHeight * 2, Color.GreenYellow);
            }
        }

        private void DrawPlayerPanes(Vector2 view, SpriteBatch spriteBatch)
        {
            var data = AssaultWing.Instance.DataEngine;

            // Draw player panes.
            int playerI = -1;
            foreach (var player in data.Players)
            {
                if (player.IsRemote) continue;
                ++playerI;

                // Find out things.
                string playerItemName = "???";
                switch (_currentItems[playerI])
                {
                    case EquipMenuItem.Ship: playerItemName = player.ShipName; break;
                    case EquipMenuItem.Extra: playerItemName = player.ExtraDeviceName; break;
                    case EquipMenuItem.Weapon2: playerItemName = player.Weapon2Name; break;
                }
                Texture2D GetPlayerPaneTopTexture = playerI == 1 ? _player2PaneTopTexture : _player1PaneTopTexture;

                // Draw pane background.
                spriteBatch.Draw(GetPlayerPaneTopTexture, GetPlayerPanePos(playerI) - view, Color.White);
                spriteBatch.Draw(_playerPaneTexture, GetPlayerPanePos(playerI) - view + PlayerPaneMainDeltaPos, Color.White);
                spriteBatch.DrawString(_menuSmallFont, player.Name, GetPlayerNamePos(playerI, player.Name) - view, Color.White);

                // Draw icons of selected equipment.
                _equipmentSelectors[playerI, (int)EquipMenuItem.Ship].Draw(view, spriteBatch);
                _equipmentSelectors[playerI, (int)EquipMenuItem.Extra].Draw(view, spriteBatch);
                _equipmentSelectors[playerI, (int)EquipMenuItem.Weapon2].Draw(view, spriteBatch);

                // Draw cursor, highlight and item name.
                float cursorTime = (float)(AssaultWing.Instance.GameTime.TotalRealTime - _cursorFadeStartTimes[playerI]).TotalSeconds;
                spriteBatch.Draw(_highlightMainTexture, GetPlayerCursorPos(playerI) - view, Color.White);
                spriteBatch.Draw(_cursorMainTexture, GetPlayerCursorPos(playerI) - view, new Color(255, 255, 255, (byte)_cursorFade.Evaluate(cursorTime)));
                spriteBatch.DrawString(_menuSmallFont, playerItemName, GetPlayerItemNamePos(playerI, playerItemName) - view, Color.White);
            }
        }

        private void DrawNetworkPane(Vector2 view, SpriteBatch spriteBatch)
        {
            var data = AssaultWing.Instance.DataEngine;

            // Draw network game status pane.
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone)
            {
                // Draw pane background.
                Vector2 statusPanePos = _pos - view + new Vector2(537, 160);
                spriteBatch.Draw(_statusPaneTexture, statusPanePos, Color.White);

                // Draw pane content text.
                Vector2 textPos = statusPanePos + new Vector2(60, 60);
                string textContent = AssaultWing.Instance.NetworkMode == NetworkMode.Client
                    ? "Connected to game server"
                    : "Hosting a game as server";
                bool unsureData = data.Spectators.Count == 1 && AssaultWing.Instance.NetworkMode == NetworkMode.Client;
                if (unsureData)
                {
                    textContent += "\n\n2 or more players";
                    textContent += "\n\nArena: to be announced";
                }
                else
                {
                    textContent += "\n\n" + data.Spectators.Count + (data.Spectators.Count == 1 ? " player" : " players");
                    textContent += "\n\nArena: " + data.ArenaPlaylist[0];
                }
                spriteBatch.DrawString(_menuBigFont, textContent, textPos, Color.White);
            }
        }
    }
}
