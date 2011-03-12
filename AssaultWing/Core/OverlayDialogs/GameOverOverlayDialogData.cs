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
        public GameOverOverlayDialogData(AssaultWing game)
            : base(game, new TriggeredCallback(TriggeredCallback.PROCEED_CONTROL, () => { }))
        {
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            var fontHuge = Game.MenuEngine.MenuContent.FontHuge;
            var fontSmall = Game.MenuEngine.MenuContent.FontSmall;
            var textLeftEdge = 100f; // left edge of left-aligned text
            var textCenter = new Vector2(gfx.Viewport.Width / 2, 50); // text line top center
            var titleText = "Game Over";
            var titleSize = fontHuge.MeasureString(titleText);
            var titlePos = textCenter - new Vector2(titleSize.X / 2, 0);
            spriteBatch.DrawString(fontHuge, titleText, titlePos.Round(), Color.White);
            textCenter += new Vector2(0, 2 * fontHuge.LineSpacing);

            var data = AssaultWingCore.Instance.DataEngine;
            var standings = data.GameplayMode.GetStandings(data.Players);
            int line = 0;
            foreach (var entry in standings)
            {
                line++;
                var column1Pos = new Vector2(textLeftEdge, textCenter.Y);
                var column2Pos = column1Pos + new Vector2(250, 0);
                var scoreText = string.Format("{0} = {1}-{2}", entry.Score, entry.Kills, entry.Suicides);
                spriteBatch.DrawString(fontSmall, line + ". " + entry.Name, column1Pos.Round(), Color.White);
                spriteBatch.DrawString(fontSmall, scoreText, column2Pos.Round(), Color.White);
                textCenter += new Vector2(0, fontSmall.LineSpacing);
            }

            textCenter += new Vector2(0, 2 * fontSmall.LineSpacing);
            var infoText = "Press Enter";
            var infoSize = fontSmall.MeasureString(infoText);
            var infoPos = textCenter - new Vector2(infoSize.X / 2, 0);
            spriteBatch.DrawString(fontSmall, infoText, infoPos.Round(), Color.White);
        }
    }
}
