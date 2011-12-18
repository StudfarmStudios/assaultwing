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
        private AssaultWing _game;

        public override Point Dimensions { get { return new Point(80, 5 + Font.LineSpacing); } }

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
            DrawText(spriteBatch, 0, timeText);
        }

        private void DrawPing(SpriteBatch spriteBatch)
        {
            if (_game.NetworkMode != NetworkMode.Client) return;
            var pingMs = (int)_game.NetworkEngine.GameServerConnection.PingInfo.PingTime.TotalMilliseconds;
            if (pingMs >= 120) DrawText(spriteBatch, 1, pingMs + " ms lag");
        }

        private void DrawText(SpriteBatch spriteBatch, int line, string text)
        {
            var textSize = Font.MeasureString(text) + new Vector2(5, 0);
            var textPos = Dimensions.ToVector2() - textSize + new Vector2(0, Font.LineSpacing) * line;
            ModelRenderer.DrawBorderedText(spriteBatch, Font, text, textPos, Color.White, 1, 1);
        }
    }
}
