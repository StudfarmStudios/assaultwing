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
    /// a finished arena while loading another arena.
    /// </summary>
    public class ArenaOverOverlayDialogData : OverlayDialogData
    {
        string arenaWinner;

        SpriteFont fontBig, fontSmall;

        /// <summary>
        /// Creates contents for an overlay dialog displaying arena over.
        /// </summary>
        public ArenaOverOverlayDialogData()
            : base(new TriggeredCallback(TriggeredCallback.GetProceedControl(), delegate() { AssaultWing.Instance.PlayNextArena(); }))
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            fontBig = data.GetFont(FontName.MenuFontBig);
            fontSmall = data.GetFont(FontName.MenuFontSmall);

            // Find out the winner
            arenaWinner = "No-one";
            data.ForEachPlayer(delegate(Player player)
            {
                if (player.Lives > 0)
                    arenaWinner = player.Name;
            });
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
            Vector2 textPos = new Vector2(100, 50);
            spriteBatch.DrawString(fontBig, arenaWinner, textPos, Color.White);
            textPos += new Vector2(0, fontBig.LineSpacing);
            spriteBatch.DrawString(fontSmall, "is the winner of the arena", textPos, Color.White);
            textPos += new Vector2(0, fontSmall.LineSpacing + fontBig.LineSpacing);
            spriteBatch.DrawString(fontBig, "Current score:", textPos, Color.White);
            textPos += new Vector2(0, fontBig.LineSpacing);

            // List players and their scores sorted decreasing by score.
            List<int> playerIs = new List<int>();
            List<int> playerScores = new List<int>();
            for (int i = 0; ; ++i)
            {
                Player player = data.GetPlayer(i);
                if (player == null) break;
                playerIs.Add(i);
                playerScores.Add(player.Kills - player.Suicides);
            }
            playerIs.Sort(delegate(int i, int j) { return Math.Sign(playerScores[j] - playerScores[i]); });
            for (int i = 0; i < playerIs.Count; ++i)
            {
                Player player = data.GetPlayer(playerIs[i]);
                string scoreText = string.Format("{0} = {1}-{2}", player.Kills - player.Suicides, player.Kills, player.Suicides);
                spriteBatch.DrawString(fontSmall, (i + 1) + ". " + player.Name, textPos, Color.White);
                spriteBatch.DrawString(fontSmall, scoreText, textPos + new Vector2(250, 0), Color.White);
                textPos += new Vector2(0, fontSmall.LineSpacing);
            }

            textPos += new Vector2(0, fontSmall.LineSpacing);
            spriteBatch.DrawString(fontSmall, "Loading next arena... (when you press Enter)", textPos, Color.White);
        }
    }
}
