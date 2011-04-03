using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game.Arenas;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Graphics;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
using AW2.Helpers.Collections;

namespace AW2.Game
{
    /// <summary>
    /// Basic implementation of game data.
    /// </summary>
    /// Gobs in an arena are kept on several arena layers. One of the layers
    /// is where the actual gameplay takes place. The rest are just for the looks.
    /// The gameplay layer is the default for all gob-related actions.
    /// To deal with some other layer, you need to know its layer index.
    /// There is also another special layer; the gameplay backlayer. It is
    /// at the same depth as the gameplay layer but is drawn behind it.
    /// The gameplay backlayer is for 2D graphics that needs to be behind gobs.
    public class DataEngine : AWGameComponent
    {
        /// <summary>
        /// Type templates, indexed by their type name.
        /// </summary>
        private NamedItemCollection<object> _templates;

        private Texture2D _arenaRadarSilhouette;
        private Vector2 _arenaDimensionsOnRadar;
        private Matrix _arenaToRadarTransform;
        private TimeSpan _lastArenaRadarSilhouetteUpdate;
        private IndexedItemCollection<Spectator> _spectators;

        /// <summary>
        /// Players and other spectators of the game session.
        /// </summary>
        public IndexedItemCollection<Spectator> Spectators
        {
            get { return _spectators; }
            private set
            {
                _spectators = value;
                _spectators.Added += SpectatorAddedHandler;
                _spectators.Removed += SpectatorRemovedHandler;
            }
        }

        public IEnumerable<Player> Players { get { return Spectators.OfType<Player>(); } }
        public IndexedItemCollection<ShipDevice> Devices { get; private set; }
        public Arena Arena { get; set; }
        public TimeSpan ArenaTotalTime { get { return Arena == null ? TimeSpan.Zero : Arena.TotalTime; } }
        public int ArenaFrameCount { get { return Arena == null ? 0 : Arena.FrameNumber; } }

        public event Action<Spectator> SpectatorAdded;
        public event Action<Spectator> SpectatorRemoved;

        public DataEngine(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
            Spectators = new IndexedItemCollection<Spectator>();
            Spectators.Added += SpectatorAdded;
            Spectators.Removed += SpectatorRemoved;

            Devices = new IndexedItemCollection<ShipDevice>();
            Devices.Added += device =>
            {
                device.Arena = Arena;
                device.Activate();
            };
            Devices.Removed += device => device.Dispose();

            Viewports = new AWViewportCollection(Game.GraphicsDeviceService, 0, null);
            _templates = new NamedItemCollection<object>();
        }

        #region arenas

        /// <summary>
        /// The currently active arena's silhouette, scaled and ready to be 
        /// drawn in a player's viewport's radar display.
        /// </summary>
        public Texture2D ArenaRadarSilhouette
        {
            get
            {
                if (_arenaRadarSilhouette == null ||
                    (UpdateArenaRadarSilhouette && _lastArenaRadarSilhouetteUpdate.SecondsAgoGameTime() >= 1))
                {
                    RefreshArenaToRadarTransform();
                    RefreshArenaRadarSilhouette();
                    UpdateArenaRadarSilhouette = false;
                    _lastArenaRadarSilhouetteUpdate = Game.GameTime.TotalGameTime;
                }
                return _arenaRadarSilhouette;
            }
        }

        /// <summary>
        /// If true, the arena radar silhouette will be updated soon.
        /// </summary>
        public bool UpdateArenaRadarSilhouette { get; set; }

        /// <summary>
        /// The transformation to map coordinates in the current arena 
        /// into player viewport radar display coordinates.
        /// </summary>
        /// Arena origin is the lower left corner, positive X is to the right,
        /// and positive Y is up. Radar display origin is the top left corner
        /// of the radar display area, positive X is to the right, and positive
        /// Y is down.
        public Matrix ArenaToRadarTransform { get { return _arenaToRadarTransform; } }

        public void StartArena()
        {
            // Clear old stuff from previous arena, if any.
            Devices.Clear();
            _lastArenaRadarSilhouetteUpdate = TimeSpan.Zero;
            foreach (var player in Spectators) player.ResetForArena();
            Game.GobsCounter.SetRawValue(Arena.Gobs.Count);
        }

