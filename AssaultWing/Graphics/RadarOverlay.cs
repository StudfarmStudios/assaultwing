using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game;
using AW2.Game.Gobs;

namespace AW2.Graphics
{
    /// <summary>
    /// Overlay graphics component displaying a radar view of the arena.
    /// </summary>
    class RadarOverlay : OverlayComponent
    {
        private static readonly Color ARENA_RADAR_SILHOUETTE_COLOR = new Color(190, 190, 190, 85);
        private Player _player;
        private Texture2D _radarDisplayTexture;
        private Texture2D _shipOnRadarTexture;
        private Texture2D _dockOnRadarTexture;

        public override Point Dimensions
        {
            get { return new Point(_radarDisplayTexture.Width, _radarDisplayTexture.Height); }
        }

        /// <param name="player">The player whose status to display.</param>
        public RadarOverlay(Player player)
            : base(HorizontalAlignment.Left, VerticalAlignment.Top)
        {
            _player = player;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            // Radar background
            spriteBatch.Draw(_radarDisplayTexture, Vector2.Zero, Color.White);

            // Arena walls on radar
            Vector2 radarDisplayTopLeft = new Vector2(7, 7);
            spriteBatch.Draw(AssaultWing.Instance.DataEngine.ArenaRadarSilhouette, radarDisplayTopLeft, ARENA_RADAR_SILHOUETTE_COLOR);

            // Ships on radar
            var arenaToRadarTransform = AssaultWing.Instance.DataEngine.ArenaToRadarTransform;
            foreach (var player in AssaultWing.Instance.DataEngine.Players)
            {
                if (player.Ship == null) continue;
                var posInArena = player.Ship.Pos;
                var posOnRadar = radarDisplayTopLeft + Vector2.Transform(posInArena, arenaToRadarTransform);
                Color shipColor = player.PlayerColor;
                float shipScale = 0.4f;

                if (_player.Id == player.Id)
                {
                    shipColor = Color.White;
                    shipScale = 0.7f;
                }

                spriteBatch.Draw(_shipOnRadarTexture, posOnRadar, null, shipColor, 0,
                    GetTextureCenter(_shipOnRadarTexture), shipScale, SpriteEffects.None, 0);
            }

            foreach (var dock in AssaultWing.Instance.DataEngine.Arena.Gobs.OfType<Dock>())
            {
                var posInArena = dock.Pos;
                var posOnRadar = radarDisplayTopLeft + Vector2.Transform(posInArena, arenaToRadarTransform);
                spriteBatch.Draw(_dockOnRadarTexture, posOnRadar, null, Color.White, 0,
                    GetTextureCenter(_dockOnRadarTexture), 0.1f, SpriteEffects.None, 0);
            }
        }

        public override void LoadContent()
        {
            _radarDisplayTexture = AssaultWing.Instance.Content.Load<Texture2D>("gui_radar_bg");
            _shipOnRadarTexture = AssaultWing.Instance.Content.Load<Texture2D>("gui_playerinfo_white_ball");
            _dockOnRadarTexture = AssaultWing.Instance.Content.Load<Texture2D>("p_green_box");
        }

        public override void UnloadContent()
        {
        }

        private Vector2 GetTextureCenter(Texture2D texture)
        {
            return new Vector2(texture.Width, texture.Height) / 2;
        }
    }
}
