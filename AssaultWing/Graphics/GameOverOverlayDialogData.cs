using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.UI;

namespace AW2.Graphics
{
    /// <summary>
    /// Content and action data for an overlay dialog displaying standings after
    /// a finished game session.
    /// </summary>
    public class GameOverOverlayDialogData : OverlayDialogData
    {
        SpriteFont fontHuge, fontBig, fontSmall;

        /// <summary>
        /// Creates contents for an overlay dialog displaying game over.
        /// </summary>
        public GameOverOverlayDialogData()
            : base(new TriggeredCallback(TriggeredCallback.GetProceedControl(), delegate() { AssaultWing.Instance.ShowMenu(); }))
        {
        }

        /// <summary>
        /// Draws the overlay graphics component using the guarantee that the
        /// graphics device's viewport is set to the exact area needed by the component.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to use. <c>Begin</c> is assumed
        /// to have been called and <c>End</c> is assumed to be called after this
        /// method returns.</param>
        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            float textLeftEdge = 100; // left edge of left-aligned text
            Vector2 textCenter = new Vector2(gfx.Viewport.Width / 2, 50); // text line top center
            Vector2 textSize = fontHuge.MeasureString("Game Over");
            spriteBatch.DrawString(fontHuge, "Game Over", textCenter - new Vector2(textSize.X / 2, 0), Color.White);
            textCenter += new Vector2(0, 2 * fontHuge.LineSpacing);

            var data = AssaultWing.Instance.DataEngine;
            var standings = data.GameplayMode.GetStandings(data.Players);
            int line = 0;
            foreach (var entry in standings)
            {
                Vector2 textPos = new Vector2(textLeftEdge, textCenter.Y);
                string scoreText = string.Format("{0} = {1}-{2}", entry.Score, entry.Kills, entry.Suicides);
                spriteBatch.DrawString(fontSmall, (line + 1) + ". " + entry.Name, textPos, Color.White);
                spriteBatch.DrawString(fontSmall, scoreText, textPos + new Vector2(250, 0), Color.White);
                textCenter += new Vector2(0, fontSmall.LineSpacing);
                ++line;
            }
            
            textCenter += new Vector2(0, 2 * fontSmall.LineSpacing);
            textSize = fontSmall.MeasureString("Press Enter to return to Main Menu");
            spriteBatch.DrawString(fontSmall, "Press Enter to return to Main Menu", textCenter - new Vector2(textSize.X / 2, 0), Color.White);
        }

        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            fontHuge = content.Load<SpriteFont>("MenuFontHuge");
            fontBig = content.Load<SpriteFont>("MenuFontBig");
            fontSmall = content.Load<SpriteFont>("MenuFontSmall");
        }
    }
}
