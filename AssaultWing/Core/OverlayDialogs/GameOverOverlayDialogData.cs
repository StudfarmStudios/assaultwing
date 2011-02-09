using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.UI;

namespace AW2.Core.OverlayDialogs
{
    /// <summary>
    /// Content and action data for an overlay dialog displaying standings after
    /// a finished game session.
    /// </summary>
    public class GameOverOverlayDialogData : OverlayDialogData
    {
        private SpriteFont _fontHuge, _fontBig, _fontSmall;

        public GameOverOverlayDialogData(AssaultWing game)
            : base(game, new TriggeredCallback(TriggeredCallback.GetProceedControl(), game.ShowMenu))
        {
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            var textLeftEdge = 100f; // left edge of left-aligned text
            var textCenter = new Vector2(gfx.Viewport.Width / 2, 50); // text line top center
            var titleSize = _fontHuge.MeasureString("Game Over");
            var titlePos = textCenter - new Vector2(titleSize.X / 2, 0);
            spriteBatch.DrawString(_fontHuge, "Game Over", titlePos.Round(), Color.White);
            textCenter += new Vector2(0, 2 * _fontHuge.LineSpacing);

            var data = AssaultWingCore.Instance.DataEngine;
            var standings = data.GameplayMode.GetStandings(data.Players);
            int line = 0;
            foreach (var entry in standings)
            {
                line++;
                var column1Pos = new Vector2(textLeftEdge, textCenter.Y);
                var column2Pos = column1Pos + new Vector2(250, 0);
                var scoreText = string.Format("{0} = {1}-{2}", entry.Score, entry.Kills, entry.Suicides);
                spriteBatch.DrawString(_fontSmall, line + ". " + entry.Name, column1Pos.Round(), Color.White);
                spriteBatch.DrawString(_fontSmall, scoreText, column2Pos.Round(), Color.White);
                textCenter += new Vector2(0, _fontSmall.LineSpacing);
            }
            
            textCenter += new Vector2(0, 2 * _fontSmall.LineSpacing);
            var infoSize = _fontSmall.MeasureString("Press Enter to return to Main Menu");
            var infoPos = textCenter - new Vector2(infoSize.X / 2, 0);
            spriteBatch.DrawString(_fontSmall, "Press Enter to return to Main Menu", infoPos.Round(), Color.White);
        }

        public override void LoadContent()
        {
            var content = AssaultWingCore.Instance.Content;
            _fontHuge = content.Load<SpriteFont>("MenuFontHuge");
            _fontBig = content.Load<SpriteFont>("MenuFontBig");
            _fontSmall = content.Load<SpriteFont>("MenuFontSmall");
        }
    }
}
