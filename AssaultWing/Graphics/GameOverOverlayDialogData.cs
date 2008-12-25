using System;
using System.Collections.Generic;
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
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            float textLeftEdge = 100; // left edge of left-aligned text
            Vector2 textCenter = new Vector2(gfx.Viewport.Width / 2, 50); // text line top center
            Vector2 textSize = fontHuge.MeasureString("Game Over");
            spriteBatch.DrawString(fontHuge, "Game Over", textCenter - new Vector2(textSize.X / 2, 0), Color.White);
            textCenter += new Vector2(0, 2 * fontHuge.LineSpacing);


            // List players and their scores sorted decreasing by score.
            List<int> playerIds = new List<int>();
            List<int> playerScores = new List<int>();
            data.ForEachPlayer(player =>
            {
                playerIds.Add(player.Id);
                playerScores.Add(player.Kills - player.Suicides);
            });
            playerIds.Sort((i, j) => Math.Sign(playerScores[j] - playerScores[i]));
            for (int i = 0; i < playerIds.Count; ++i)
            {
                Player player = data.GetPlayer(playerIds[i]);
                Vector2 textPos = new Vector2(textLeftEdge, textCenter.Y);
                string scoreText = string.Format("{0} = {1}-{2}", player.Kills - player.Suicides, player.Kills, player.Suicides);
                spriteBatch.DrawString(fontSmall, (i + 1) + ". " + player.Name, textPos, Color.White);
                spriteBatch.DrawString(fontSmall, scoreText, textPos + new Vector2(250, 0), Color.White);
                textCenter += new Vector2(0, fontSmall.LineSpacing);
            }
            
            textCenter += new Vector2(0, 2 * fontSmall.LineSpacing);
            textSize = fontSmall.MeasureString("Press Enter to return to Main Menu");
            spriteBatch.DrawString(fontSmall, "Press Enter to return to Main Menu", textCenter - new Vector2(textSize.X / 2, 0), Color.White);
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            fontHuge = data.GetFont(FontName.MenuFontHuge);
            fontBig = data.GetFont(FontName.MenuFontBig);
            fontSmall = data.GetFont(FontName.MenuFontSmall);
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public override void UnloadContent()
        {
            // Our textures are disposed by the graphics engine.
        }
    }
}
