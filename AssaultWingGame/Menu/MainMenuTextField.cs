using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.UI;

namespace AW2.Menu
{
    public class MainMenuTextField : MainMenuItem
    {
        private EditableText _text;

        public MainMenuTextField(MenuEngineImpl menuEngine, EditableText text)
            : base(menuEngine)
        {
            _text = text;
        }

        public override void Update()
        {
            _text.Update(_menuEngine.ResetCursorFade);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 pos)
        {
            spriteBatch.DrawString(_menuEngine.MenuContent.FontBig, Name + _text.Content, pos, Color.White);
        }

        public override void DrawHighlight(SpriteBatch spriteBatch, Vector2 pos)
        {
            var font = _menuEngine.MenuContent.FontBig;
            var partialTextSize = new Vector2(34, 1) + font.MeasureString(Name + _text.Content.Substring(0, _text.CaretPosition));
            var cursorDelta = new Vector2(partialTextSize.X, 0);
            DrawHighlight(spriteBatch, pos + cursorDelta, pos);
        }
    }
}
