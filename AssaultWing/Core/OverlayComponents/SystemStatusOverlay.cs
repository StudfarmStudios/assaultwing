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
            _game = (AssaultWing)viewport.Player.Game;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            DrawPing(spriteBatch);
        }

        private void DrawPing(SpriteBatch spriteBatch)
        {
            if (_game.NetworkMode != NetworkMode.Client) return;
            var pingMs = (int)_game.NetworkEngine.GameServerConnection.PingInfo.PingTime.TotalMilliseconds;
            if (pingMs >= 120)
            {
                var viewport = _game.GraphicsDeviceService.GraphicsDevice.Viewport;
                var text = pingMs + " ms lag";
                var textSize = Font.MeasureString(text) + new Vector2(5, 0);
                ModelRenderer.DrawBorderedText(spriteBatch, Font, text, Dimensions.ToVector2() - textSize, Color.Red, 1, 1);
            }
        }
    }
}
