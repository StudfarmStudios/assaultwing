using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Graphics;
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
        enum EquipMenuItem
        {
            /// <summary>
            /// Start a local play session.
            /// </summary>
            Ship,

            /// <summary>
            /// Start a network play session.
            /// </summary>
            Weapon1,

            /// <summary>
            /// Set up Assault Wing's technical thingies.
            /// </summary>
            Weapon2,

            /// <summary>
            /// The first item in the main menu.
            /// </summary>
            _FirstItem = Ship,

            /// <summary>
            /// The last item in the main menu.
            /// </summary>
            _LastItem = Weapon2,
        }

        Control controlBack, controlDone;
        Vector2 pos; // position of the component's background texture in menu system coordinates
        SpriteFont menuBigFont, menuSmallFont;
        Texture2D backgroundTexture;
        Texture2D cursorMainTexture, highlightMainTexture;
        Texture2D playerPaneTexture, player1PaneTopTexture, player2PaneTopTexture;
        Texture2D statusPaneTexture;

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
                }
            }
        }

        /// <summary>
        /// The center of the menu component in menu system coordinates.
        /// </summary>
        /// This is a good place to center the menu view to when the menu component
        /// is to be seen well on the screen.
        public override Vector2 Center { get { return pos + new Vector2(750, 480); } }

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
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            menuBigFont = data.GetFont(FontName.MenuFontBig);
            menuSmallFont = data.GetFont(FontName.MenuFontSmall);
            backgroundTexture = data.GetTexture(TextureName.EquipMenuBackground);
            cursorMainTexture = data.GetTexture(TextureName.EquipMenuCursorMain);
            highlightMainTexture = data.GetTexture(TextureName.EquipMenuHighlightMain);
            playerPaneTexture = data.GetTexture(TextureName.EquipMenuPlayerBackground);
            player1PaneTopTexture = data.GetTexture(TextureName.EquipMenuPlayerTop1);
            player2PaneTopTexture = data.GetTexture(TextureName.EquipMenuPlayerTop2);
            statusPaneTexture = data.GetTexture(TextureName.EquipMenuStatusDisplay);
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
            // Check our controls and react to them.
            if (Active)
            {
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                NetworkEngine net = (NetworkEngine)AssaultWing.Instance.Services.GetService(typeof(NetworkEngine));

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

                            // We don't accept input while an arena is loading.
                            Active = false;
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

                // React to players' controls.
                int playerI = -1;
                data.ForEachPlayer(player =>
                {
                    if (player.IsRemote) return;
                    ++playerI;
                    if (player.Controls.thrust.Pulse)
                    {
                        cursorFadeStartTimes[playerI] = AssaultWing.Instance.GameTime.TotalRealTime;
                        if (currentItems[playerI] != EquipMenuItem._FirstItem) --currentItems[playerI];
                    }
                    if (player.Controls.down.Pulse)
                    {
                        cursorFadeStartTimes[playerI] = AssaultWing.Instance.GameTime.TotalRealTime;
                        if (currentItems[playerI] != EquipMenuItem._LastItem) ++currentItems[playerI];
                    }
                    if (player.Controls.fire1.Pulse)
                    {
                        cursorFadeStartTimes[playerI] = AssaultWing.Instance.GameTime.TotalRealTime;
                        string[] shipNames = { "Hyperion", "Prowler" };
                        string[] weapon1Names = { "peashooter", "shotgun" };
                        string[] weapon2Names = { "bazooka", "rockets" };
                        switch (currentItems[playerI])
                        {
                            case EquipMenuItem.Ship:
                                {
                                    int currentI = 0;
                                    for (int i = 0; i < shipNames.Length; ++i)
                                        if (shipNames[i] == player.ShipName)
                                        {
                                            currentI = i;
                                            break;
                                        }
                                    player.ShipName = shipNames[(currentI + 1) % shipNames.Length];
                                } break;
                            case EquipMenuItem.Weapon1:
                                {
                                    int currentI = 0;
                                    for (int i = 0; i < weapon1Names.Length; ++i)
                                        if (weapon1Names[i] == player.Weapon1Name)
                                        {
                                            currentI = i;
                                            break;
                                        }
                                    player.Weapon1Name = weapon1Names[(currentI + 1) % weapon1Names.Length];
                                } break;
                            case EquipMenuItem.Weapon2:
                                {
                                    int currentI = 0;
                                    for (int i = 0; i < weapon2Names.Length; ++i)
                                        if (weapon2Names[i] == player.Weapon2Name)
                                        {
                                            currentI = i;
                                            break;
                                        }
                                    player.Weapon2Name = weapon2Names[(currentI + 1) % weapon2Names.Length];
                                } break;
                        }

                        // Send new equipment choices to the game server.
                        var equipUpdateRequest = new JoinGameRequest();
                        equipUpdateRequest.PlayerInfos = new List<PlayerInfo> { new PlayerInfo(player) };
                        net.SendToServer(equipUpdateRequest);
                    }
                });

                // React to network messages.
                if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                {
                    // Handle JoinGameRequests from game clients.
                    JoinGameRequest message = null;
                    while ((message = net.ReceiveFromClients<JoinGameRequest>()) != null)
                    {
                        // Send player ID changes for new players, if any. A join game request
                        // may also update the chosen equipment of a previously added player.
                        JoinGameReply reply = new JoinGameReply();
                        var playerIdChanges = new List<JoinGameReply.IdChange>();
                        foreach (PlayerInfo info in message.PlayerInfos)
                        {
                            var oldPlayer = data.TryGetPlayer(plr => plr.ConnectionId == message.ConnectionId && plr.Id == info.id);
                            if (oldPlayer != null)
                            {
                                oldPlayer.Name = info.name;
                                oldPlayer.ShipName = info.shipTypeName;
                                oldPlayer.Weapon1Name = info.weapon1TypeName;
                                oldPlayer.Weapon2Name = info.weapon2TypeName;
                            }
                            else
                            {
                                Player player = new Player(info.name, info.shipTypeName, info.weapon1TypeName, info.weapon2TypeName, message.ConnectionId);
                                data.AddPlayer(player);
                                playerIdChanges.Add(new JoinGameReply.IdChange { oldId = info.id, newId = player.Id });
                            }
                        }
                        if (playerIdChanges.Count > 0)
                        {
                            reply.PlayerIdChanges = playerIdChanges.ToArray();
                            net.SendToClient(message.ConnectionId, reply);
                        }
                    }
                }
                if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
                {
                    var message = net.ReceiveFromServer<StartGameMessage>();
                    if (message != null)
                    {
                        for (int i = 0; i < message.PlayerCount; ++i)
                        {
                            Player player = new Player("uninitialised", "uninitialised", "uninitialised", "uninitialised", 0x7ea1eaf);
                            message.Read(player, SerializationModeFlags.All);

                            // Only add the player if it is remote.
                            if (data.GetPlayer(player.Id) == null)
                                data.AddPlayer(player);
                        }

                        data.ArenaPlaylist = message.ArenaPlaylist;

                        // Prepare and start playing the game.
                        menuEngine.ProgressBarAction(
                            AssaultWing.Instance.PrepareFirstArena,
                            AssaultWing.Instance.StartArena);

                        // We don't accept input while an arena is loading.
                        Active = false;
                    }
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
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            spriteBatch.Draw(backgroundTexture, pos - view, Color.White);

            // Draw player panes.
            Vector2 player1PanePos = new Vector2(334, 164);
            Vector2 playerPaneDeltaPos = new Vector2(203, 0);
            Vector2 playerPaneMainDeltaPos = new Vector2(0, player1PaneTopTexture.Height);
            Vector2 playerPaneCursorDeltaPos = playerPaneMainDeltaPos + new Vector2(22, 3);
            Vector2 playerPaneIconDeltaPos = playerPaneMainDeltaPos + new Vector2(21, 1);
            Vector2 playerPaneRowDeltaPos = new Vector2(0, 91);
            Vector2 playerPaneNamePos = new Vector2(104, 30);
            int playerI = -1;
            data.ForEachPlayer(player =>
            {
                if (player.IsRemote) return;
                ++playerI;

                // Draw pane background.
                Vector2 playerPanePos = pos - view + player1PanePos + playerI * playerPaneDeltaPos;
                Vector2 playerCursorPos = playerPanePos + playerPaneCursorDeltaPos
                    + (int)currentItems[playerI] * playerPaneRowDeltaPos;
                Vector2 playerNamePos = playerPanePos
                    + new Vector2((int)(104 - menuSmallFont.MeasureString(player.Name).X / 2), 30);
                Texture2D playerPaneTopTexture = playerI == 1 ? player2PaneTopTexture : player1PaneTopTexture;
                spriteBatch.Draw(playerPaneTopTexture, playerPanePos, Color.White);
                spriteBatch.Draw(playerPaneTexture, playerPanePos + playerPaneMainDeltaPos, Color.White);
                spriteBatch.DrawString(menuSmallFont, player.Name, playerNamePos, Color.White);

                // Draw icons of selected equipment.
                Game.Gobs.Ship ship = (Game.Gobs.Ship)data.GetTypeTemplate(typeof(Gob), player.ShipName);
                Weapon weapon1 = (Weapon)data.GetTypeTemplate(typeof(Weapon), player.Weapon1Name);
                Weapon weapon2 = (Weapon)data.GetTypeTemplate(typeof(Weapon), player.Weapon2Name);
                Texture2D shipTexture = data.GetTexture(ship.IconEquipName);
                Texture2D weapon1Texture = data.GetTexture(weapon1.IconEquipName);
                Texture2D weapon2Texture = data.GetTexture(weapon2.IconEquipName);
                spriteBatch.Draw(shipTexture, playerPanePos + playerPaneCursorDeltaPos, Color.White);
                spriteBatch.Draw(weapon1Texture, playerPanePos + playerPaneCursorDeltaPos + playerPaneRowDeltaPos, Color.White);
                spriteBatch.Draw(weapon2Texture, playerPanePos + playerPaneCursorDeltaPos + 2 * playerPaneRowDeltaPos, Color.White);

                // Draw cursor and highlight.
                float cursorTime = (float)(AssaultWing.Instance.GameTime.TotalRealTime - cursorFadeStartTimes[playerI]).TotalSeconds;
                spriteBatch.Draw(highlightMainTexture, playerCursorPos, Color.White);
                spriteBatch.Draw(cursorMainTexture, playerCursorPos, new Color(255, 255, 255, (byte)cursorFade.Evaluate(cursorTime)));
            });

            // Draw network game status pane.
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Standalone)
            {
                // Draw pane background.
                Vector2 statusPanePos = pos - view + new Vector2(537, 160);
                spriteBatch.Draw(statusPaneTexture, statusPanePos, Color.White);

                // Draw pane content text.
                int playerCount = 0;
                data.ForEachPlayer(player => ++playerCount);
                Vector2 textPos = statusPanePos + new Vector2(60, 60);
                string textContent = AssaultWing.Instance.NetworkMode == NetworkMode.Client
                    ? "Connected to game server"
                    : "Hosting a game as server";
                textContent += "\n\n" + playerCount + (playerCount == 1 ? " player" : " players");
                textContent += "\n\nArena: " + data.ArenaPlaylist[0];
                spriteBatch.DrawString(menuBigFont, textContent, textPos, Color.White);
            }
        }
    }
}
