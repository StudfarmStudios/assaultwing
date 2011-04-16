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
                var deathsText = entry.Deaths.ToString();
                var rowColor = _player.ID == entry.SpectatorID ? Color.White : entry.PlayerColor;

                ModelRenderer.DrawBorderedText(spriteBatch, _textFont, entry.Name, playerNamePos.Round(), rowColor, 0.9f, 1);
                ModelRenderer.DrawBorderedText(spriteBatch, _scoreFont, scoreText, scorePos.Round(), rowColor, 0.9f, 1);
                ModelRenderer.DrawBorderedText(spriteBatch, _textFont, killsText, killsPos.Round(), rowColor, 0.9f, 1);
                ModelRenderer.DrawBorderedText(spriteBatch, _textFont, deathsText, deathsPos.Round(), rowColor, 0.9f, 1);

                ++line;
            }
        }

        public override void LoadContent()
        {
            AssaultWingCore.Instance.GraphicsDeviceService.CheckThread();
            var content = AssaultWingCore.Instance.Content;
            _scoreBackgroundTexture = content.Load<Texture2D>("gui_playerlist_bg");
            _textFont = content.Load<SpriteFont>("ConsoleFont");
            _scoreFont = content.Load<SpriteFont>("ScoreFont");
        }
    }
}