        #endregion arenas

        #region type templates

        /// <summary>
        /// Saves an object to be used as a template for a user-defined named type.
        /// </summary>
        public void AddTypeTemplate(CanonicalString typeName, object template)
        {
            try
            {
                _templates.Add(typeName, template);
            }
            catch (ArgumentException)
            {
                Log.Write("WARNING: Overwriting template for user-defined type " + typeName);
            }
        }

        public object GetTypeTemplate(CanonicalString typeName)
        {
            object result;
            _templates.TryGetValue(typeName, out result);
            return result;
        }

        public IEnumerable<T> GetTypeTemplates<T>()
        {
            return _templates.Values.OfType<T>();
        }

        #endregion type templates

        #region viewports

        public AWViewportCollection Viewports { get; private set; }

        public void RearrangeViewports()
        {
            var playerCount = Game.DataEngine.Spectators.Where(player => player.NeedsViewport).Count();
            var viewportPermutation =
                playerCount <= 1 ? x => x
                : playerCount == 2 ? x => x == 0 ? 1 : x == 1 ? 0 : x
                : (Func<int, int>)(x => x == 0 ? 1 : x == 1 ? 2 : x == 2 ? 0 : x);
            RearrangeViewports(viewportPermutation);
        }

        private void RearrangeViewports(Func<int, int> viewportToPlayerPermutation)
        {
            if (Arena == null) return;
            var localPlayers = Game.DataEngine.Spectators.Where(player => player.NeedsViewport).ToList();
            if (Viewports != null) Viewports.Dispose();
            Viewports = new AWViewportCollection(Game.GraphicsDeviceService, localPlayers.Count(),
                (index, rectangle) => localPlayers[viewportToPlayerPermutation(index)].CreateViewport(rectangle));
        }

        /// <summary>
        /// Rearranges player viewports so that one player gets all screen space
        /// and the others get nothing.
        /// </summary>
        public void RearrangeViewports(int privilegedPlayer)
        {
            var player = Game.DataEngine.Spectators[privilegedPlayer];
            Viewports = new AWViewportCollection(Game.GraphicsDeviceService, 1, (index, viewport) => player.CreateViewport(viewport));
        }

        #endregion viewports

        #region miscellaneous

        public GameplayMode GameplayMode { get; set; }

        /// <summary>
        /// Clears all data about the state of the game session that is not
        /// needed when the game session is over.
        /// Data that is generated during a game session and is still relevant 
        /// after the game session is left untouched.
        /// </summary>
        /// Call this method after the game session has ended.
        public void ClearGameState()
        {
            if (Arena != null) Arena.Dispose();
            Arena = null;
            Viewports = new AWViewportCollection(Game.GraphicsDeviceService, 0, null);
            foreach (var player in Spectators) player.ResetForArena();
        }

        /// <summary>
        /// Unloads content needed by the currently active arena.
        /// </summary>
        public override void UnloadContent()
        {
            if (_arenaRadarSilhouette != null)
            {
                _arenaRadarSilhouette.Dispose();
                _arenaRadarSilhouette = null;
            }
        }

        /// <summary>
        /// Refreshes <c>ArenaRadarSilhouette</c> according to the contents 
        /// of the currently active arena.
        /// </summary>
        /// To be called whenever arena (or arena walls) change,
        /// after <c>RefreshArenaToRadarTransform</c>.
        /// <seealso cref="ArenaRadarSilhouette"/>
        /// <seealso cref="RefreshArenaToRadarTransform"/>
        public void RefreshArenaRadarSilhouette()
        {
            if (Arena == null)
                throw new InvalidOperationException("No active arena");

            // Dispose of any previous silhouette.
            if (_arenaRadarSilhouette != null)
            {
                _arenaRadarSilhouette.Dispose();
                _arenaRadarSilhouette = null;
            }

            // Draw arena walls in one color in a radar-sized texture.
            var gfx = Game.GraphicsDeviceService.GraphicsDevice;
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
            var projection = Matrix.CreateOrthographicOffCenter(0, Arena.Dimensions.X,
                0, Arena.Dimensions.Y, 10, 1000);

            // Set and clear our own render target.
            gfx.SetRenderTarget(maskTarget);
            gfx.Clear(ClearOptions.Target, Color.Transparent, 0, 0);

            // Draw the arena's walls.
            // TODO: Reuse one SpriteBatch instance. Creating a new one is slow.
            var spriteBatch = new SpriteBatch(gfx);
            spriteBatch.Begin();
            foreach (var wall in Arena.Gobs.GameplayLayer.Gobs.OfType<Wall>())
                wall.DrawSilhouette(view, projection, spriteBatch);
            spriteBatch.End();
            spriteBatch.Dispose();

            // Restore render target so what we can extract drawn pixels.
            // Create a copy of the texture in local memory so that a graphics device
            // reset (e.g. when changing resolution) doesn't lose the texture.
            gfx.SetRenderTarget(null);
            var textureData = new Color[targetHeight * targetWidth];
            maskTarget.GetData(textureData);
            _arenaRadarSilhouette = new Texture2D(gfx, targetWidth, targetHeight, false, SurfaceFormat.Color);
            _arenaRadarSilhouette.SetData(textureData);

            maskTarget.Dispose();
        }

