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
using AW2.Helpers.Serialization;
using AW2.Net.Messages;
using AW2.Net.ManagementMessages;

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
        #region Fields

        /// <summary>
        /// Type templates, indexed by <see cref="CanonicalString.Canonical"/> of their type name.
        /// </summary>
        private List<object> _templates;

        private Texture2D _arenaRadarSilhouette;
        private Vector2 _arenaDimensionsOnRadar;
        private Matrix _arenaToRadarTransform;
        private TimeSpan _lastArenaRadarSilhouetteUpdate;
        private ProgressBar _progressBar;
        private IndexedItemCollection<Spectator> _spectators;

        #endregion Fields

        #region Properties

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

        /// <summary>
        /// Players of the game session.
        /// </summary>
        public IEnumerable<Player> Players
        {
            get { return Spectators.Where(p => p is Player).Cast<Player>(); }
        }

        /// <summary>
        /// Weapons that are active in the game session.
        /// </summary>
        public IndexedItemCollection<ShipDevice> Devices { get; private set; }

        /// <summary>
        /// The currently active arena.
        /// </summary>
        /// Use <see cref="InitializeFromArena(string)"/> to change the active arena.
        public Arena Arena { get; private set; }

        public TimeSpan ArenaTotalTime { get { return Arena == null ? TimeSpan.Zero : Arena.TotalTime; } }
        public int ArenaFrameCount { get { return Arena == null ? 0 : Arena.FrameNumber; } }

        /// <summary>
        /// Called when a new <see cref="Arena"/> is instantiated.
        /// </summary>
        public event Action<Arena> NewArena;

        #endregion Properties

        public DataEngine(AssaultWingCore game)
            : base(game)
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
            _templates = new List<object>();
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

        /// <summary>
        /// Information about all available arenas.
        /// </summary>
        public List<ArenaInfo> ArenaInfos { get; set; }

        /// <summary>
        /// Advances the arena playlist and prepares the new current arena for playing.
        /// When the playing really should start, call <see cref="StartArena"/>
        /// </summary>
        public void NextArena(string arenaName)
        {
            if (Arena != null) Arena.Dispose();
            var arenaFilename = ArenaInfos.Single(info => info.Name == arenaName).FileName;
            var arena = Arena.FromFile(Game, arenaFilename);
            if (NewArena != null) NewArena(arena);
            InitializeFromArena(arena, true);
        }

        /// <summary>
        /// Sets a previously prepared arena as the active one.
        /// </summary>
        /// Call this method right before commencing play in the prepared arena.
        public void StartArena()
        {
            // Clear old stuff from previous arena, if any.
            Devices.Clear();
            foreach (var player in Spectators) player.ResetForArena();
            Game.GobsCounter.SetRawValue(Arena.Gobs.Count);
            if (Arena.IsForPlaying)
            {
                RefreshArenaToRadarTransform();
                RefreshArenaRadarSilhouette();
            }
        }

        /// <summary>
        /// Prepares the game data for playing an arena.
        /// When the playing really should start, call <c>StartArena</c>.
        /// </summary>
        /// <param name="initializeForPlaying">Should the arena be initialised
        /// for playing. If not, some initialisations are skipped.</param>
        public void InitializeFromArena(Arena arena, bool initializeForPlaying)
        {
            CustomOperations = null;
            Arena = arena;
            Arena.IsForPlaying = initializeForPlaying;
            if (initializeForPlaying) Arena.Bin.Load(System.IO.Path.Combine(Paths.ARENAS, Arena.BinFilename));
            Game.LoadArenaContent(Arena);
            int wallCount = Arena.Gobs.Count(gob => gob is Wall);
            _progressBar.SetSubtaskCount(wallCount);
            Arena.Reset(); // this usually takes several seconds
        }

        #endregion arenas

        #region type templates

        /// <summary>
        /// Saves an object to be used as a template for a user-defined named type.
        /// </summary>
        public void AddTypeTemplate(CanonicalString typeName, object template)
        {
            while (_templates.Count < typeName.Canonical + 1) _templates.Add(null);
            if (_templates[typeName.Canonical] != null)
                Log.Write("WARNING: Overwriting template for user-defined type " + typeName);
            _templates[typeName.Canonical] = template;
        }

        public object GetTypeTemplate(CanonicalString typeName)
        {
            var item = _templates.ElementAtOrDefault(typeName.Canonical);
            if (item == null) throw new ApplicationException("Missing template for user-defined type " + typeName);
            return item;
        }

        /// <summary>
        /// Performs the specified action on each user-defined type template
        /// that was stored under the given base class.
        /// </summary>
        /// <seealso cref="DataEngine.AddTypeTemplate"/>
        /// <typeparam name="T">Base class of templates to loop through.</typeparam>
        /// <param name="action">The Action delegate to perform on each template.</param>
        public void ForEachTypeTemplate<T>(Action<T> action)
        {
            foreach (var template in _templates)
                if (template != null && template is T) action((T)template);
        }

        #endregion type templates

        #region viewports

        public AWViewportCollection Viewports { get; private set; }

        public void RearrangeViewports()
        {
            var localPlayers = Game.DataEngine.Spectators.Where(player => player.NeedsViewport).ToList();
            Viewports = new AWViewportCollection(Game.GraphicsDeviceService, localPlayers.Count(),
                (index, rectangle) => localPlayers[index].CreateViewport(rectangle));
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

        /// <summary>
        /// The current mode of gameplay.
        /// </summary>
        public GameplayMode GameplayMode { get; set; }

        /// <summary>
        /// The progress bar.
        /// </summary>
        /// Anybody can tell the progress bar that their tasks are completed.
        /// This way the progress bar knows how its running task is progressing.
        public ProgressBar ProgressBar
        {
            get
            {
                if (_progressBar == null) _progressBar = new ProgressBar();
                return _progressBar;
            }
        }

        /// <summary>
        /// Custom operations to perform once at the end of a frame.
        /// </summary>
        /// This event is called by the data engine after all updates of a frame.
        /// You can add your own delegate to this event to postpone it after 
        /// everything else is calculated in the frame.
        /// The parameter to the Action delegate is undefined.
        public event Action CustomOperations;

        /// <summary>
        /// Commits pending operations. This method should be called at the end of each frame.
        /// </summary>
        public void CommitPending()
        {
            // Game client updates gobs and players as told by the game server.
            if (Game.NetworkMode == NetworkMode.Client)
            {
                {
                    GobUpdateMessage message = null;
                    while ((message = Game.NetworkEngine.GameServerConnection.TryDequeueMessage<GobUpdateMessage>()) != null)
                    {
                        var framesAgo = Game.NetworkEngine.GetMessageAge(message);
                        message.ReadGobs(gobId =>
                        {
                            var theGob = Arena.Gobs.FirstOrDefault(gob => gob.ID == gobId);
                            return theGob == null || theGob.IsDisposed ? null : theGob;
                        }, SerializationModeFlags.VaryingData, framesAgo);
                    }
                }
            }

            // Apply custom operations.
            if (CustomOperations != null)
                CustomOperations();
            CustomOperations = null;

            // Game client removes gobs as told by the game server.
            if (Game.NetworkMode == NetworkMode.Client)
            {
                GobDeletionMessage message = null;
                while ((message = Game.NetworkEngine.GameServerConnection.TryDequeueMessage<GobDeletionMessage>()) != null)
                {
                    Gob gob = Arena.Gobs.FirstOrDefault(gobb => gobb.ID == message.GobId);
                    if (gob == null)
                    {
                        // The gob hasn't been created yet. This happens when the server
                        // has created a gob and deleted it on the same frame, and
                        // the creation and deletion messages arrived just after we 
                        // finished receiving creation messages but right before we 
                        // started receiving deletion messages for this frame.
                        Game.NetworkEngine.GameServerConnection.Messages.Requeue(message);
                        break;
                    }
                    gob.DieOnClient();
                }
            }

            // Game server sends state updates about gobs to game clients.
            if (Game.NetworkMode == NetworkMode.Server)
            {
                var now = ArenaTotalTime;
                var message = new GobUpdateMessage();
                foreach (var gob in Arena.Gobs.GameplayLayer.Gobs)
                {
                    if (!gob.ForcedNetworkUpdate)
                    {
                        if (!gob.IsRelevant) continue;
                        if (!gob.Movable) continue;
                        if (gob.NetworkUpdatePeriod == TimeSpan.Zero) continue;
                        if (gob.LastNetworkUpdate + gob.NetworkUpdatePeriod > now) continue;
                    }
                    gob.LastNetworkUpdate = now;
                    message.AddGob(gob.ID, gob, SerializationModeFlags.VaryingData);
                }
                Game.NetworkEngine.SendToGameClients(message);
            }

#if DEBUG_PROFILE
            AssaultWing.Instance.GobCount = Arena.Gobs.Count;
#endif
        }

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
        public void UnloadContent()
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
            gfxAdapter.QueryRenderTargetFormat(GraphicsProfile.HiDef, SurfaceFormat.Color, DepthFormat.None, 1, out selectedFormat, out selectedDepthFormat, out selectedMultiSampleCount);
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

        private void SpectatorAdded(Spectator spectator)
        {
            spectator.Game = (AssaultWingCore)this.Game;
            spectator.ID = GetFreeSpectatorID();
            var player = spectator as Player;
            if (player != null && Game.NetworkMode != NetworkMode.Client)
            {
                player.PlayerColor = Color.Black; // reset to a color that won't affect free color picking
                player.PlayerColor = GetFreePlayerColor();
            }
        }

        private void SpectatorRemoved(Spectator spectator)
        {
            if (Game.NetworkMode == NetworkMode.Server)
            {
                var message = new PlayerDeletionMessage { PlayerID = spectator.ID };
                Game.NetworkEngine.SendToGameClients(message);
            }
            spectator.Dispose();
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

        private void UpdateGameServerInfoToManagementServer()
        {
            var mess = new UpdateGameServerMessage { CurrentClients = Players.Count() };
            Game.NetworkEngine.ManagementServerConnection.Send(mess);
        }

        #endregion Private methods

        #region Callbacks

        private void SpectatorAddedHandler(Spectator spectator)
        {
            if (Game.NetworkMode == NetworkMode.Server)
                UpdateGameServerInfoToManagementServer();
        }

        private void SpectatorRemovedHandler(Spectator spectator)
        {
            if (Game.NetworkMode == NetworkMode.Server)
                UpdateGameServerInfoToManagementServer();
        }

        #endregion Callbacks
    }
}
