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
        private const int SCORE_LINE_SPACING = 17;

        private Player _player;

        public override Point Dimensions
        {
            get { return new Point(Content.ScoreBackgroundTexture.Width, 30 + (Game.DataEngine.Players.Count() * SCORE_LINE_SPACING)); }
        }

        private AssaultWingCore Game { get { return _player.Game; } }
        private GameContent Content { get { return Game.GraphicsEngine.GameContent; } }

        public ScoreOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Left, VerticalAlignment.Bottom)
        {
            _player = viewport.Player;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Content.ScoreBackgroundTexture, Vector2.Zero, Color.White);

            var textTopLeft = new Vector2(13, 29);
            int playerNameWidth = 139;
            int scoreWidth = 48;
            int scoreEntryWidth = 35;

            var standings = Game.DataEngine.GameplayMode.GetStandings(Game.DataEngine.Players);
            int line = 0;

            foreach (var entry in standings)
            {
                var playerNamePos = textTopLeft + new Vector2(0, line * SCORE_LINE_SPACING);
                var scorePos = textTopLeft + new Vector2(playerNameWidth, line * SCORE_LINE_SPACING - 3);
                var killsPos = textTopLeft + new Vector2(playerNameWidth + scoreWidth, line * SCORE_LINE_SPACING);
                var deathsPos = textTopLeft + new Vector2(playerNameWidth + scoreWidth + scoreEntryWidth, line * SCORE_LINE_SPACING);
                var rowColor = _player.ID == entry.SpectatorID ? Color.White : entry.Color;

                ModelRenderer.DrawBorderedText(spriteBatch, Content.ConsoleFont, entry.Name, playerNamePos.Round(), rowColor, 0.9f, 1);
                ModelRenderer.DrawBorderedText(spriteBatch, Content.ScoreFont, entry.Score.ToString(), scorePos.Round(), rowColor, 0.9f, 1);
                ModelRenderer.DrawBorderedText(spriteBatch, Content.ConsoleFont, entry.Kills.ToString(), killsPos.Round(), rowColor, 0.9f, 1);
                ModelRenderer.DrawBorderedText(spriteBatch, Content.ConsoleFont, entry.Deaths.ToString(), deathsPos.Round(), rowColor, 0.9f, 1);

                ++line;
            }
        }
    }
}
