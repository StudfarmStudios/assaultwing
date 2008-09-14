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
    class EquipMenuComponent : MenuComponent
    {
        Control controlBack, controlDone;
        Vector2 pos; // position of the component's background texture in menu system coordinates
        SpriteFont menuBigFont, menuSmallFont;
        Texture2D backgroundTexture;

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
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            controlDone = new KeyboardKey(Keys.Enter);
            controlBack = new KeyboardKey(Keys.Escape);
            pos = new Vector2(0, 0);
            menuBigFont = data.GetFont(FontName.MenuFontBig);
            menuSmallFont = data.GetFont(FontName.MenuFontSmall);
            backgroundTexture = data.GetTexture(TextureName.EquipMenuBackground);
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

                // TODO: React to player controls.
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
            spriteBatch.Draw(backgroundTexture, pos - view, Color.White);
        }
    }
}
