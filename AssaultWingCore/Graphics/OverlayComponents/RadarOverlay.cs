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
        private List<Dock> _docks;

        public override Point Dimensions { get { return new Point(Game.GraphicsEngine.GameContent.RadarDisplayTexture.Width, Game.GraphicsEngine.GameContent.RadarDisplayTexture.Height); } }
        private AssaultWingCore Game { get { return _player.Game; } }

        public RadarOverlay(PlayerViewport viewport)
            : base(viewport, HorizontalAlignment.Left, VerticalAlignment.Top)
        {
            _player = viewport.Owner;
            _docks = Game.DataEngine.Arena.Gobs.OfType<Dock>().ToList();
            Game.DataEngine.Arena.GobAdded += GobAddedHandler;
        }

        protected override void DrawContent(SpriteBatch spriteBatch)
        {
            DrawBackground(spriteBatch);
            DrawWalls(spriteBatch);
            DrawDocks(spriteBatch);
            DrawMinions(spriteBatch);
        }

        private void DrawBackground(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Game.GraphicsEngine.GameContent.RadarDisplayTexture, Vector2.Zero, Color.White);
        }

        private void DrawWalls(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Game.DataEngine.ArenaRadarSilhouette, RADAR_DISPLAY_TOP_LEFT, ARENA_RADAR_SILHOUETTE_COLOR);
        }

        private void DrawDocks(SpriteBatch spriteBatch)
        {
            var arenaToRadarTransform = Game.DataEngine.ArenaToRadarTransform;
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
                spriteBatch.Draw(Game.GraphicsEngine.GameContent.DockOnRadarTexture, posOnRadar, null, Color.White, 0,
                    Game.GraphicsEngine.GameContent.DockOnRadarTexture.Dimensions() / 2, 0.1f, SpriteEffects.None, 0);
            }
            if (deadDocks) _docks.RemoveAll(dock => dock.Dead);
        }

        private void DrawMinions(SpriteBatch spriteBatch)
        {
            var arenaToRadarTransform = Game.DataEngine.ArenaToRadarTransform;
            foreach (var minion in Game.DataEngine.Minions)
            {
                if (minion.Dead) continue;
                var owner = minion.Owner;
                var posInArena = minion.Pos;
                var posOnRadar = RADAR_DISPLAY_TOP_LEFT + Vector2.Transform(posInArena, arenaToRadarTransform);
                var shipAlpha = owner.IsRemote && minion.IsHiding ? minion.Alpha : 1;
                var shipColor = Color.Multiply(_player.ID == owner.ID ? Color.White : owner.Color, shipAlpha);
                var shipScale = _player.ID == owner.ID ? 0.7f : 0.4f;
                spriteBatch.Draw(Game.GraphicsEngine.GameContent.ShipOnRadarTexture, posOnRadar, null, shipColor, 0,
                    Game.GraphicsEngine.GameContent.ShipOnRadarTexture.Dimensions() / 2, shipScale, SpriteEffects.None, 0);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            var arena = Game.DataEngine.Arena;
            if (arena != null) arena.GobAdded -= GobAddedHandler;
        }

        private void GobAddedHandler(Gob gob)
        {
            if (gob is Dock) _docks.Add((Dock)gob);
        }
    }
}
