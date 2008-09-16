using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AW2.Game;
using AW2.Graphics;
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
        Control controlBack, controlDone;
        Vector2 pos; // position of the component's background texture in menu system coordinates
        SpriteFont menuBigFont, menuSmallFont;
        Texture2D backgroundTexture;
        Texture2D cursorMainTexture, highlightMainTexture;
        Texture2D playerPaneTexture, player1PaneTopTexture, player2PaneTopTexture;

        /// <summary>
        /// Index of current item each player's pane main display.
        /// </summary>
        int[] currentItems;

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
            currentItems = new int[4];

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
                if (controlBack.Pulse)
                    menuEngine.ActivateComponent(MenuComponentType.Main);
                else if (controlDone.Pulse)
                    menuEngine.ActivateComponent(MenuComponentType.Arena);

                // React to players' controls.
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                for (int playerI = 0; ; ++playerI)
                {
                    Player player = data.GetPlayer(playerI);
                    if (player == null) break;
                    if (player.Controls.thrust.Pulse)
                    {
                        if (currentItems[playerI] > 0) --currentItems[playerI];
                    }
                    if (player.Controls.down.Pulse)
                    {
                        if (currentItems[playerI] < 2) ++currentItems[playerI];
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
            for (int playerI = 0; ; ++playerI)
            {
                Player player = data.GetPlayer(playerI);
                if (player == null) break;
                Vector2 playerPanePos = pos - view + player1PanePos + playerI * playerPaneDeltaPos;
                Vector2 playerCursorPos = playerPanePos + playerPaneCursorDeltaPos
                    + currentItems[playerI] * playerPaneRowDeltaPos;
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

                spriteBatch.Draw(highlightMainTexture, playerCursorPos, Color.White);
                spriteBatch.Draw(cursorMainTexture, playerCursorPos, Color.White);
            }
        }
    }
}
