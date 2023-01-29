using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Game.Logic;
using AW2.Game.Players;
using AW2.Helpers;

namespace AW2.Graphics.OverlayComponents
{
    public class ScoreOverlay : OverlayComponent
    {
        private static readonly Vector2 TextTopLeft = new Vector2(13, 29);
        private const int NameWidth = 139;
        private const int ScoreWidth = 48;
        private const int ScoreEntryWidth = 35;
        private const int ScoreLineSpacing = 17;

        private Player _player;

        public override Point Dimensions
        {
            get { return new Point(Content.ScoreBackgroundTexture.Width, 30 + EntryCount * ScoreLineSpacing); }
        }

        private AssaultWingCore Game { get { return _player.Game; } }
        private GameContent Content { get { return Game.GraphicsEngine.GameContent; } }
        private Standings Standings { get { return Game.DataEngine.Standings; } }
        private int EntryCount { get { return Standings.HasTrivialTeams ? Standings.SpectatorCount : Standings.TeamCount + Standings.SpectatorCount; } }

        public ScoreOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Left, VerticalAlignment.Bottom)
        {
            _player = viewport.Owner;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Content.ScoreBackgroundTexture, Vector2.Zero, Color.White);
            if (Standings.HasTrivialTeams)
                DrawTrivialTeams(spriteBatch);
            else
                DrawFullTeams(spriteBatch);
        }

        private void DrawTrivialTeams(SpriteBatch spriteBatch)
        {
            int line = 0;
            foreach (var specEntry in Standings.GetSpectators()) DrawStandingEntry(spriteBatch, specEntry, line++, 0);
        }

        private void DrawFullTeams(SpriteBatch spriteBatch)
        {
            int line = 0;
            foreach (var mainEntry in Standings)
            {
                DrawStandingEntry(spriteBatch, mainEntry.Item1, line++, 0);
                foreach (var subEntry in mainEntry.Item2) DrawStandingEntry(spriteBatch, subEntry, line++, 10);
            }
        }

        private void DrawStandingEntry(SpriteBatch spriteBatch, Standing entry, int line, int indent)
        {
            var namePos = TextTopLeft + new Vector2(indent, line * ScoreLineSpacing);
            var scorePos = TextTopLeft + new Vector2(NameWidth, line * ScoreLineSpacing - 3);
            var killsPos = TextTopLeft + new Vector2(NameWidth + ScoreWidth, line * ScoreLineSpacing);
            var deathsPos = TextTopLeft + new Vector2(NameWidth + ScoreWidth + ScoreEntryWidth, line * ScoreLineSpacing);
            var rowAlpha = entry.IsActive ? 1 : 0.5f;
            var isHighlighted = Game.DataEngine.FindSpectator(entry.ID) == _player;
            var rowColor = Color.Multiply(isHighlighted ? Color.White : entry.Color, rowAlpha);
            ModelRenderer.DrawBorderedText(spriteBatch, Content.ConsoleFont, entry.Name, Vector2.Round(namePos), rowColor, 0.9f, 1);
            ModelRenderer.DrawBorderedText(spriteBatch, Content.ScoreFont, entry.Score.ToString(), Vector2.Round(scorePos), rowColor, 0.9f, 1);
            ModelRenderer.DrawBorderedText(spriteBatch, Content.ConsoleFont, entry.Kills.ToString(), Vector2.Round(killsPos), rowColor, 0.9f, 1);
            ModelRenderer.DrawBorderedText(spriteBatch, Content.ConsoleFont, entry.Deaths.ToString(), Vector2.Round(deathsPos), rowColor, 0.9f, 1);
        }
    }
}