        #endregion miscellaneous

        public void RemoveRemoteSpectators()
        {
            Spectators.Remove(spec => spec.IsRemote);
        }

        #region Private methods

        /// <summary>
        /// Refreshes <c>arenaToRadarTransform</c> and <c>arenaDimensionsOnRadar</c>
        /// according to the dimensions of the currently active arena.
        /// </summary>
        /// To be called whenever arena (or arena dimensions) change.
        /// <seealso cref="ArenaToRadarTransform"/>
        private void RefreshArenaToRadarTransform()
        {
            if (Arena == null)
                throw new InvalidOperationException("No active arena");
            Vector2 radarDisplayDimensions = new Vector2(200, 200); // TODO: Make this constant configurable
            Vector2 arenaDimensions = Arena.Dimensions;
            float arenaToRadarScale = Math.Min(
                radarDisplayDimensions.X / arenaDimensions.X,
                radarDisplayDimensions.Y / arenaDimensions.Y);
            _arenaDimensionsOnRadar = arenaDimensions * arenaToRadarScale;
            _arenaToRadarTransform =
                Matrix.CreateScale(arenaToRadarScale, -arenaToRadarScale, 1) *
                Matrix.CreateTranslation(0, _arenaDimensionsOnRadar.Y, 0);
        }

        private int GetFreeSpectatorID()
        {
            var usedIDs = Spectators.Select(spec => spec.ID);
            for (int id = 0; id <= byte.MaxValue; ++id)
                if (!usedIDs.Contains(id)) return id;
            throw new ApplicationException("There are no free spectator IDs");
        }

        private Color GetFreePlayerColor()
        {
            return GetPlayerColorPalette().Except(Players.Select(p => p.PlayerColor)).First();
        }

        private static IEnumerable<Color> GetPlayerColorPalette()
        {
            yield return Color.CornflowerBlue;
            yield return Color.DeepPink;
            yield return Color.Orange;
            yield return Color.Orchid;
            yield return Color.YellowGreen;
            yield return Color.MediumSpringGreen;
            yield return Color.HotPink;
            yield return Color.Aquamarine;
            yield return Color.Olive;
            yield return Color.OrangeRed;
            yield return Color.Thistle;
            yield return Color.Violet;
            yield return Color.Turquoise;
            yield return Color.Fuchsia;
            yield return Color.LightSteelBlue;
            yield return Color.Lime;
            // 16 colours total
        }

        #endregion Private methods

        #region Callbacks

        private void SpectatorAddedHandler(Spectator spectator)
        {
            spectator.Game = Game;
            spectator.ID = GetFreeSpectatorID();
            var player = spectator as Player;
            if (player != null && Game.NetworkMode != NetworkMode.Client)
            {
                player.PlayerColor = Color.Black; // reset to a color that won't affect free color picking
                player.PlayerColor = GetFreePlayerColor();
            }
            if (SpectatorAdded != null) SpectatorAdded(spectator);
        }

        private void SpectatorRemovedHandler(Spectator spectator)
        {
            spectator.Dispose();
            if (SpectatorRemoved != null) SpectatorRemoved(spectator);
        }

        #endregion Callbacks
    }
}
