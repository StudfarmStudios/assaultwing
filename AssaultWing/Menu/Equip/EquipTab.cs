﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Menu.Equip
{
    public class EquipTab : EquipMenuTab
    {
        private enum EquipMenuItem { Name, Ship, Extra, Weapon2 }

        private const int MAX_MENU_PANES = 4;
        private static Curve g_nameInfoMove;

        private bool _playerNameChanged;

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

        public override Texture2D TabTexture { get { return Content.TabEquipmentTexture; } }

        private IEnumerable<Tuple<Player, int>> MenuPanePlayers
        {
            get
            {
                return MenuEngine.Game.DataEngine.Players
                    .Where(p => !p.IsRemote)
                    .Select((p, i) => Tuple.Create(p, i));
            }
        }

        #region Private coordinate data

        private static readonly Vector2 PLAYER_PANE_NAME_POS = new Vector2(104, 38);
        private static readonly Vector2 PLAYER_PANE_DELTA_POS = new Vector2(203, 0);
        private static readonly Vector2 PLAYER_PANE_ROW_DELTA_POS = new Vector2(0, 91);

        private Vector2 PlayerPaneMainDeltaPos { get { return new Vector2(0, Content.PlayerNameBackgroundTexture.Height); } }
        private Vector2 PlayerPaneCursorDeltaPos { get { return PlayerPaneMainDeltaPos + new Vector2(22, 3); } }
        private Vector2 PlayerPaneIconDeltaPos { get { return PlayerPaneMainDeltaPos + new Vector2(21, 1); } }

        private Vector2 GetPlayerPanePos(int playerI)
        {
            return LeftPanePos + playerI * PLAYER_PANE_DELTA_POS;
        }
        private Vector2 GetPlayerCursorPos(int playerI)
        {
            return GetPlayerPanePos(playerI) + PlayerPaneCursorDeltaPos + ((int)_currentItems[playerI] - 1) * PLAYER_PANE_ROW_DELTA_POS;
        }
        private Vector2 GetPlayerNamePos(int playerI, string name)
        {
            return GetPlayerPanePos(playerI) + new Vector2((int)(106 - Content.FontSmall.MeasureString(name).X / 2), 36);
        }
        private Vector2 GetPlayerItemNamePos(int playerI, string name)
        {
            return GetPlayerPanePos(playerI) + new Vector2((int)(104 - Content.FontSmall.MeasureString(name).X / 2), 355);
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

        static EquipTab()
        {
            g_nameInfoMove = new Curve();
            g_nameInfoMove.Keys.Add(new CurveKey(0f, 0));
            g_nameInfoMove.Keys.Add(new CurveKey(0.6f, 15));
            g_nameInfoMove.Keys.Add(new CurveKey(1.2f, 0));
            g_nameInfoMove.PreLoop = CurveLoopType.Cycle;
            g_nameInfoMove.PostLoop = CurveLoopType.Cycle;
        }

        public EquipTab(EquipMenuComponent menuComponent)
            : base(menuComponent)
        {
            _currentItems = new EquipMenuItem[MAX_MENU_PANES];
            _cursorFadeStartTimes = new TimeSpan[MAX_MENU_PANES];
            UpdateSelectors();
        }

        public override void Update()
        {
            UpdateSelectors();
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

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone)
                DrawLargeStatusBackground(view, spriteBatch);
            DrawPlayerPanes(view, spriteBatch);
            DrawShipInfoDisplay(view, spriteBatch);
            DrawShipDeviceInfoDisplay(view, spriteBatch);
            DrawWeaponInfoDisplay(view, spriteBatch);
            DrawNameChangeInfo(view, spriteBatch);
        }

        private void UpdateSelectors()
        {
            if (_playerNames != null && MenuPanePlayers.Count() == _playerNames.Count()) return;
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

        private void ConditionalPlayerAction(bool condition, int playerI, string soundName, Action action)
        {
            if (!condition) return;
            _cursorFadeStartTimes[playerI] = MenuEngine.Game.GameTime.TotalRealTime;
            if (soundName != null) MenuEngine.Game.SoundEngine.PlaySound(soundName);
            action();
        }

        private void DrawPlayerPanes(Vector2 view, SpriteBatch spriteBatch)
        {
            UpdateSelectors();
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
                spriteBatch.Draw(Content.PlayerNameBackgroundTexture, GetPlayerPanePos(playerI) - view, player.PlayerColor);
                spriteBatch.Draw(Content.PlayerNameBorderTexture, GetPlayerPanePos(playerI) - view, Color.White);
                spriteBatch.Draw(Content.PlayerPaneTexture, GetPlayerPanePos(playerI) - view + PlayerPaneMainDeltaPos, Color.White);
                spriteBatch.DrawString(Content.FontSmall, player.Name, (GetPlayerNamePos(playerI, player.Name) - view).Round(), Color.White);

                // Draw icons of selected equipment.
                _equipmentSelectors[playerI, (int)EquipMenuItem.Ship].Draw(view, spriteBatch);
                _equipmentSelectors[playerI, (int)EquipMenuItem.Extra].Draw(view, spriteBatch);
                _equipmentSelectors[playerI, (int)EquipMenuItem.Weapon2].Draw(view, spriteBatch);

                // Draw cursor, highlight and item name.
                float cursorTime = (float)(MenuEngine.Game.GameTime.TotalRealTime - _cursorFadeStartTimes[playerI]).TotalSeconds;
                var hiliteTexture = _currentItems[playerI] == EquipMenuItem.Name ? Content.ListHiliteTexture : Content.HighlightMainTexture;
                var hiliteTexturePos = _currentItems[playerI] == EquipMenuItem.Name ? GetPlayerPanePos(playerI) - view : GetPlayerCursorPos(playerI) - view;
                spriteBatch.Draw(hiliteTexture, hiliteTexturePos, Color.White);

                // Draw player name textcursor if necessary
                if (_currentItems[playerI] == EquipMenuItem.Name)
                {
                    var partialTextSize = Content.FontSmall.MeasureString(_playerNames[playerI].Content.Substring(0, _playerNames[playerI].CaretPosition));
                    var totalTextSize = Content.FontSmall.MeasureString(_playerNames[playerI].Content);
                    var textCursorPos = hiliteTexturePos + new Vector2(partialTextSize.X - totalTextSize.X / 2 + 91, 24);
                    spriteBatch.Draw(Content.ListTextCursorTexture, textCursorPos.Round(), Color.Multiply(Color.White, EquipMenuComponent.CursorFade.Evaluate(cursorTime)));
                }

                var cursorTexture = _currentItems[playerI] == EquipMenuItem.Name ? Content.ListCursorTexture : Content.CursorMainTexture;
                var cursorTexturePos = _currentItems[playerI] == EquipMenuItem.Name ? GetPlayerPanePos(playerI) - view : GetPlayerCursorPos(playerI) - view;
                spriteBatch.Draw(cursorTexture, cursorTexturePos, Color.Multiply(Color.White, EquipMenuComponent.CursorFade.Evaluate(cursorTime)));
                spriteBatch.DrawString(Content.FontSmall, playerItemName, (GetPlayerItemNamePos(playerI, playerItemName) - view).Round(), Color.White);
            }
        }

        private Vector2 InfoDisplayPos { get { return MenuComponent.Pos + new Vector2(560, 186); } }
        private Vector2 InfoDataPos { get { return InfoDisplayPos + new Vector2(200, 100); } }
        private Vector2 GetInfoTextPos(string text)
        {
            return InfoDataPos + new Vector2(177, 194) - Content.FontSmall.MeasureString(text) / 2;
        }

        private void DrawInfoValue(Vector2 view, SpriteBatch spriteBatch, int line, Enum value)
        {
            var infoDataValueLineHeight = 21;
            var infoDataValuePos = InfoDataPos - view + new Vector2(350, 8);
            var valuePos = infoDataValuePos - new Vector2(Content.FontSmall.MeasureString(value.ToString()).X, -infoDataValueLineHeight * line);
            spriteBatch.DrawString(Content.FontSmall, value.ToString(), valuePos.Round(), EquipInfo.GetColor(value));
        }

        private void DrawShipInfoDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone &&
                MenuPanePlayers.ElementAt(0) != null &&
                _currentItems[MenuPanePlayers.ElementAt(0).Item2] == EquipMenuItem.Ship)
            {
                var player = MenuPanePlayers.ElementAt(0).Item1;
                var ship = (Ship)MenuEngine.Game.DataEngine.GetTypeTemplate(player.ShipName);
                var info = ship.ShipInfo;

                var shipPicture = MenuEngine.Game.Content.Load<Texture2D>(info.PictureName);
                var shipTitlePicture = MenuEngine.Game.Content.Load<Texture2D>(info.TitlePictureName);
                var infoHeaders = MenuEngine.Game.Content.Load<Texture2D>("menu_equip_shipinfo_headers");

                spriteBatch.Draw(shipPicture, InfoDisplayPos - view + new Vector2(-6, 0), Color.White);
                spriteBatch.Draw(shipTitlePicture, InfoDisplayPos - view + new Vector2(190, 18), Color.White);
                spriteBatch.Draw(infoHeaders, InfoDataPos - view, Color.White);

                DrawInfoValue(view, spriteBatch, 0, info.Hull);
                DrawInfoValue(view, spriteBatch, 1, info.Armor);
                DrawInfoValue(view, spriteBatch, 2, info.Speed);
                DrawInfoValue(view, spriteBatch, 3, info.Steering);
                DrawInfoValue(view, spriteBatch, 4, info.ModEnergy);
                DrawInfoValue(view, spriteBatch, 5, info.SpecialEnergy);
                spriteBatch.DrawString(Content.FontSmall, info.InfoText, (GetInfoTextPos(info.InfoText) - view).Round(), new Color(218, 159, 33));
            }
        }

        private void DrawShipDeviceInfoDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone &&
                MenuPanePlayers.ElementAt(0) != null &&
                (_currentItems[MenuPanePlayers.ElementAt(0).Item2] == EquipMenuItem.Weapon2 ||
                _currentItems[MenuPanePlayers.ElementAt(0).Item2] == EquipMenuItem.Extra))
            {
                var player = MenuPanePlayers.ElementAt(0).Item1;

                var device = (ShipDevice)MenuEngine.Game.DataEngine.GetTypeTemplate(player.ExtraDeviceName);
                if (_currentItems[MenuPanePlayers.ElementAt(0).Item2] == EquipMenuItem.Weapon2) device = (ShipDevice)MenuEngine.Game.DataEngine.GetTypeTemplate(player.Weapon2Name);

                var info = device.DeviceInfo;

                var devicePicture = MenuEngine.Game.Content.Load<Texture2D>(info.PictureName);
                var deviceTitlePicture = MenuEngine.Game.Content.Load<Texture2D>(info.TitlePictureName);
                var deviceHeaders = MenuEngine.Game.Content.Load<Texture2D>("menu_equip_deviceinfo_headers");

                spriteBatch.Draw(devicePicture, InfoDisplayPos - view + new Vector2(-6, 0), Color.White);
                spriteBatch.Draw(deviceTitlePicture, InfoDisplayPos - view + new Vector2(190, 18), Color.White);
                spriteBatch.Draw(deviceHeaders, InfoDataPos - view, Color.White);

                DrawInfoValue(view, spriteBatch, 0, info.ReloadSpeed);
                DrawInfoValue(view, spriteBatch, 1, info.EnergyUsage);
                DrawInfoValue(view, spriteBatch, 2, info.UsageType);
                spriteBatch.DrawString(Content.FontSmall, info.InfoText, (GetInfoTextPos(info.InfoText) - view).Round(), new Color(218, 159, 33));
            }
        }

        private void DrawWeaponInfoDisplay(Vector2 view, SpriteBatch spriteBatch)
        {
            if (MenuEngine.Game.NetworkMode != NetworkMode.Standalone &&
                MenuPanePlayers.ElementAt(0) != null &&
                _currentItems[MenuPanePlayers.ElementAt(0).Item2] == EquipMenuItem.Weapon2)
            {
                var player = MenuPanePlayers.ElementAt(0).Item1;
                var weapon = (Weapon)MenuEngine.Game.DataEngine.GetTypeTemplate(player.Weapon2Name);
                var info = weapon.WeaponInfo;
                spriteBatch.Draw(Content.WeaponHeaders, InfoDataPos + new Vector2(0, 64) - view, Color.White);
                DrawInfoValue(view, spriteBatch, 3, info.SingleShotDamage);
                DrawInfoValue(view, spriteBatch, 4, info.ShotSpeed);
                DrawInfoValue(view, spriteBatch, 5, info.RecoilMomentum);
            }
        }

        private void DrawNameChangeInfo(Vector2 view, SpriteBatch spriteBatch)
        {
            if (_playerNameChanged || MenuEngine.Game.NetworkMode == NetworkMode.Standalone) return;
            var moveTime = (float)MenuEngine.Game.GameTime.TotalRealTime.TotalSeconds;
            var nameChangeInfoPos = MenuComponent.Pos - view + new Vector2(250 + g_nameInfoMove.Evaluate(moveTime), 180);
            var nameChangeInfoTexture = MenuEngine.Game.Content.Load<Texture2D>("menu_equip_player_name_changeinfo");
            spriteBatch.Draw(nameChangeInfoTexture, nameChangeInfoPos, MenuPanePlayers.ElementAt(0).Item1.PlayerColor);
        }
    }
}