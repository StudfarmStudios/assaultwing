using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Graphics;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
using AW2.Helpers.Collections;
using AW2.Net;
using AW2.Net.Messages;

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
    public class DataEngine
    {
        #region Fields

        /// <summary>
        /// Type templates, indexed by <see cref="CanonicalString.Canonical"/> of their type name.
        /// </summary>
        private List<object> _templates;

        private Arena _preparedArena;
        private Texture2D _arenaRadarSilhouette;
        private Vector2 _arenaDimensionsOnRadar;
        private Matrix _arenaToRadarTransform;
        private ProgressBar _progressBar;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Players and other spectators of the game session.
        /// </summary>
        public IndexedItemCollection<Spectator> Spectators { get; private set; }

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

        #endregion Properties

        public DataEngine()
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

            Viewports = new AWViewportCollection(0, null);
            _templates = new List<object>();
            ArenaPlaylist = new Playlist(new string[] { "Amazonas" });
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
                if (_arenaRadarSilhouette == null) RefreshArenaRadarSilhouette();
                return _arenaRadarSilhouette;
            }
        }

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
        /// Arenas to play in one session.
        /// </summary>
        public Playlist ArenaPlaylist { get; set; }

        /// <summary>
        /// Information about all available arenas.
        /// </summary>
        public List<ArenaInfo> ArenaInfos { get; set; }

        /// <summary>
        /// Advances the arena playlist and prepares the new current arena for playing.
        /// </summary>
        /// When the playing really should start, call <c>StartArena</c>.
        /// <returns><b>true</b> if the initialisation succeeded,
        /// <b>false</b> otherwise.</returns>
        public bool NextArena()
        {
            if (ArenaPlaylist.MoveNext())
            {
                var arenaFilename = ArenaInfos.Single(info => info.Name == ArenaPlaylist.Current).FileName;
                Arena arena = null;
                try
                {
                    arena = Arena.FromFile(arenaFilename);
                    InitializeFromArena(arena, true);
                    return true;
                }
                catch (Exception e)
                {
                    Log.Write("Failed to load arena: " + e);
                    return NextArena();
                }
            }
            else
            {
                Arena = null;
                return false;
            }
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
            if (Arena != null) Arena.Dispose();

            Arena = _preparedArena;
            _preparedArena = null;
            AssaultWing.Instance.GobsCounter.SetRawValue(Arena.Gobs.Count);
            if (Arena.IsForPlaying)
            {
                RefreshArenaToRadarTransform();
                RefreshArenaRadarSilhouette();
            }
        }

        /// <summary>
        /// Prepares the game data for playing an arena.
        /// </summary>
        /// When the playing really should start, call <c>StartArena</c>.
        /// <param name="initializeForPlaying">Should the arena be initialised
        /// for playing. If not, some initialisations are skipped.</param>
        public void InitializeFromArena(Arena arena, bool initializeForPlaying)
        {
            CustomOperations = null;
            _preparedArena = arena;
            _preparedArena.IsForPlaying = initializeForPlaying;
            if (initializeForPlaying) _preparedArena.Bin.Load(System.IO.Path.Combine(Paths.ARENAS, _preparedArena.BinFilename));
            AssaultWing.Instance.LoadArenaContent(_preparedArena);
            int wallCount = _preparedArena.Gobs.Count(gob => gob is Wall);
            _progressBar.SetSubtaskCount(wallCount);
            _preparedArena.Reset(); // this usually takes several seconds
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
            var localPlayers = AssaultWing.Instance.DataEngine.Spectators.Where(player => player.NeedsViewport);
            var playerEnumerator = localPlayers.GetEnumerator();
            AssaultWing.Instance.DataEngine.Viewports = new AWViewportCollection(localPlayers.Count(), rectangle =>
            {
                if (!playerEnumerator.MoveNext()) throw new ApplicationException("Ran out of players when assigning viewports");
                return playerEnumerator.Current.CreateViewport(rectangle);
            });
            playerEnumerator.Dispose();
        }

        /// <summary>
        /// Rearranges player viewports so that one player gets all screen space
        /// and the others get nothing.
        /// </summary>
        public void RearrangeViewports(int privilegedPlayer)
        {
            var player = AssaultWing.Instance.DataEngine.Spectators[privilegedPlayer];
            AssaultWing.Instance.DataEngine.Viewports = new AWViewportCollection(1, viewport => player.CreateViewport(viewport));
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
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                {
                    GobUpdateMessage message = null;
                    while ((message = AssaultWing.Instance.NetworkEngine.GameServerConnection.Messages.TryDequeue<GobUpdateMessage>()) != null)
                    {
                        var messageAge = AssaultWing.Instance.NetworkEngine.GetMessageAge(message);
                        message.ReadGobs(gobId =>
                        {
                            var theGob = Arena.Gobs.FirstOrDefault(gob => gob.ID == gobId);
                            return theGob == null || theGob.IsDisposed ? null : theGob;
                        }, SerializationModeFlags.VaryingData, messageAge);
                    }
                }
            }

            // Apply custom operations.
            if (CustomOperations != null)
                CustomOperations();
            CustomOperations = null;

            // Game client removes gobs as told by the game server.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                GobDeletionMessage message = null;
                while ((message = AssaultWing.Instance.NetworkEngine.GameServerConnection.Messages.TryDequeue<GobDeletionMessage>()) != null)
                {
                    Gob gob = Arena.Gobs.FirstOrDefault(gobb => gobb.ID == message.GobId);
                    if (gob == null)
                    {
                        // The gob hasn't been created yet. This happens when the server
                        // has created a gob and deleted it on the same frame, and
                        // the creation and deletion messages arrived just after we 
                        // finished receiving creation messages but right before we 
                        // started receiving deletion messages for this frame.
                        AssaultWing.Instance.NetworkEngine.GameServerConnection.Messages.Requeue(message);
                        break;
                    }
                    gob.DieOnClient();
                }
            }

            // Game server sends state updates about gobs to game clients.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                var now = ArenaTotalTime;
                var message = new GobUpdateMessage();
                foreach (var gob in Arena.Gobs.GameplayLayer.Gobs)
                {
                    if (!gob.IsRelevant) continue;
                    if (!gob.Movable) continue;
                    if (gob.LastNetworkUpdate + gob.NetworkUpdatePeriod > now) continue;
                    gob.LastNetworkUpdate = now;
                    message.AddGob(gob.ID, gob, SerializationModeFlags.VaryingData);
                }
                AssaultWing.Instance.NetworkEngine.SendToGameClients(message);
            }

