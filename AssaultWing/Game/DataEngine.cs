using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Gobs;
using AW2.Graphics;
using AW2.Helpers;
using AW2.Helpers.Collections;
using AW2.Net;
using AW2.Net.Messages;
using Viewport = AW2.Graphics.AWViewport;

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
        class NamedDataCollection<T> : NamedItemCollection<T> where T : class
        {
            public NamedDataCollection(string kindName, CanonicalString substituteName)
            {
                if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
                    Removed += item => ((IDisposable)item).Dispose();
                NotFound += obj =>
                {
                    Log.Write(string.Format("Warning: {0} {1} not found", kindName, obj.ToString()));
                    T substitute;
                    if (TryGetValue(substituteName, out substitute))
                        return substitute;
                    string message = string.Format("Missing {0} {1} and default {2}", kindName, obj.ToString(), substituteName);
                    throw new KeyNotFoundException(message);
                };
            }
        }

        #region Fields

        List<Viewport> viewports;
        List<ViewportSeparator> viewportSeparators;
        Dictionary<string, string> arenaFileNameList;

        /// <summary>
        /// Type templates, as a list "indexed" by base class (such as 'typeof(Gob)'), 
        /// yielding a list "indexed" by template type name (such as "rocket"),
        /// yielding the template.
        /// </summary>
        List<Pair<Type, List<Pair<string, object>>>> templates;

        Dictionary<string, Arena> arenas;
        Arena preparedArena;
        Texture2D arenaRadarSilhouette;
        Vector2 arenaDimensionsOnRadar;
        Matrix arenaToRadarTransform;
        ProgressBar progressBar;

        /// <summary>
        /// The textures used in static graphics, indexed by <c>TextureName</c>.
        /// </summary>
        Texture2D[] overlays;

        /// <summary>
        /// The fonts used in static graphics, indexed by <c>FontName</c>.
        /// </summary>
        SpriteFont[] fonts;

        /// <summary>
        /// The viewport we are currently drawing into.
        /// </summary>
        Viewport activeViewport;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Players who participate in the game session.
        /// </summary>
        public IndexedItemCollection<Player> Players { get; private set; }

        /// <summary>
        /// Weapons that are active in the game session.
        /// </summary>
        public IndexedItemCollection<Weapon> Weapons { get; private set; }

        /// <summary>
        /// The currently active arena.
        /// </summary>
        /// Use <see cref="InitializeFromArena(string)"/> to change the active arena.
        public Arena Arena { get; private set; }

        /// <summary>
        /// 3D models available for the game.
        /// </summary>
        public NamedItemCollection<Model> Models { get; private set; }

        /// <summary>
        /// Textures available for the game.
        /// </summary>
        public NamedItemCollection<Texture2D> Textures { get; private set; }

        /// <summary>
        /// Previews of arenas.
        /// </summary>
        public NamedItemCollection<Texture2D> ArenaPreviews { get; private set; }

        #endregion Properties

        /// <summary>
        /// Creates a new data engine.
        /// </summary>
        public DataEngine()
        {
            Players = new IndexedItemCollection<Player>();
            Players.Removed += player => 
            {
                if (player.Ship != null) 
                    player.Ship.Die(new DeathCause());
                player.Controls.thrust.Release();
                player.Controls.left.Release();
                player.Controls.right.Release();
                player.Controls.down.Release();
                player.Controls.fire1.Release();
                player.Controls.fire2.Release();
                player.Controls.extra.Release();
            };

            Weapons = new IndexedItemCollection<Weapon>();
            Weapons.Added += weapon => { weapon.Arena = Arena; };

            Models = new NamedDataCollection<Model>("model", (CanonicalString)"dummymodel");
            Textures = new NamedDataCollection<Texture2D>("texture", (CanonicalString)"dummytexture");
            ArenaPreviews = new NamedDataCollection<Texture2D>("arena preview", (CanonicalString)"noPreview");

            viewports = new List<Viewport>();
            viewportSeparators = new List<ViewportSeparator>();
            templates = new List<Pair<Type, List<Pair<string, object>>>>();
            arenas = new Dictionary<string, Arena>();
            overlays = new Texture2D[Enum.GetValues(typeof(TextureName)).Length];
            fonts = new SpriteFont[Enum.GetValues(typeof(FontName)).Length];
            ArenaPlaylist = new Playlist(new string[] { "dummyarena" });
        }

        #region textures

        /// <summary>
        /// Stores a static 2D texture by name, overwriting any texture previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the 2D texture.</param>
        /// <param name="texture">The 2D texture.</param>
        public void AddTexture(TextureName name, Texture2D texture)
        {
            overlays[(int)name] = texture;
        }

        /// <summary>
        /// Returns a 2D texture used in static graphics.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public Texture2D GetTexture(TextureName name)
        {
            return overlays[(int)name];
        }

        #endregion textures

        #region fonts

        /// <summary>
        /// Stores a static font by name, overwriting any font previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <param name="font">The font.</param>
        public void AddFont(FontName name, SpriteFont font)
        {
            fonts[(int)name] = font;
        }

        /// <summary>
        /// Returns a font used in static graphics.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <returns>The font.</returns>
        public SpriteFont GetFont(FontName name)
        {
            return fonts[(int)name];
        }

        /// <summary>
        /// Disposes of all fonts.
        /// </summary>
        public void ClearFonts()
        {
            for (int i = 0; i < fonts.Length; ++i)
                fonts[i] = null;
        }

        #endregion fonts

        #region arenas

        /// <summary>
        /// The currently active arena's silhouette, scaled and ready to be 
        /// drawn in a player's viewport's radar display.
        /// </summary>
        public Texture2D ArenaRadarSilhouette { get { return arenaRadarSilhouette; } }

        /// <summary>
        /// The transformation to map coordinates in the current arena 
        /// into player viewport radar display coordinates.
        /// </summary>
        /// Arena origin is the lower left corner, positive X is to the right,
        /// and positive Y is up. Radar display origin is the top left corner
        /// of the radar display area, positive X is to the right, and positive
        /// Y is down.
        public Matrix ArenaToRadarTransform { get { return arenaToRadarTransform; } }

        /// <summary>
        /// Arenas to play in one session.
        /// </summary>
        public Playlist ArenaPlaylist { get; set; }

        /// <summary>
        /// Mapping of all available arenas to the names of the files 
        /// where the arenas are defined.
        /// </summary>
        public Dictionary<string, string> ArenaFileNameList
        {
            get { return arenaFileNameList; }
            set { arenaFileNameList = value; }
        }

        /// <summary>
        /// Returns a named arena.
        /// </summary>
        private Arena GetArena(string name)
        {
            if (preparedArena == null || !preparedArena.Name.Equals(name))
            {
                TypeLoader arenaLoader = new TypeLoader(typeof(Arena), Paths.Arenas);
                preparedArena = (Arena)arenaLoader.LoadSpecifiedTypes(arenaFileNameList[name]);
            }
            return preparedArena;
        }

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
                InitializeFromArena(ArenaPlaylist.Current);
                return true;
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
            // Reset players.
            foreach (var player in Players)
            {
                player.Reset();
                player.Lives = AssaultWing.Instance.NetworkMode == NetworkMode.Standalone
                    ? 3 // HACK: standalone games have three lives
                    : -1; // HACK: network games have infinite lives
            }

            Arena = preparedArena;
            preparedArena = null;
            RefreshArenaToRadarTransform();
            RefreshArenaRadarSilhouette();
        }

        /// <summary>
        /// Prepares the game data for playing an arena.
        /// </summary>
        /// When the playing really should start, call <c>StartArena</c>.
        /// <param name="name">The name of the arena.</param>
        public void InitializeFromArena(string name)
        {
            // Clear remaining data from a possible previous arena.
            if (Arena != null) Arena.Dispose();
            Weapons.Clear();
            CustomOperations = null;

            preparedArena = GetArena(name);
            preparedArena.Reset();
            AssaultWing.Instance.LoadArenaContent(preparedArena);

            // Create initial objects. This is by far the most time consuming part
            // in initialising an arena for playing.
            int wallCount = preparedArena.Gobs.Count(gob => gob is Wall);
            progressBar.SetSubtaskCount(wallCount);
            foreach (var gob in preparedArena.Gobs) preparedArena.Prepare(gob);
        }

        /// <summary>
        /// Performs the specified action on each arena.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each arena.</param>
        public void ForEachArena(Action<Arena> action)
        {
            foreach (Arena arena in arenas.Values)
                action(arena);
        }

        #endregion arenas

        #region type templates

        /// <summary>
        /// Saves an instance as a template for a user-defined type with the given name.
        /// </summary>
        /// User-defined types for some base class are defined by templates that are instances
        /// of some subclass of the base class. Template instances carry values for certain 
        /// fields (called 'type parameters') of the subclasses, and the values are used in 
        /// initialising the fields when an instance of the user-defined type is created 
        /// during gameplay.
        /// <param name="baseClass">The base class of the user-defined type, e.g. Gob or Weapon.</param>
        /// <param name="typeName">The name of the user-defined type, e.g. "explosion" or "shotgun".</param>
        /// <param name="template">The instance to save as a template for the user-defined type.</param>
        public void AddTypeTemplate(Type baseClass, string typeName, object template)
        {
            CanonicalString.Register(typeName);
            var baseClassKey = templates.Find(x => x.First.Equals(baseClass));
            if (baseClassKey != null)
            {
                var typeNameKey = baseClassKey.Second.Find(x => x.First == typeName);
                if (typeNameKey != null)
                {
                    Log.Write("WARNING: Overwriting user-defined type " + baseClass.Name + "/" + typeName);
                    typeNameKey.Second = template;
                }
                else
                {
                    baseClassKey.Second.Add(new Pair<string, object>(typeName, template));
                }
            }
            else
            {
                List<Pair<string, object>> newList = new List<Pair<string, object>>();
                newList.Add(new Pair<string, object>(typeName, template));
                templates.Add(new Pair<Type, List<Pair<string, object>>>(baseClass, newList));
            }
        }

        /// <summary>
        /// Returns the template instance that defines the named user-defined type.
        /// </summary>
        /// <see cref="DataEngine.AddTypeTemplate"/>
        /// <param name="baseClass">The base class of the user-defined type, e.g. Gob or Weapon.</param>
        /// <param name="typeName">The name of the user-defined type, e.g. "explosion" or "shotgun".</param>
        /// <returns>The template instance that defines the named user-defined type.</returns>
        public object GetTypeTemplate(Type baseClass, string typeName)
        {
            foreach (Pair<Type, List<Pair<string, object>>> typePair in templates)
                if (typePair.First.Equals(baseClass))
                {
                    foreach (Pair<string, object> namePair in typePair.Second)
                        if (namePair.First == typeName)
                            return namePair.Second;

                    // Proper value not found. Try to find a dummy value.
                    Log.Write("Missing template for user-defined type " + baseClass.Name + "/" + typeName);
                    string fallbackTypeName = "dummy" + baseClass.Name.ToLower() + "type";
                    foreach (Pair<string, object> namePair in typePair.Second)
                        if (namePair.First == fallbackTypeName)
                            return namePair.Second;
                    throw new Exception("Missing templates for user-defined type " + baseClass.Name + "/" + typeName + " and fallback " + fallbackTypeName);
                }
            throw new Exception("Missing templates for user-defined type " + baseClass.Name + "/" + typeName + " (no templates for the whole base class)");
        }

        /// <summary>
        /// Tells if a name corresponds to any user-defined type template.
        /// </summary>
        /// <param name="baseClass">The base class of the user-defined type template, e.g. Gob or Weapon.</param>
        /// <param name="typeName">The name of the user-defined type template.</param>
        /// <returns><c>true</c> if there is a user-defined type template with the name, <c>false</c> otherwise.</returns>
        public bool HasTypeTemplate(Type baseClass, string typeName)
        {
            foreach (Pair<Type, List<Pair<string, object>>> typePair in templates)
                if (typePair.First.Equals(baseClass))
                    foreach (Pair<string, object> namePair in typePair.Second)
                        if (namePair.First == typeName)
                            return true;
            return false;
        }

        /// <summary>
        /// Performs the specified action on each user-defined type template
        /// that was stored under the given base class.
        /// </summary>
        /// <see cref="DataEngine.AddTypeTemplate"/>
        /// <typeparam name="T">Base class of templates to loop through.</typeparam>
        /// <param name="action">The Action delegate to perform on each template.</param>
        public void ForEachTypeTemplate<T>(Action<T> action)
        {
            foreach (Pair<Type, List<Pair<string, object>>> typePair in templates)
                if (typePair.First.Equals(typeof(T)))
                    foreach (Pair<string, object> namePair in typePair.Second)
                        action((T)namePair.Second);
        }

        #endregion type templates

        #region viewports

        /// <summary>
        /// The viewport we are currently drawing into.
        /// </summary>
        public Viewport Viewport { get { return activeViewport; } }

        /// <summary>
        /// Adds a viewport.
        /// </summary>
        /// <param name="viewport">Viewport to add.</param>
        public void AddViewport(Viewport viewport)
        {
            viewport.LoadContent();
            viewports.Add(viewport);
        }

        /// <summary>
        /// Adds a viewport separator to be displayed.
        /// </summary>
        /// <param name="separator">The viewport separator.</param>
        public void AddViewportSeparator(ViewportSeparator separator)
        {
            viewportSeparators.Add(separator);
        }

        /// <summary>
        /// Removes all viewports and viewport separators.
        /// </summary>
        public void ClearViewports()
        {
            foreach (AWViewport viewport in viewports)
                viewport.UnloadContent();
            viewports.Clear();
            viewportSeparators.Clear();
        }

        /// <summary>
        /// Performs the specified action on each viewport.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each viewport.</param>
        public void ForEachViewport(Action<Viewport> action)
        {
            foreach (Viewport viewport in viewports)
            {
                activeViewport = viewport;
                action(viewport);
            }
        }

        /// <summary>
        /// Performs the specified action on each viewport separator.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each viewport separator.</param>
        public void ForEachViewportSeparator(Action<ViewportSeparator> action)
        {
            foreach (ViewportSeparator separator in viewportSeparators)
            {
                action(separator);
            }
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
                if (progressBar == null) progressBar = new ProgressBar();
                return progressBar;
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
        /// Sets lighting for the effect.
        /// </summary>
        /// <param name="effect">The effect to modify.</param>
        public void PrepareEffect(BasicEffect effect)
        {
            Arena.PrepareEffect(effect);
        }

        /// <summary>
        /// Commits pending operations. This method should be called at the end of each frame.
        /// </summary>
        public void CommitPending()
        {
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
                CheckGobCreationMessages();

            // Game client updates gobs and players as told by the game server.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                {
                    GobUpdateMessage message = null;
                    while ((message = AssaultWing.Instance.NetworkEngine.GameServerConnection.Messages.TryDequeue<GobUpdateMessage>()) != null)
                        message.ReadGobs(gobId => Arena.Gobs.FirstOrDefault(gob => gob.Id == gobId), SerializationModeFlags.VaryingData);
                }
                {
                    GobDamageMessage message = null;
                    while ((message = AssaultWing.Instance.NetworkEngine.GameServerConnection.Messages.TryDequeue<GobDamageMessage>()) != null)
                    {
                        Gob gob = Arena.Gobs.FirstOrDefault(gobb => gobb.Id == message.GobId);
                        if (gob == null) continue; // Skip updates for gobs we haven't yet created.
                        gob.DamageLevel = message.DamageLevel;
                    }
                }
                {
                    PlayerUpdateMessage message = null;
                    while ((message = AssaultWing.Instance.NetworkEngine.GameServerConnection.Messages.TryDequeue<PlayerUpdateMessage>()) != null)
                    {
                        Player player = Players.FirstOrDefault(plr => plr.Id == message.PlayerId);
                        if (player == null) throw new ArgumentException("Update for unknown player ID " + message.PlayerId);
                        message.Read(player, SerializationModeFlags.VaryingData);
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
                    Gob gob = Arena.Gobs.FirstOrDefault(gobb => gobb.Id == message.GobId);
                    if (gob == null)
                    {
                        // The gob hasn't been created yet. This happens when the server
                        // has created a gob and deleted it on the same frame, and
                        // the creation and deletion messages arrived just after we 
                        // finished receiving creation messages but right before we 
                        // started receiving deletion messages for this frame.
                        break;
                    }
                    gob.DieOnClient();
                }
            }

            // Game server sends state updates about gobs to game clients.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                TimeSpan now = AssaultWing.Instance.GameTime.TotalGameTime;
                var message = new GobUpdateMessage();
                foreach (var gob in Arena.Gobs.GameplayLayer.Gobs)
                {
                    if (!gob.IsRelevant) return;
                    if (!gob.Movable) return;
                    if (gob.LastNetworkUpdate + gob.NetworkUpdatePeriod > now) return;
                    gob.LastNetworkUpdate = now;
                    message.AddGob(gob.Id, gob, SerializationModeFlags.VaryingData);
                }
                AssaultWing.Instance.NetworkEngine.GameClientConnections.Send(message);
            }

            // Game server sends state updates about players to game clients.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                foreach (var player in Players)
                {
                    if (!player.MustUpdateToClients) return;
                    player.MustUpdateToClients = false;
                    var message = new PlayerUpdateMessage();
                    message.PlayerId = player.Id;
                    message.Write(player, SerializationModeFlags.VaryingData);
                    AssaultWing.Instance.NetworkEngine.GameClientConnections.Send(message);
                }
            }

