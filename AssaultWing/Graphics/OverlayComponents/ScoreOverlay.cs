using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics.OverlayComponents
{
    public class ScoreOverlay : OverlayComponent
    {
        private Player _player;
        private Texture2D _scoreBackgroundTexture;
        private SpriteFont _scoreFont;
        private int scoreLineSpacing = 12;

        public override Point Dimensions
        {
            get { return new Point(_scoreBackgroundTexture.Width, 10 + (AssaultWingCore.Instance.DataEngine.Players.Count() * scoreLineSpacing)); }
        }

        public ScoreOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Left, VerticalAlignment.Bottom)
        {
            _player = viewport.Player;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            // Draw scoredisplay background
            spriteBatch.Draw(_scoreBackgroundTexture, Vector2.Zero, Color.White);

            var textTopLeft = new Vector2(5, 10);
            int numberColumnWidth = 16;
            int playerNameWidth = 115;
            int textMargin = 10;

            var standings = AssaultWingCore.Instance.DataEngine.GameplayMode.GetStandings(AssaultWingCore.Instance.DataEngine.Players);
            int line = 0;

            foreach (var entry in standings)
            {
                var currentStanding = line + 1;
                var standingSize = _scoreFont.MeasureString(currentStanding.ToString());
                var standingPos = textTopLeft + new Vector2(numberColumnWidth - standingSize.X, line * scoreLineSpacing);
                var playerNamePos = textTopLeft + new Vector2(numberColumnWidth + textMargin, line * scoreLineSpacing);
                var scorePos = textTopLeft + new Vector2(numberColumnWidth + (textMargin * 2) + playerNameWidth, line * scoreLineSpacing);
                var scoreText = string.Format("{0} = {1}-{2}", entry.Score, entry.Kills, entry.Suicides);
                var rowColor = _player.ID == entry.SpectatorId ? Color.White : entry.PlayerColor;
                spriteBatch.DrawString(_scoreFont, currentStanding.ToString(), standingPos.Round(), rowColor);
                spriteBatch.DrawString(_scoreFont, entry.Name, playerNamePos.Round(), rowColor);
                spriteBatch.DrawString(_scoreFont, scoreText, scorePos.Round(), rowColor);
                ++line;
            }
        }

        public override void LoadContent()
        {
            var content = AssaultWingCore.Instance.Content;
            _scoreBackgroundTexture = content.Load<Texture2D>("gui_radar_bg");
            _scoreFont = content.Load<SpriteFont>("ConsoleFont");
        }
    }
}
