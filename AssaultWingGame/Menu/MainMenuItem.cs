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
        /// Index of the menu item in a menu item collection. Set by the menu item collection.
        /// </summary>
        public int ItemIndex { get; set; }

        /// <summary>
        /// Visible name of the menu item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Action to perform on triggering the menu item.
        /// </summary>
        public Action<MainMenuComponent> Action { get; set; }

        protected SpriteFont Font { get { return _menuEngine.MenuContent.FontBig; } }

        public MainMenuItem(MenuEngineImpl menuEngine)
        {
            _menuEngine = menuEngine;
        }

        public virtual void Update() { }

        public virtual void Draw(SpriteBatch spriteBatch, Vector2 origin)
        {
            Draw(spriteBatch, origin, Name);
        }

        public virtual void DrawHighlight(SpriteBatch spriteBatch, Vector2 origin)
        {
            DrawHighlight(spriteBatch, origin, Vector2.Zero);
        }

        protected void Draw(SpriteBatch spriteBatch, Vector2 origin, string text)
        {
            var highlightToTextDelta = new Vector2(34, 1);
            var pos = GetHighlightPos(origin) + highlightToTextDelta;
            spriteBatch.DrawString(_menuEngine.MenuContent.FontBig, text, pos, Color.White);
        }

        protected void DrawHighlight(SpriteBatch spriteBatch, Vector2 origin, Vector2 cursorDelta)
        {
            var highlightPos = GetHighlightPos(origin);
            var cursorPos = highlightPos + cursorDelta;
            spriteBatch.Draw(_menuEngine.MenuContent.MainCursor, cursorPos, Color.Multiply(Color.White, _menuEngine.GetCursorFade()));
            spriteBatch.Draw(_menuEngine.MenuContent.MainHighlight, highlightPos, Color.White);
        }

        private Vector2 GetHighlightPos(Vector2 origin)
        {
            var highlightDelta = new Vector2(551, 354);
            var lineDelta = new Vector2(0, Font.LineSpacing);
            return origin + highlightDelta + ItemIndex * lineDelta;
        }
    }
}