#if DEBUG_PROFILE
            AssaultWing.Instance.GobCount = Arena.Gobs.Count;
#endif
        }

        public void ProcessGobCreationMessage(GobCreationMessage message, TimeSpan messageAge)
        {
            Gob gob = (Gob)Clonable.Instantiate(message.GobTypeName);
            message.Read(gob, SerializationModeFlags.All, messageAge);
            if (message.CreateToNextArena)
            {
                gob.Layer = _preparedArena.Layers[message.LayerIndex];
                _preparedArena.Gobs.Add(gob);
            }
            else
            {
                gob.Layer = Arena.Layers[message.LayerIndex];
                Arena.Gobs.Add(gob);
            }
            // Ships we set automatically as the ship the ship's owner is controlling.
            Ship gobShip = gob as Ship;
            if (gobShip != null)
                gobShip.Owner.Ship = gobShip;
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
            Viewports = new AWViewportCollection(0, null);
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
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            GraphicsDeviceCapabilities gfxCaps = gfx.GraphicsDeviceCapabilities;
            int targetWidth = (int)_arenaDimensionsOnRadar.X;
            int targetHeight = (int)_arenaDimensionsOnRadar.Y;
            GraphicsAdapter gfxAdapter = gfx.CreationParameters.Adapter;
            if (!gfxAdapter.CheckDeviceFormat(DeviceType.Hardware, gfx.DisplayMode.Format,
                TextureUsage.None, QueryUsages.None, ResourceType.RenderTarget, SurfaceFormat.Color))
                throw new Exception("Cannot create render target of type SurfaceFormat.Color");
            RenderTarget2D maskTarget = new RenderTarget2D(gfx, targetWidth, targetHeight,
                1, SurfaceFormat.Color);

            // Set up graphics device.
            DepthStencilBuffer oldDepthStencilBuffer = gfx.DepthStencilBuffer;
            gfx.DepthStencilBuffer = null;

            // Set up draw matrices.
            Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 500), Vector3.Zero, Vector3.Up);
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Arena.Dimensions.X,
                0, Arena.Dimensions.Y, 10, 1000);

            // Set and clear our own render target.
            gfx.SetRenderTarget(0, maskTarget);
            gfx.Clear(ClearOptions.Target, Color.TransparentBlack, 0, 0);

            // Draw the arena's walls.
            SpriteBatch spriteBatch = new SpriteBatch(gfx);
            spriteBatch.Begin();
            foreach (var gob in Arena.Gobs.GameplayLayer.Gobs)
            {
                Wall wall = gob as Wall;
                if (wall != null)
                    wall.DrawSilhouette(view, projection, spriteBatch);
            }
            spriteBatch.End();

            // Restore render target so what we can extract drawn pixels.
            // Create a copy of the texture in local memory so that a graphics device
            // reset (e.g. when changing resolution) doesn't lose the texture.
            gfx.SetRenderTarget(0, null);
            Color[] textureData = new Color[targetHeight * targetWidth];
            maskTarget.GetTexture().GetData(textureData);
            _arenaRadarSilhouette = new Texture2D(gfx, targetWidth, targetHeight, 1, TextureUsage.None, SurfaceFormat.Color);
            _arenaRadarSilhouette.SetData(textureData);

            // Restore graphics device's old settings.
            gfx.DepthStencilBuffer = oldDepthStencilBuffer;
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
            spectator.ID = GetFreeSpectatorID();
            var player = spectator as Player;
            if (player != null && AssaultWing.Instance.NetworkMode != NetworkMode.Client)
            {
                player.PlayerColor = Color.Black; // reset to a color that won't affect free color picking
                player.PlayerColor = GetFreePlayerColor();
            }
        }

        private void SpectatorRemoved(Spectator spectator)
        {
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                var message = new PlayerDeletionMessage { PlayerID = spectator.ID };
                AssaultWing.Instance.NetworkEngine.SendToGameClients(message);
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

        #endregion Private methods
    }
}
