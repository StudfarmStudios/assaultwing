using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.UI;

namespace AW2.Menu.Main
{
    public class MainMenuTextField : MainMenuItem
    {
        private EditableText _text;

        public MainMenuTextField(MenuEngineImpl menuEngine, Func<string> getName, Action action, EditableText text)
            : base(menuEngine, null, action)
        {
            _text = text;
            Name = () => getName() + _text.Content;
        }

        protected override void DrawHighlight(SpriteBatch spriteBatch, Vector2 origin, Vector2 cursorDelta, int visibleIndex)
        {
            base.DrawHighlight(spriteBatch, origin, cursorDelta, visibleIndex);
            // TODO: Draw text caret.
        }
    }
}
