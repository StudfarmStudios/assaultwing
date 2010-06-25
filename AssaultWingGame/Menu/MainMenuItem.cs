using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Menu
{
    /// <summary>
    /// An item in the main menu, consisting of a visible name and an action
    /// to trigger when the item is selected.
    /// </summary>
    public class MainMenuItem
    {
        protected MenuEngineImpl _menuEngine;

        /// <summary>
        /// Visible name of the menu item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Action to perform on triggering the menu item.
        /// </summary>
        public Action<MainMenuComponent> Action { get; set; }

        public MainMenuItem(MenuEngineImpl menuEngine)
        {
            _menuEngine = menuEngine;
        }

        public virtual void Update() { }

        public virtual void Draw(SpriteBatch spriteBatch, Vector2 pos)
        {
            spriteBatch.DrawString(_menuEngine.MenuContent.FontBig, Name, pos, Color.White);
        }

        public virtual void DrawHighlight(SpriteBatch spriteBatch, Vector2 pos)
        {
            DrawHighlight(spriteBatch, pos, pos);
        }

        protected void DrawHighlight(SpriteBatch spriteBatch, Vector2 cursorPos, Vector2 highlightPos)
        {
            spriteBatch.Draw(_menuEngine.MenuContent.MainCursor, cursorPos, new Color(1, 1, 1, _menuEngine.GetCursorFade()));
            spriteBatch.Draw(_menuEngine.MenuContent.MainHighlight, highlightPos, Color.White);
        }
    }
}
