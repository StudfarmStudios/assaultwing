using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Graphics;
using AW2.Helpers;

namespace AW2.Core.GameComponents
{
    /// <summary>
    /// Displays some system status for the end-user.
    /// </summary>
    public class SystemStatusOverlay : OverlayComponent
    {
        private const int LINE_COUNT = 2;
        private const int TEXT_MARGIN = 5;

        private AssaultWing _game;

        public override Point Dimensions { get { return new Point(TEXT_MARGIN + 80, TEXT_MARGIN + Font.LineSpacing * LINE_COUNT); } }

        private GameContent Content { get { return _game.GraphicsEngine.GameContent; } }
        private SpriteFont Font { get { return Content.ConsoleFont; } }

        public SystemStatusOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Right, VerticalAlignment.Top)
        {
            _game = (AssaultWing)viewport.Owner.Game;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            DrawArenaTimeLeft(spriteBatch);
            DrawPing(spriteBatch);
        }

        private void DrawArenaTimeLeft(SpriteBatch spriteBatch)
        {
            if (_game.DataEngine.ArenaFinishTime == TimeSpan.Zero) return;
            var timeLeft = _game.DataEngine.ArenaFinishTime - _game.GameTime.TotalRealTime;
            if (timeLeft < TimeSpan.Zero) return;
            var timeText = timeLeft.ToDurationString("d", "h", "min", "s", false);
            DrawText(spriteBatch, 0, timeText, Color.White);
        }

        private void DrawPing(SpriteBatch spriteBatch)
        {
            if (_game.NetworkMode != NetworkMode.Client) return;
            var pingMs = (int)_game.NetworkEngine.GameServerConnection.PingInfo.PingTime.TotalMilliseconds;
            if (pingMs >= 120) DrawText(spriteBatch, 1, pingMs + " ms lag", Color.Red);
        }

        private void DrawText(SpriteBatch spriteBatch, int line, string text, Color color)
        {
            if (line < 0 || line >= LINE_COUNT) throw new ArgumentOutOfRangeException("line");
            var textPos = new Vector2(Dimensions.X - TEXT_MARGIN, Font.LineSpacing * (1 + line) + TEXT_MARGIN) - Font.MeasureString(text);
            ModelRenderer.DrawBorderedText(spriteBatch, Font, text, textPos, color, 1, 1);
        }
    }
}
