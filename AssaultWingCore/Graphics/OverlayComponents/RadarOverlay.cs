using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Game.Gobs;
using AW2.Helpers;

namespace AW2.Graphics.OverlayComponents
{
    /// <summary>
    /// Overlay graphics component displaying a radar view of the arena.
    /// </summary>
    public class RadarOverlay : OverlayComponent
    {
        private static readonly Color ARENA_RADAR_SILHOUETTE_COLOR = Color.FromNonPremultiplied(190, 190, 190, 85);
        private static readonly Vector2 RADAR_DISPLAY_TOP_LEFT = new Vector2(7, 7);
        private Player _player;
        private Texture2D _radarDisplayTexture;
        private Texture2D _shipOnRadarTexture;
        private Texture2D _dockOnRadarTexture;
        private List<Dock> _docks;

        public override Point Dimensions
        {
            get { return new Point(_radarDisplayTexture.Width, _radarDisplayTexture.Height); }
        }

        public RadarOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Left, VerticalAlignment.Top)
        {
            _player = viewport.Player;
            _docks = _player.Game.DataEngine.Arena.Gobs.OfType<Dock>().ToList();
            _player.Game.DataEngine.Arena.GobAdded += GobAddedHandler;
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
            spriteBatch.Draw(AssaultWingCore.Instance.DataEngine.ArenaRadarSilhouette, RADAR_DISPLAY_TOP_LEFT, ARENA_RADAR_SILHOUETTE_COLOR);
        }

        private void DrawDocks(SpriteBatch spriteBatch)
        {
            var arenaToRadarTransform = AssaultWingCore.Instance.DataEngine.ArenaToRadarTransform;
            bool deadDocks = false;
            foreach (var dock in _docks)
            {
                if (dock.Dead)
                {
                    deadDocks = true;
                    continue;
                }
                var posInArena = dock.Pos;
                var posOnRadar = RADAR_DISPLAY_TOP_LEFT + Vector2.Transform(posInArena, arenaToRadarTransform);
                spriteBatch.Draw(_dockOnRadarTexture, posOnRadar, null, Color.White, 0,
                    _dockOnRadarTexture.Dimensions() / 2, 0.1f, SpriteEffects.None, 0);
            }
            if (deadDocks) _docks.RemoveAll(dock => dock.Dead);
        }

        private void DrawShips(SpriteBatch spriteBatch)
        {
            var arenaToRadarTransform = AssaultWingCore.Instance.DataEngine.ArenaToRadarTransform;
            foreach (var player in AssaultWingCore.Instance.DataEngine.Players)
            {
                if (player.Ship == null || player.Ship.Dead) continue;
                var posInArena = player.Ship.Pos;
                var posOnRadar = RADAR_DISPLAY_TOP_LEFT + Vector2.Transform(posInArena, arenaToRadarTransform);
                var shipColor = _player.ID == player.ID ? Color.White : player.PlayerColor;
                var shipScale = _player.ID == player.ID ? 0.7f : 0.4f;
                spriteBatch.Draw(_shipOnRadarTexture, posOnRadar, null, shipColor, 0,
                    _shipOnRadarTexture.Dimensions() / 2, shipScale, SpriteEffects.None, 0);
            }
        }

        public override void LoadContent()
        {
            _radarDisplayTexture = AssaultWingCore.Instance.Content.Load<Texture2D>("gui_radar_bg");
            _shipOnRadarTexture = AssaultWingCore.Instance.Content.Load<Texture2D>("gui_playerinfo_white_ball");
            _dockOnRadarTexture = AssaultWingCore.Instance.Content.Load<Texture2D>("p_green_box");
        }

        public override void Dispose()
        {
            base.Dispose();
            var arena = _player.Game.DataEngine.Arena;
            if (arena != null) arena.GobAdded -= GobAddedHandler;
        }

        private void GobAddedHandler(Gob gob)
        {
            if (gob is Dock) _docks.Add((Dock)gob);
        }
    }
}
