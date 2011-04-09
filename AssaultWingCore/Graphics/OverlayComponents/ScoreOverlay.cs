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
        private SpriteFont _textFont;
        private int scoreLineSpacing = 17;

        public override Point Dimensions
        {
            get { return new Point(_scoreBackgroundTexture.Width, 30 + (AssaultWingCore.Instance.DataEngine.Players.Count() * scoreLineSpacing)); }
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

            var textTopLeft = new Vector2(13, 29);
            int playerNameWidth = 139;
            int scoreWidth = 48;
            int scoreEntryWidth = 35;

            var standings = AssaultWingCore.Instance.DataEngine.GameplayMode.GetStandings(AssaultWingCore.Instance.DataEngine.Players);
            int line = 0;

            foreach (var entry in standings)
            {
                var currentStanding = line + 1;
                var playerNamePos = textTopLeft + new Vector2(0, line * scoreLineSpacing);
                var scorePos = textTopLeft + new Vector2(playerNameWidth, line * scoreLineSpacing - 3);
                var killsPos = textTopLeft + new Vector2(playerNameWidth + scoreWidth, line * scoreLineSpacing);
                var deathsPos = textTopLeft + new Vector2(playerNameWidth + scoreWidth + scoreEntryWidth, line * scoreLineSpacing);
                var scoreText = entry.Score.ToString();
                var killsText = entry.Kills.ToString();
                var deathsText = entry.Suicides.ToString();
                var rowColor = _player.ID == entry.SpectatorId ? Color.White : entry.PlayerColor;

                DrawBorderedText(entry.Name, playerNamePos.Round(), _textFont, spriteBatch, rowColor);
                DrawBorderedText(scoreText, scorePos.Round(), _scoreFont, spriteBatch, rowColor);
                DrawBorderedText(killsText, killsPos.Round(), _textFont, spriteBatch, rowColor);
                DrawBorderedText(deathsText, deathsPos.Round(), _textFont, spriteBatch, rowColor);

                ++line;
            }
        }

        private void DrawBorderedText(string text, Vector2 position, SpriteFont font, SpriteBatch batch, Color color)
        {
            batch.DrawString(font, text, position - Vector2.One, Color.Black);
            batch.DrawString(font, text, position + new Vector2(1, -1), Color.Black);
            batch.DrawString(font, text, position + new Vector2(-1, +1), Color.Black);
            batch.DrawString(font, text, position + Vector2.One, Color.Black);
            batch.DrawString(font, text, position, color);
        }

        public override void LoadContent()
        {
            var content = AssaultWingCore.Instance.Content;
            _scoreBackgroundTexture = content.Load<Texture2D>("gui_playerlist_bg");
            _textFont = content.Load<SpriteFont>("ConsoleFont");
            _scoreFont = content.Load<SpriteFont>("ScoreFont");
        }
    }
}
