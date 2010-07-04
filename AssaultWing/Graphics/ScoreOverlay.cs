using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Game.Gobs;

namespace AW2.Graphics
{
    class ScoreOverlay : OverlayComponent
    {
        private Player _player;
        private Texture2D _scoreBackgroundTexture;
        private SpriteFont _scoreFont;
        private int scoreLineSpacing = 12;

        public override Point Dimensions
        {
            get { return new Point(_scoreBackgroundTexture.Width, 10 + (AssaultWing.Instance.DataEngine.Players.Count() * scoreLineSpacing)); }
        }

        public ScoreOverlay(Player player)
            : base(HorizontalAlignment.Left, VerticalAlignment.Bottom)
        {
            _player = player;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            // Draw scoredisplay background
            spriteBatch.Draw(_scoreBackgroundTexture, Vector2.Zero, Color.White);

            Vector2 textTopLeft = new Vector2(5, 10);
            int numberColumnWidth = 16;
            int playerNameWidth = 115;
            int textMargin = 10;

            var standings = AssaultWing.Instance.DataEngine.GameplayMode.GetStandings(AssaultWing.Instance.DataEngine.Players);
            int line = 0;

            foreach (var entry in standings)
            {
                int currentStanding = line + 1;
                Vector2 standingSize = _scoreFont.MeasureString(currentStanding.ToString());
                Vector2 standingPos = textTopLeft + new Vector2(numberColumnWidth - standingSize.X, line * scoreLineSpacing);
                Vector2 playerNamePos = textTopLeft + new Vector2(numberColumnWidth + textMargin, line * scoreLineSpacing);
                Vector2 scorePos = textTopLeft + new Vector2(numberColumnWidth + (textMargin * 2) + playerNameWidth, line * scoreLineSpacing);
                string scoreText = string.Format("{0} = {1}-{2}", entry.Score, entry.Kills, entry.Suicides);
                Color rowColor = entry.PlayerColor;

                if (_player.ID == entry.SpectatorId)
                {
                    rowColor = Color.White;
                }

                spriteBatch.DrawString(_scoreFont, currentStanding.ToString(), standingPos, rowColor);
                spriteBatch.DrawString(_scoreFont, entry.Name, playerNamePos, rowColor);
                spriteBatch.DrawString(_scoreFont, scoreText, scorePos, rowColor);
                ++line;
            }

        }

        public override void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            _scoreBackgroundTexture = content.Load<Texture2D>("gui_radar_bg");
            _scoreFont = content.Load<SpriteFont>("ConsoleFont");
        }
    }
}
