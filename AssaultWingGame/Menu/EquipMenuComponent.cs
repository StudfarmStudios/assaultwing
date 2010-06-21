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

        private const int MAX_MENU_PANES = 4;

        private Control _controlBack, _controlDone;
        private Vector2 _pos; // position of the component's background texture in menu system coordinates
        private SpriteFont _menuBigFont, _menuSmallFont;
        private Texture2D _backgroundTexture;
        private Texture2D _cursorMainTexture, _highlightMainTexture;
        private Texture2D _playerPaneTexture, _playerNameBackgroundTexture, _playerNameBorderTexture, _playerNameHiliteTexture, _playerNameCursorTexture, _playerNameTextCursorTexture;
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

        private IEnumerable<Pair<Player, int>> MenuPanePlayers
        {
            get
            {
                return AssaultWing.Instance.DataEngine.Players
                    .Where(p => !p.IsRemote)
                    .Select((p, i) => new Pair<Player, int>(p, i));
            }
        }

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
            _currentItems = new EquipMenuItem[MAX_MENU_PANES];
            _cursorFadeStartTimes = new TimeSpan[MAX_MENU_PANES];
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
            _playerNameBackgroundTexture = content.Load<Texture2D>("menu_equip_player_name_bg");
            _playerNameBorderTexture = content.Load<Texture2D>("menu_equip_player_name_border");
            _playerNameHiliteTexture = content.Load<Texture2D>("menu_equip_player_name_hilite");
            _playerNameCursorTexture = content.Load<Texture2D>("menu_equip_player_name_cursor");
            _playerNameTextCursorTexture = content.Load<Texture2D>("menu_equip_player_name_textcursor");
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
            if (MenuPanePlayers.Count() != _playerNames.Count()) CreateSelectors();
            if (Active)
            {
                CheckGeneralControls();
                CheckPlayerControls();
                switch (AssaultWing.Instance.NetworkMode)
                {
                    case NetworkMode.Client:
                        SendSelectionsToRemote(
                            p => !p.IsRemote && p.ServerRegistration != Spectator.ServerRegistrationType.Requested,
                            AssaultWing.Instance.NetworkEngine.GameServerConnection);
                        break;
                    case NetworkMode.Server:
                        SendSelectionsToRemote(
                            p => true,
                            AssaultWing.Instance.NetworkEngine.GameClientConnections);
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
                var player = indexedPlayer.First;
                int playerI = indexedPlayer.Second;
                _currentItems[playerI] = EquipMenuItem.Ship;
                _playerNames[playerI] = new EditableText(player.Name, 20, EditableText.Keysets.PlayerNameSet);
                _equipmentSelectors[playerI, (int)EquipMenuItem.Ship] = new ShipSelector(player, GetShipSelectorPos(playerI));
                _equipmentSelectors[playerI, (int)EquipMenuItem.Extra] = new ExtraDeviceSelector(player, GetExtraDeviceSelectorPos(playerI));
                _equipmentSelectors[playerI, (int)EquipMenuItem.Weapon2] = new Weapon2Selector(player, GetWeapon2SelectorPos(playerI));
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
            foreach (var indexedPlayer in MenuPanePlayers)
            {
                var player = indexedPlayer.First;
                int playerI = indexedPlayer.Second;
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
                    _playerNames[playerI].Update(() => { player.Name = _playerNames[playerI].Content; });
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

        private static void SendSelectionsToRemote(Func<Player, bool> sendCriteria, IConnection connection)
        {
            foreach (var player in AssaultWing.Instance.DataEngine.Players.Where(sendCriteria))
            {
                var mess = new PlayerSettingsRequest
                {
                    IsRegisteredToServer = player.ServerRegistration == Spectator.ServerRegistrationType.Yes,
                    PlayerID = player.ID
                };
                mess.Write(player, SerializationModeFlags.ConstantData);
                if (player.ServerRegistration == Spectator.ServerRegistrationType.No)
                    player.ServerRegistration = Spectator.ServerRegistrationType.Requested;
                connection.Send(mess);
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

            // Draw info display if in network game
            DrawShipInfoDisplay(view, spriteBatch);

            // Draw ship device info display
            DrawShipDeviceInfoDisplay(view, spriteBatch);

            // Draw weapon info display
            DrawWeaponInfoDisplay(view, spriteBatch);

            // Draw player info (TEST/HACK)
            DrawPlayerInfoDisplay(view, spriteBatch, MenuPanePlayers.ElementAt(0).First);
        }

        private void DrawPlayerInfoDisplay(Vector2 view, SpriteBatch spriteBatch, Player player)
        {
            // THIS IF IS A HACK JUST TO ENABLE THIS SCREEN TO SHOWN WHEN IN PLAYER NAME CHANGE
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone &&
                MenuPanePlayers.ElementAt(0) != null &&
                _currentItems[MenuPanePlayers.ElementAt(0).Second] == EquipMenuItem.Name)
            {
                Weapon weapon = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(player.Weapon2Name);
                ShipDeviceInfo weaponInfo = weapon.DeviceInfo;
                ShipDevice device = (ShipDevice)AssaultWing.Instance.DataEngine.GetTypeTemplate(player.ExtraDeviceName);
                ShipDeviceInfo deviceInfo = device.DeviceInfo;
                Ship ship = (Ship)AssaultWing.Instance.DataEngine.GetTypeTemplate(player.ShipName);
                ShipInfo shipInfo = ship.ShipInfo;
                Vector2 infoDisplayPos = _pos - view + new Vector2(570, 191);

                var shipPicture = AssaultWing.Instance.Content.Load<Texture2D>(shipInfo.PictureName);
                var shipTitlePicture = AssaultWing.Instance.Content.Load<Texture2D>(shipInfo.TitlePictureName);
                var weaponPicture = AssaultWing.Instance.Content.Load<Texture2D>(weaponInfo.PictureName);
                var weaponTitlePicture = AssaultWing.Instance.Content.Load<Texture2D>(weaponInfo.TitlePictureName);
                var devicePicture = AssaultWing.Instance.Content.Load<Texture2D>(deviceInfo.PictureName);
                var deviceTitlePicture = AssaultWing.Instance.Content.Load<Texture2D>(deviceInfo.TitlePictureName);

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
        }

        private void DrawWeaponInfoDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone &&
                MenuPanePlayers.ElementAt(0) != null &&
                _currentItems[MenuPanePlayers.ElementAt(0).Second] == EquipMenuItem.Weapon2)
            {
                Player player = MenuPanePlayers.ElementAt(0).First;
                Weapon weapon = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(player.Weapon2Name);
                WeaponInfo info = weapon.WeaponInfo;
                Vector2 infoDisplayPos = _pos - view + new Vector2(560, 186);
                Vector2 infoDataPos = infoDisplayPos + new Vector2(200, 164);
                Vector2 infoDataValuePos = infoDataPos + new Vector2(350, 8);
                Vector2 infoDataValueLineHeight = new Vector2(0, 21);

                var weaponHeaders = AssaultWing.Instance.Content.Load<Texture2D>("menu_equip_weaponinfo_headers");
                spriteBatch.Draw(weaponHeaders, infoDataPos, Color.White);

                spriteBatch.DrawString(_menuSmallFont, info.SingleShotDamage.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.SingleShotDamage.ToString()).X, 0), EquipInfo.GetColorForAmountType(info.SingleShotDamage));
                spriteBatch.DrawString(_menuSmallFont, info.ShotSpeed.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.ShotSpeed.ToString()).X, 0) + (infoDataValueLineHeight), EquipInfo.GetColorForAmountType(info.ShotSpeed));
                spriteBatch.DrawString(_menuSmallFont, info.RecoilMomentum.ToString(), infoDataValuePos - new Vector2(_menuSmallFont.MeasureString(info.RecoilMomentum.ToString()).X, 0) + (infoDataValueLineHeight * 2), EquipInfo.GetReversedColorForAmountType(info.RecoilMomentum));
            }
        }

        private void DrawShipDeviceInfoDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone &&
                MenuPanePlayers.ElementAt(0) != null &&
                (_currentItems[MenuPanePlayers.ElementAt(0).Second] == EquipMenuItem.Weapon2 ||
                _currentItems[MenuPanePlayers.ElementAt(0).Second] == EquipMenuItem.Extra))
            {
                Player player = MenuPanePlayers.ElementAt(0).First;

                ShipDevice device = (ShipDevice)AssaultWing.Instance.DataEngine.GetTypeTemplate(player.ExtraDeviceName);
                if (_currentItems[MenuPanePlayers.ElementAt(0).Second] == EquipMenuItem.Weapon2) device = (ShipDevice)AssaultWing.Instance.DataEngine.GetTypeTemplate(player.Weapon2Name);

                ShipDeviceInfo info = device.DeviceInfo;
                Vector2 infoDisplayPos = _pos - view + new Vector2(560, 186);
                Vector2 infoDataPos = infoDisplayPos + new Vector2(200, 100);
                Vector2 infoDataValuePos = infoDataPos + new Vector2(350, 8);
                Vector2 infoDataValueLineHeight = new Vector2(0, 21);
                Vector2 infoTextPos = infoDataPos + new Vector2(177, 194) - _menuSmallFont.MeasureString(info.InfoText) / 2;

                var devicePicture = AssaultWing.Instance.Content.Load<Texture2D>(info.PictureName);
                var deviceTitlePicture = AssaultWing.Instance.Content.Load<Texture2D>(info.TitlePictureName);
                var deviceHeaders = AssaultWing.Instance.Content.Load<Texture2D>("menu_equip_deviceinfo_headers");

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
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone &&
                MenuPanePlayers.ElementAt(0) != null &&
                _currentItems[MenuPanePlayers.ElementAt(0).Second] == EquipMenuItem.Ship)
            {
                Player player = MenuPanePlayers.ElementAt(0).First;
                Ship ship = (Ship)AssaultWing.Instance.DataEngine.GetTypeTemplate(player.ShipName);
                ShipInfo info = ship.ShipInfo;
                Vector2 infoDisplayPos = _pos - view + new Vector2(560, 186);
                Vector2 infoDataPos = infoDisplayPos + new Vector2(200, 100);
                Vector2 infoDataValuePos = infoDataPos + new Vector2(350, 8);
                Vector2 infoDataValueLineHeight = new Vector2(0, 21);
                Vector2 infoTextPos = infoDataPos + new Vector2(177, 194) - _menuSmallFont.MeasureString(info.InfoText) / 2;

                var shipPicture = AssaultWing.Instance.Content.Load<Texture2D>(info.PictureName);
                var shipTitlePicture = AssaultWing.Instance.Content.Load<Texture2D>(info.TitlePictureName);
                var infoHeaders = AssaultWing.Instance.Content.Load<Texture2D>("menu_equip_shipinfo_headers");

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
            foreach (var indexedPlayer in MenuPanePlayers)
            {
                var player = indexedPlayer.First;
                int playerI = indexedPlayer.Second;

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
                float cursorTime = (float)(AssaultWing.Instance.GameTime.TotalRealTime - _cursorFadeStartTimes[playerI]).TotalSeconds;
                Texture2D hiliteTexture = _currentItems[playerI] == EquipMenuItem.Name ? _playerNameHiliteTexture : _highlightMainTexture;
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
                    spriteBatch.Draw(_playerNameTextCursorTexture, textCursorPos, new Color(255, 255, 255, (byte)_cursorFade.Evaluate(cursorTime)));
                }

                Texture2D cursorTexture = _currentItems[playerI] == EquipMenuItem.Name ? _playerNameCursorTexture : _cursorMainTexture;
                Vector2 cursorTexturePos = _currentItems[playerI] == EquipMenuItem.Name ? GetPlayerPanePos(playerI) - view : GetPlayerCursorPos(playerI) - view;
                spriteBatch.Draw(cursorTexture, cursorTexturePos, new Color(255, 255, 255, (byte)_cursorFade.Evaluate(cursorTime)));
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
               
                /*
                // Draw pane content text. (the IF is hack for now to allow something to be drawn when not selecting ship)
                if (MenuPanePlayers.ElementAt(0) != null && _currentItems[MenuPanePlayers.ElementAt(0).Second] == EquipMenuItem.Name)
                {
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
                }*/
            }
        }
    }
}
