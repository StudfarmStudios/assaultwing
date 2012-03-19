using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Graphics
{
    /// <summary>
    /// Manages the arena silhouette that is used in the radar overlay.
    /// </summary>
    public class ArenaSilhouette : IDisposable
    {
        private Vector2 _arenaDimensionsOnRadar;
        private Matrix _arenaToRadarTransform;
        private TimeSpan _lastArenaRadarSilhouetteUpdate;

        public AssaultWingCore Game { get; private set; }

        /// <summary>
        /// The currently active arena's silhouette, scaled and ready to be 
        /// drawn in a player's viewport's radar display.
        /// </summary>
        public Texture2D ArenaRadarSilhouette { get; private set; }

        /// <summary>
        /// If true, the arena radar silhouette will be updated soon.
        /// </summary>
        public bool UpdateArenaRadarSilhouette { get; set; }

        /// <summary>
        /// The transformation to map coordinates in the current arena 
        /// into player viewport radar display coordinates.
        /// </summary>
        /// <remarks>
        /// Arena origin is the lower left corner, positive X is to the right,
        /// and positive Y is up. Radar display origin is the top left corner
        /// of the radar display area, positive X is to the right, and positive
        /// Y is down.
        /// </remarks>
        public Matrix ArenaToRadarTransform { get { return _arenaToRadarTransform; } }

        public ArenaSilhouette(AssaultWingCore game)
        {
            Game = game;
        }

        public void EnsureUpdated()
        {
            if (Game.DataEngine.Arena == null) return;
            if (ArenaRadarSilhouette != null && (!UpdateArenaRadarSilhouette || _lastArenaRadarSilhouetteUpdate.SecondsAgoGameTime() < 0.5f)) return;
            RefreshArenaToRadarTransform();
            RefreshArenaRadarSilhouette();
            UpdateArenaRadarSilhouette = false;
            _lastArenaRadarSilhouetteUpdate = Game.GameTime.TotalGameTime;
        }

        public void Clear()
        {
            _lastArenaRadarSilhouetteUpdate = TimeSpan.Zero;
        }

        public void Dispose()
        {
            if (ArenaRadarSilhouette != null) ArenaRadarSilhouette.Dispose();
            ArenaRadarSilhouette = null;
        }

        private void RefreshArenaRadarSilhouette()
        {
            if (Game.DataEngine.Arena == null) throw new InvalidOperationException("No active arena");
            Dispose();

            // Draw arena walls in one color in a radar-sized texture.
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
            var oldViewport = gfx.Viewport;
            int targetWidth = (int)_arenaDimensionsOnRadar.X;
            int targetHeight = (int)_arenaDimensionsOnRadar.Y;
            var gfxAdapter = gfx.Adapter;
            SurfaceFormat selectedFormat;
            DepthFormat selectedDepthFormat;
            int selectedMultiSampleCount;
            gfxAdapter.QueryRenderTargetFormat(GraphicsProfile.Reach, SurfaceFormat.Color, DepthFormat.None, 1, out selectedFormat, out selectedDepthFormat, out selectedMultiSampleCount);
            var maskTarget = new RenderTarget2D(gfx, targetWidth, targetHeight, false, selectedFormat, selectedDepthFormat);

            // Set up draw matrices.
            var view = Matrix.CreateLookAt(new Vector3(0, 0, 500), Vector3.Zero, Vector3.Up);
            var projection = Matrix.CreateOrthographicOffCenter(0, Game.DataEngine.Arena.Dimensions.X,
                0, Game.DataEngine.Arena.Dimensions.Y, 10, 1000);

            // Set and clear our own render target.
            gfx.SetRenderTarget(maskTarget);
            gfx.Clear(ClearOptions.Target, Color.Transparent, 0, 0);

            // Draw the arena's walls.
            Game.GraphicsEngine.GameContent.RadarSilhouetteSpriteBatch.Begin();
            foreach (var wall in Game.DataEngine.Arena.GobsInRelevantLayers.OfType<AW2.Game.Gobs.Wall>())
                wall.DrawSilhouette(view, projection, Game.GraphicsEngine.GameContent.RadarSilhouetteSpriteBatch);
            Game.GraphicsEngine.GameContent.RadarSilhouetteSpriteBatch.End();

            // Restore render target so what we can extract drawn pixels.
            // Create a copy of the texture in local memory so that a graphics device
            // reset (e.g. when changing resolution) doesn't lose the texture.
            gfx.SetRenderTarget(null);
            gfx.Viewport = oldViewport;
            var textureData = new Color[targetHeight * targetWidth];
            maskTarget.GetData(textureData);
            ArenaRadarSilhouette = new Texture2D(gfx, targetWidth, targetHeight, false, SurfaceFormat.Color);
            ArenaRadarSilhouette.SetData(textureData);

            maskTarget.Dispose();
        }

        private void RefreshArenaToRadarTransform()
        {
            if (Game.DataEngine.Arena == null) throw new InvalidOperationException("No active arena");
            Vector2 radarDisplayDimensions = new Vector2(200, 200); // TODO: Make this constant configurable
            Vector2 arenaDimensions = Game.DataEngine.Arena.Dimensions;
            var arenaToRadarScale = Math.Min(
                radarDisplayDimensions.X / arenaDimensions.X,
                radarDisplayDimensions.Y / arenaDimensions.Y);
            _arenaDimensionsOnRadar = arenaDimensions * arenaToRadarScale;
            _arenaToRadarTransform =
                Matrix.CreateScale(arenaToRadarScale, -arenaToRadarScale, 1) *
                Matrix.CreateTranslation(0, _arenaDimensionsOnRadar.Y, 0);
        }
    }
}
