using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Game;
using AW2.Game.Gobs;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component displaying a radar view of the arena.
    /// </summary>
    public class RadarOverlay : OverlayComponent
    {
        private static readonly Color ARENA_RADAR_SILHOUETTE_COLOR = new Color(190, 190, 190, 85);
        private static readonly Vector2 RADAR_DISPLAY_TOP_LEFT = new Vector2(7, 7);
        private Player _player;
        private Texture2D _radarDisplayTexture;
        private Texture2D _shipOnRadarTexture;
        private Texture2D _dockOnRadarTexture;

        public override Point Dimensions
        {
            get { return new Point(_radarDisplayTexture.Width, _radarDisplayTexture.Height); }
        }

        public RadarOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Left, VerticalAlignment.Top)
        {
            _player = viewport.Player;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);
            DrawWalls(spriteBatch);
            DrawDocks(spriteBatch);
            DrawShips(spriteBatch);
        }

        private void DrawBackground(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_radarDisplayTexture, Vector2.Zero, Color.White);
        }

        private static void DrawWalls(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(AssaultWing.Instance.DataEngine.ArenaRadarSilhouette, RADAR_DISPLAY_TOP_LEFT, ARENA_RADAR_SILHOUETTE_COLOR);
        }

        private void DrawDocks(SpriteBatch spriteBatch)
        {
            var arenaToRadarTransform = AssaultWing.Instance.DataEngine.ArenaToRadarTransform;
            foreach (var dock in AssaultWing.Instance.DataEngine.Arena.Gobs.OfType<Dock>())
            {
                var posInArena = dock.Pos;
                var posOnRadar = RADAR_DISPLAY_TOP_LEFT + Vector2.Transform(posInArena, arenaToRadarTransform);
                spriteBatch.Draw(_dockOnRadarTexture, posOnRadar, null, Color.White, 0,
                    _dockOnRadarTexture.Dimensions() / 2, 0.1f, SpriteEffects.None, 0);
            }
        }

        private void DrawShips(SpriteBatch spriteBatch)
        {
            var arenaToRadarTransform = AssaultWing.Instance.DataEngine.ArenaToRadarTransform;
            foreach (var player in AssaultWing.Instance.DataEngine.Players)
            {
                if (player.Ship == null || player.Ship.Dead) continue;
                var posInArena = player.Ship.Pos;
                var posOnRadar = RADAR_DISPLAY_TOP_LEFT + Vector2.Transform(posInArena, arenaToRadarTransform);
                Color shipColor = player.PlayerColor;
                float shipScale = 0.4f;

                if (_player.ID == player.ID)
                {
                    shipColor = Color.White;
                    shipScale = 0.7f;
                }

                spriteBatch.Draw(_shipOnRadarTexture, posOnRadar, null, shipColor, 0,
                    _shipOnRadarTexture.Dimensions() / 2, shipScale, SpriteEffects.None, 0);
            }
        }

        public override void LoadContent()
        {
            _radarDisplayTexture = AssaultWing.Instance.Content.Load<Texture2D>("gui_radar_bg");
            _shipOnRadarTexture = AssaultWing.Instance.Content.Load<Texture2D>("gui_playerinfo_white_ball");
            _dockOnRadarTexture = AssaultWing.Instance.Content.Load<Texture2D>("p_green_box");
        }
    }
}
