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
        public MenuEngineImpl MenuEngine { get; private set; }

        /// <summary>
        /// Index of the menu item in a menu item collection. Set by the menu item collection.
        /// </summary>
        public int ItemIndex { get; set; }

        /// <summary>
        /// Visible name of the menu item.
        /// </summary>
        public Func<string> Name { get; protected set; }

        /// <summary>
        /// Action to perform on triggering the menu item.
        /// </summary>
        public Action Action { get; private set; }

        /// <summary>
        /// Action to perform on pressing Left on the menu item.
        /// </summary>
        public Action ActionLeft { get; private set; }

        /// <summary>
        /// Action to perform on pressing Right on the menu item.
        /// </summary>
        public Action ActionRight { get; private set; }

        protected SpriteFont Font { get { return MenuEngine.MenuContent.FontBig; } }

        public MainMenuItem(MenuEngineImpl menuEngine, Func<string> getName, Action action)
            : this(menuEngine, getName, action, () => { }, () => { })
        {
        }

        public MainMenuItem(MenuEngineImpl menuEngine, Func<string> getName, Action action, Action actionLeft, Action actionRight)
        {
            MenuEngine = menuEngine;
            Name = getName;
            Action = action;
            ActionLeft = actionLeft;
            ActionRight = actionRight;
        }

        public void Update() { }

        public virtual void Draw(SpriteBatch spriteBatch, Vector2 origin, int visibleIndex)
        {
            GraphicsEngineImpl.DrawFormattedText(origin, MenuEngine.MenuContent.FontBigEnWidth, Name(),
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
            spriteBatch.DrawString(MenuEngine.MenuContent.FontBig, text, Vector2.Round(pos), Color.White);
        }

        protected virtual void DrawHighlight(SpriteBatch spriteBatch, Vector2 origin, Vector2 cursorDelta, int visibleIndex)
        {
            var highlightPos = GetHighlightPos(origin, visibleIndex);
            var cursorPos = highlightPos + cursorDelta;
            spriteBatch.Draw(MenuEngine.MenuContent.MainCursor, cursorPos, Color.Multiply(Color.White, MenuEngine.GetCursorFade()));
            spriteBatch.Draw(MenuEngine.MenuContent.MainHighlight, highlightPos, Color.White);
        }

        protected Vector2 GetHighlightPos(Vector2 origin, int visibleIndex)
        {
            var highlightDelta = new Vector2(551, 354);
            var lineDelta = new Vector2(0, Font.LineSpacing);
            return origin + highlightDelta + visibleIndex * lineDelta;
        }
    }
}
