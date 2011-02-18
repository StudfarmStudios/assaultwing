using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.UI;

namespace AW2.Menu.Main
{
    public class MainMenuTextField : MainMenuItem
    {
        private EditableText _text;

        public MainMenuTextField(MenuEngineImpl menuEngine, Func<string> getName, Action<MainMenuComponent> action, EditableText text)
            : base(menuEngine, getName, action)
        {
            _text = text;
        }

        public override void Update()
        {
            _text.Update(_menuEngine.ResetCursorFade);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 origin)
        {
            Draw(spriteBatch, origin, Name() + _text.Content);
        }

        public override void DrawHighlight(SpriteBatch spriteBatch, Vector2 origin)
        {
            float partialTextWidth = Font.MeasureString(Name() + _text.Content.Substring(0, _text.CaretPosition)).X;
            var cursorDelta = new Vector2(34 - 7 + partialTextWidth, 0);
            DrawHighlight(spriteBatch, origin, cursorDelta);
        }
    }
}
