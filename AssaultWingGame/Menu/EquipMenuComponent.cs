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
    class EquipMenuComponent : MenuComponent
    {
        /// <summary>
        /// An item in a pane main display in the equip menu.
        /// </summary>
        enum EquipMenuItem { Ship, Extra, Weapon2 }

        bool serverIsCreatingGobs; // HACK: for initialising arena on client
        Control controlBack, controlDone;
        Vector2 pos; // position of the component's background texture in menu system coordinates
        SpriteFont menuBigFont, menuSmallFont;
        Texture2D backgroundTexture;
        Texture2D cursorMainTexture, highlightMainTexture;
        Texture2D playerPaneTexture, player1PaneTopTexture, player2PaneTopTexture;
        Texture2D statusPaneTexture;
        Texture2D tabEquipmentTexture, tabPlayersTexture, tabGameSettingsTexture, tabChatTexture, tabHilite;
        Texture2D buttonReadyTexture, buttonReadyHiliteTexture;

        /// <summary>
        /// Cursor fade curve as a function of time in seconds.
        /// Values range from 0 (transparent) to 255 (opaque).
        /// </summary>
        Curve cursorFade;

        /// <summary>
        /// Time at which the cursor started fading for each player.
        /// </summary>
        TimeSpan[] cursorFadeStartTimes;

        /// <summary>
        /// Index of current item in each player's pane main display.
        /// </summary>
        EquipMenuItem[] currentItems;

        /// <summary>
        /// Equipment selectors for each player and each aspect of the player's equipment.
        /// Indexed as [playerI, aspectI].
        /// </summary>
        EquipmentSelector[,] equipmentSelectors;

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
                    serverIsCreatingGobs = false;
                }
            }
        }

        /// <summary>
        /// The center of the menu component in menu system coordinates.
        /// </summary>
        /// This is a good place to center the menu view to when the menu component
        /// is to be seen well on the screen.
        public override Vector2 Center { get { return pos + new Vector2(750, 460); } }

        /// <summary>
        /// Creates an equip menu component for a menu system.
        /// </summary>
        /// <param name="menuEngine">The menu system.</param>
        public EquipMenuComponent(MenuEngineImpl menuEngine)
            : base(menuEngine)
        {
            controlDone = new KeyboardKey(Keys.Enter);
            controlBack = new KeyboardKey(Keys.Escape);
            pos = new Vector2(0, 0);
            currentItems = new EquipMenuItem[4];
            cursorFadeStartTimes = new TimeSpan[4];

            cursorFade = new Curve();
            cursorFade.Keys.Add(new CurveKey(0, 255, 0, 0, CurveContinuity.Step));
            cursorFade.Keys.Add(new CurveKey(0.5f, 0, 0, 0, CurveContinuity.Step));
            cursorFade.Keys.Add(new CurveKey(1, 255, 0, 0, CurveContinuity.Step));
            cursorFade.PreLoop = CurveLoopType.Cycle;
            cursorFade.PostLoop = CurveLoopType.Cycle;
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            menuBigFont = content.Load<SpriteFont>("MenuFontBig");
            menuSmallFont = content.Load<SpriteFont>("MenuFontSmall");
            backgroundTexture = content.Load<Texture2D>("menu_equip_bg");
            cursorMainTexture = content.Load<Texture2D>("menu_equip_cursor_large");
            highlightMainTexture = content.Load<Texture2D>("menu_equip_hilite_large");
            playerPaneTexture = content.Load<Texture2D>("menu_equip_player_bg");
            player1PaneTopTexture = content.Load<Texture2D>("menu_equip_player_color_green");
            player2PaneTopTexture = content.Load<Texture2D>("menu_equip_player_color_red");
            statusPaneTexture = content.Load<Texture2D>("menu_equip_status_display");
            
            tabEquipmentTexture = content.Load<Texture2D>("menu_equip_tab_equipment");
            tabPlayersTexture = content.Load<Texture2D>("menu_equip_tab_players");
            tabGameSettingsTexture = content.Load<Texture2D>("menu_equip_tab_gamesettings");
            tabChatTexture = content.Load<Texture2D>("menu_equip_tab_chat");
            tabHilite = content.Load<Texture2D>("menu_equip_tab_hilite");

            buttonReadyTexture = content.Load<Texture2D>("menu_equip_btn_ready");
            buttonReadyHiliteTexture = content.Load<Texture2D>("menu_equip_btn_ready_hilite");
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public override void UnloadContent()
        {
            // The textures and fonts we reference will be disposed by GraphicsEngine.
        }

        /// <summary>
        /// Updates the menu component.
        /// </summary>
        public override void Update()
        {
            if (Active)
            {
                CheckGeneralControls();
                CheckPlayerControls();
                CheckNetwork();
            }
            if (serverIsCreatingGobs) CheckNetwork(); // HACK: done for inactive menu also to allow client to initialise and start arena
        }

        /// <summary>
        /// Sets up selectors for each aspect of equipment of each player.
        /// </summary>
        private void CreateSelectors()
        {
            int aspectCount = Enum.GetValues(typeof(EquipMenuItem)).Length;
            equipmentSelectors = new EquipmentSelector[AssaultWing.Instance.DataEngine.Spectators.Count, aspectCount];

            int playerI = 0;
            foreach (var player in AssaultWing.Instance.DataEngine.Players)
            {
                equipmentSelectors[playerI, (int)EquipMenuItem.Ship] = new ShipSelector(player);
                equipmentSelectors[playerI, (int)EquipMenuItem.Extra] = new ExtraDeviceSelector(player);
                equipmentSelectors[playerI, (int)EquipMenuItem.Weapon2] = new Weapon2Selector(player);
                ++playerI;
            }
        }

        private void CheckGeneralControls()
        {
            if (controlBack.Pulse)
                menuEngine.ActivateComponent(MenuComponentType.Main);
            else if (controlDone.Pulse)
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

                ConditionalPlayerAction(player.Controls.Thrust.Pulse, playerI, () =>
                {
                    if (currentItems[playerI] > 0)
                        --currentItems[playerI];
                    AssaultWing.Instance.SoundEngine.PlaySound("MenuBrowseItem");
                });
                ConditionalPlayerAction(player.Controls.Down.Pulse, playerI, () =>
                {
                    if ((int)currentItems[playerI] < Enum.GetValues(typeof(EquipMenuItem)).Length - 1)
                        ++currentItems[playerI];
                    AssaultWing.Instance.SoundEngine.PlaySound("MenuBrowseItem");
                });

                int selectionChange = 0;
                ConditionalPlayerAction(player.Controls.Left.Pulse, playerI, () =>
                {
                    selectionChange = -1;
                    AssaultWing.Instance.SoundEngine.PlaySound("MenuChangeItem");
                });
                ConditionalPlayerAction(player.Controls.Fire1.Pulse || player.Controls.Right.Pulse, playerI, () =>
                {
                    selectionChange = 1;
                    AssaultWing.Instance.SoundEngine.PlaySound("MenuChangeItem");
                });
                if (selectionChange != 0)
                {
                    equipmentSelectors[playerI, (int)currentItems[playerI]].CurrentValue += selectionChange;

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

        /// <summary>
        /// Helper for <seealso cref="CheckPlayerControls"/>
        /// </summary>
        private void ConditionalPlayerAction(bool condition, int playerI, Action action)
        {
            if (!condition) return;
            cursorFadeStartTimes[playerI] = AssaultWing.Instance.GameTime.TotalRealTime;
            action();
        }

        private void CheckNetwork()
        {
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                var message = AssaultWing.Instance.NetworkEngine.GameServerConnection.Messages.TryDequeue<StartGameMessage>();
                if (message != null)
                {
                    message.DeserializePlayers(playerID =>
                    {
                        var player = (Player)AssaultWing.Instance.DataEngine.Spectators.FirstOrDefault(p => p.Id == playerID);
                        if (player == null)
                        {
                            player = new Player("uninitialised", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, 0x7ea1eaf);
                            AssaultWing.Instance.DataEngine.Spectators.Add(player);
                        }
                        return player;
                    });
                    AssaultWing.Instance.DataEngine.ArenaPlaylist = new AW2.Helpers.Collections.Playlist(message.ArenaPlaylist);

                    // Prepare and start playing the game.
                    menuEngine.ProgressBarAction(AssaultWing.Instance.PrepareFirstArena,
                        () => MessageHandlers.ActivateHandlers(MessageHandlers.GetClientGameplayHandlers((PingedConnection)AssaultWing.Instance.NetworkEngine.GameServerConnection)));
                    menuEngine.Deactivate();
                }
            }
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
            var data = AssaultWing.Instance.DataEngine;
            spriteBatch.Draw(backgroundTexture, pos - view, Color.White);

            // Draw common tabs for both modes (network, standalone)
            Vector2 tab1Pos = pos - view + new Vector2(341, 123);
            Vector2 tabWidth = new Vector2(97, 0);
            spriteBatch.Draw(tabEquipmentTexture, tab1Pos, Color.White);
            spriteBatch.Draw(tabPlayersTexture, tab1Pos + tabWidth, Color.White);
            
            // Draw tab hilite (texture is the same size as tabs so it can be placed to same position as the selected tab)
            spriteBatch.Draw(tabHilite, tab1Pos, Color.White);

            // Draw chat tab
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone)
            {
                spriteBatch.Draw(tabChatTexture, tab1Pos + (tabWidth * 2), Color.White);
            }
            // Draw game settings tab
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Standalone || AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                Vector2 tabGameSettingsPos = AssaultWing.Instance.NetworkMode == NetworkMode.Server
                    ? tab1Pos + (tabWidth * 3)
                    : tab1Pos + (tabWidth * 2);

                spriteBatch.Draw(tabGameSettingsTexture, tabGameSettingsPos, Color.White);
            }
            
            // Draw ready button
            spriteBatch.Draw(buttonReadyTexture, tab1Pos + new Vector2(419, 0), Color.White);
            // Draw ready buttom hilite (same size than button)
            spriteBatch.Draw(buttonReadyHiliteTexture, tab1Pos + new Vector2(419, 0), Color.White);

            // Setup positions for statusdisplay texts
            Vector2 statusDisplayTextPos = pos - view + new Vector2(885, 618);
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
            spriteBatch.DrawString(menuSmallFont, "Players", statusDisplayTextPos, Color.White);          
            spriteBatch.DrawString(menuSmallFont, statusDisplayPlayerAmount, statusDisplayTextPos + statusDisplayColumnWidth, Color.GreenYellow);
            spriteBatch.DrawString(menuSmallFont, "Arena", statusDisplayTextPos + statusDisplayRowHeight * 4, Color.White);
            spriteBatch.DrawString(menuSmallFont, statusDisplayArenaName, statusDisplayTextPos + statusDisplayRowHeight * 5, Color.GreenYellow);

            // Draw network game statusdisplay texts
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone)
            {
                spriteBatch.DrawString(menuSmallFont, "Status", statusDisplayTextPos + statusDisplayRowHeight, Color.White);
                spriteBatch.DrawString(menuSmallFont, statusDisplayStatus, statusDisplayTextPos + statusDisplayColumnWidth + statusDisplayRowHeight, Color.GreenYellow);
            }

            // Draw client statusdisplay texts
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                spriteBatch.DrawString(menuSmallFont, "Ping", statusDisplayTextPos + statusDisplayRowHeight * 2, Color.White);
                spriteBatch.DrawString(menuSmallFont, statusDisplayPing, statusDisplayTextPos + statusDisplayColumnWidth + statusDisplayRowHeight * 2, Color.GreenYellow);
            }

            // Draw player panes.
            Vector2 player1PanePos = new Vector2(334, 164);
            Vector2 playerPaneDeltaPos = new Vector2(203, 0);
            Vector2 playerPaneMainDeltaPos = new Vector2(0, player1PaneTopTexture.Height);
            Vector2 playerPaneCursorDeltaPos = playerPaneMainDeltaPos + new Vector2(22, 3);
            Vector2 playerPaneIconDeltaPos = playerPaneMainDeltaPos + new Vector2(21, 1);
            Vector2 playerPaneRowDeltaPos = new Vector2(0, 91);
            Vector2 playerPaneNamePos = new Vector2(104, 38);
            int playerI = -1;
            foreach (var player in data.Players)
            {
                if (player.IsRemote) continue;
                ++playerI;

                // Find out things.
                string playerItemName = "???";
                switch (currentItems[playerI])
                {
                    case EquipMenuItem.Ship: playerItemName = player.ShipName; break;
                    case EquipMenuItem.Extra: playerItemName = player.ExtraDeviceName; break;
                    case EquipMenuItem.Weapon2: playerItemName = player.Weapon2Name; break;
                }
                Vector2 playerPanePos = pos - view + player1PanePos + playerI * playerPaneDeltaPos;
                Vector2 playerCursorPos = playerPanePos + playerPaneCursorDeltaPos
                    + (int)currentItems[playerI] * playerPaneRowDeltaPos;
                Vector2 playerNamePos = playerPanePos
                    + new Vector2((int)(104 - menuSmallFont.MeasureString(player.Name).X / 2), 38);
                Vector2 playerItemNamePos = playerPanePos
                    + new Vector2((int)(104 - menuSmallFont.MeasureString(playerItemName).X / 2), 355);
                Texture2D playerPaneTopTexture = playerI == 1 ? player2PaneTopTexture : player1PaneTopTexture;

                // Draw pane background.
                spriteBatch.Draw(playerPaneTopTexture, playerPanePos, Color.White);
                spriteBatch.Draw(playerPaneTexture, playerPanePos + playerPaneMainDeltaPos, Color.White);
                spriteBatch.DrawString(menuSmallFont, player.Name, playerNamePos, Color.White);

                // Draw icons of selected equipment.
                var ship = (Game.Gobs.Ship)data.GetTypeTemplate(player.ShipName);
                var extraDevice = (ShipDevice)data.GetTypeTemplate(player.ExtraDeviceName);
                var weapon2 = (Weapon)data.GetTypeTemplate(player.Weapon2Name);
                Texture2D shipTexture = AssaultWing.Instance.Content.Load<Texture2D>(ship.IconEquipName);
                Texture2D extraDeviceTexture = AssaultWing.Instance.Content.Load<Texture2D>(extraDevice.IconEquipName);
                Texture2D weapon2Texture = AssaultWing.Instance.Content.Load<Texture2D>(weapon2.IconEquipName);
                spriteBatch.Draw(shipTexture, playerPanePos + playerPaneCursorDeltaPos, Color.White);
                spriteBatch.Draw(extraDeviceTexture, playerPanePos + playerPaneCursorDeltaPos + playerPaneRowDeltaPos, Color.White);
                spriteBatch.Draw(weapon2Texture, playerPanePos + playerPaneCursorDeltaPos + 2 * playerPaneRowDeltaPos, Color.White);

                // Draw cursor, highlight and item name.
                float cursorTime = (float)(AssaultWing.Instance.GameTime.TotalRealTime - cursorFadeStartTimes[playerI]).TotalSeconds;
                spriteBatch.Draw(highlightMainTexture, playerCursorPos, Color.White);
                spriteBatch.Draw(cursorMainTexture, playerCursorPos, new Color(255, 255, 255, (byte)cursorFade.Evaluate(cursorTime)));
                spriteBatch.DrawString(menuSmallFont, playerItemName, playerItemNamePos, Color.White);
            }

            // Draw network game status pane.
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone)
            {
                // Draw pane background.
                Vector2 statusPanePos = pos - view + new Vector2(537, 160);
                spriteBatch.Draw(statusPaneTexture, statusPanePos, Color.White);

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
                spriteBatch.DrawString(menuBigFont, textContent, textPos, Color.White);
            }
        }
    }
}