#if DEBUG_PROFILE
            AssaultWing.Instance.gobCount = Arena.Gobs.Count;
#endif
        }

        /// <summary>
        /// Reacts to gob creation messages received from the game server.
        /// </summary>
        public void CheckGobCreationMessages()
        {
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Client)
                throw new InvalidOperationException("Only clients should listen to gob creation messages");
            GobCreationMessage message = null;
            while ((message = AssaultWing.Instance.NetworkEngine.GameServerConnection.Messages.TryDequeue<GobCreationMessage>()) != null)
            {
                Gob gob = Gob.CreateGob(message.GobTypeName);
                message.Read(gob, SerializationModeFlags.All);
                if (message.CreateToNextArena)
                {
                    gob.Layer = preparedArena.Layers[message.LayerIndex];
                    preparedArena.Gobs.Add(gob);
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
            ClearViewports();
        }

        /// <summary>
        /// Loads content needed by the currently active arena.
        /// </summary>
        public void LoadContent()
        {
            if (Arena != null)
                RefreshArenaRadarSilhouette();
        }

        /// <summary>
        /// Unloads content needed by the currently active arena.
        /// </summary>
        public void UnloadContent()
        {
            if (arenaRadarSilhouette != null)
            {
                arenaRadarSilhouette.Dispose();
                arenaRadarSilhouette = null;
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
            if (arenaRadarSilhouette != null)
            {
                arenaRadarSilhouette.Dispose();
                arenaRadarSilhouette = null;
            }

            // Draw arena walls in one color in a radar-sized texture.
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
            GraphicsDeviceCapabilities gfxCaps = gfx.GraphicsDeviceCapabilities;
            int targetWidth = (int)arenaDimensionsOnRadar.X;
            int targetHeight = (int)arenaDimensionsOnRadar.Y;
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
            arenaRadarSilhouette = new Texture2D(gfx, targetWidth, targetHeight, 1, TextureUsage.None, SurfaceFormat.Color);
            arenaRadarSilhouette.SetData(textureData);

            // Restore graphics device's old settings.
            gfx.DepthStencilBuffer = oldDepthStencilBuffer;
            maskTarget.Dispose();
        }

        #endregion miscellaneous

        #region Private methods

        /// <summary>
        /// Refreshes <c>arenaToRadarTransform</c> and <c>arenaDimensionsOnRadar</c>
        /// according to the dimensions of the currently active arena.
        /// </summary>
        /// To be called whenever arena (or arena dimensions) change.
        /// <seealso cref="ArenaToRadarTransform"/>
        void RefreshArenaToRadarTransform()
        {
            if (Arena == null)
                throw new InvalidOperationException("No active arena");
            Vector2 radarDisplayDimensions = new Vector2(162, 150); // TODO: Make this constant configurable
            Vector2 arenaDimensions = Arena.Dimensions;
            float arenaToRadarScale = Math.Min(
                radarDisplayDimensions.X / arenaDimensions.X,
                radarDisplayDimensions.Y / arenaDimensions.Y);
            arenaDimensionsOnRadar = arenaDimensions * arenaToRadarScale;
            arenaToRadarTransform =
                Matrix.CreateScale(arenaToRadarScale, -arenaToRadarScale, 1) *
                Matrix.CreateTranslation(0, arenaDimensionsOnRadar.Y, 0);
        }

        #endregion Private methods
    }
}
