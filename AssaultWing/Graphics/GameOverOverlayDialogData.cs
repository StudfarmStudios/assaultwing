using AW2.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Graphics
{
    /// <summary>
    /// Content and action data for an overlay dialog displaying standings after
    /// a finished game session.
    /// </summary>
    public class GameOverOverlayDialogData : OverlayDialogData
    {
        private SpriteFont _fontHuge, _fontBig, _fontSmall;

        public GameOverOverlayDialogData()
            : base(new TriggeredCallback(TriggeredCallback.GetProceedControl(), AssaultWing.Instance.ShowMenu))
        {
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            float textLeftEdge = 100; // left edge of left-aligned text
            Vector2 textCenter = new Vector2(gfx.Viewport.Width / 2, 50); // text line top center
            Vector2 textSize = _fontHuge.MeasureString("Game Over");
            spriteBatch.DrawString(_fontHuge, "Game Over", textCenter - new Vector2(textSize.X / 2, 0), Color.White);
            textCenter += new Vector2(0, 2 * _fontHuge.LineSpacing);

            var data = AssaultWing.Instance.DataEngine;
            var standings = data.GameplayMode.GetStandings(data.Players);
            int line = 0;
            foreach (var entry in standings)
            {
                Vector2 textPos = new Vector2(textLeftEdge, textCenter.Y);
                string scoreText = string.Format("{0} = {1}-{2}", entry.Score, entry.Kills, entry.Suicides);
                spriteBatch.DrawString(_fontSmall, (line + 1) + ". " + entry.Name, textPos, Color.White);
                spriteBatch.DrawString(_fontSmall, scoreText, textPos + new Vector2(250, 0), Color.White);
                textCenter += new Vector2(0, _fontSmall.LineSpacing);
                ++line;
            }
            
            textCenter += new Vector2(0, 2 * _fontSmall.LineSpacing);
            textSize = _fontSmall.MeasureString("Press Enter to return to Main Menu");
            spriteBatch.DrawString(_fontSmall, "Press Enter to return to Main Menu", textCenter - new Vector2(textSize.X / 2, 0), Color.White);
        }

        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            _fontHuge = content.Load<SpriteFont>("MenuFontHuge");
            _fontBig = content.Load<SpriteFont>("MenuFontBig");
            _fontSmall = content.Load<SpriteFont>("MenuFontSmall");
        }
    }
}
