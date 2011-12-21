using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics;
using AW2.Helpers;

namespace AW2.Menu.Main
{
    /// <summary>
    /// An item in the main menu, consisting of a visible name and an action
    /// to trigger when the item is selected.
    /// </summary>
    public class MainMenuItem
    {
        private MenuEngineImpl _menuEngine;

        /// <summary>
        /// Index of the menu item in a menu item collection. Set by the menu item collection.
        /// </summary>
        public int ItemIndex { get; set; }

        /// <summary>
        /// Visible name of the menu item.
        /// </summary>
        public Func<string> Name { get; protected set; }

        /// <summary>
        /// Action to perform on triggering the menu item or pressing Right on the menu item.
        /// </summary>
        public Action Action { get; private set; }

        /// <summary>
        /// Action to perform on pressing Left on the menu item.
        /// </summary>
        public Action ActionLeft { get; private set; }

        protected SpriteFont Font { get { return _menuEngine.MenuContent.FontBig; } }

        public MainMenuItem(MenuEngineImpl menuEngine, Func<string> getName, Action action)
            : this(menuEngine, getName, action, () => { })
        {
        }

        public MainMenuItem(MenuEngineImpl menuEngine, Func<string> getName, Action action, Action actionLeft)
        {
            _menuEngine = menuEngine;
            Name = getName;
            Action = action;
            ActionLeft = actionLeft;
        }

        public void Update() { }

        public virtual void Draw(SpriteBatch spriteBatch, Vector2 origin, int visibleIndex)
        {
            GraphicsEngineImpl.DrawFormattedText(origin, _menuEngine.MenuContent.FontBigEnWidth, Name(),
                (textPos, text) => Draw(spriteBatch, textPos, visibleIndex, text));
        }

        public void DrawHighlight(SpriteBatch spriteBatch, Vector2 origin, int visibleIndex)
        {
            DrawHighlight(spriteBatch, origin, Vector2.Zero, visibleIndex);
        }

        private void Draw(SpriteBatch spriteBatch, Vector2 origin, int visibleIndex, string text)
        {
            var highlightToTextDelta = new Vector2(34, 1);
            var pos = GetHighlightPos(origin, visibleIndex) + highlightToTextDelta;
            spriteBatch.DrawString(_menuEngine.MenuContent.FontBig, text, pos.Round(), Color.White);
        }

        protected virtual void DrawHighlight(SpriteBatch spriteBatch, Vector2 origin, Vector2 cursorDelta, int visibleIndex)
        {
            var highlightPos = GetHighlightPos(origin, visibleIndex);
            var cursorPos = highlightPos + cursorDelta;
            spriteBatch.Draw(_menuEngine.MenuContent.MainCursor, cursorPos, Color.Multiply(Color.White, _menuEngine.GetCursorFade()));
            spriteBatch.Draw(_menuEngine.MenuContent.MainHighlight, highlightPos, Color.White);
        }

        private Vector2 GetHighlightPos(Vector2 origin, int visibleIndex)
        {
            var highlightDelta = new Vector2(551, 354);
            var lineDelta = new Vector2(0, Font.LineSpacing);
            return origin + highlightDelta + visibleIndex * lineDelta;
        }
    }
}
