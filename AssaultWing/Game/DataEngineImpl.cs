using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using AW2;
using AW2.Graphics;
using Viewport = AW2.Graphics.AWViewport;
using AW2.Helpers;
using TypeStringPair = System.Collections.Generic.KeyValuePair<System.Type, string>;
using Microsoft.Xna.Framework;
using AW2.Game.Particles;
using AW2.Game.Gobs;

namespace AW2.Game
{
    /// <summary>
    /// Basic implementation of game data.
    /// </summary>
    class DataEngineImpl : DataEngine
    {
        LinkedList<Gob> gobs;
        LinkedList<ParticleEngine> particleEngines;
        LinkedList<Weapon> weapons;
        List<Gob> addedGobs;
        List<Gob> removedGobs;
        List<ParticleEngine> removedParticleEngines;
        LinkedList<Player> players;
        List<Viewport> viewports;
        List<ViewportSeparator> viewportSeparators;
        Dictionary<string, Model> models;
        Dictionary<string, Texture2D> textures;
        Dictionary<TypeStringPair, object> templates;
        Dictionary<string, Arena> arenas;
        Arena activeArena;
        Texture2D arenaRadarSilhouette;
        Vector2 arenaDimensionsOnRadar;
        Matrix arenaToRadarTransform;

        /// <summary>
        /// The textures used in static graphics, indexed by <c>TextureName</c>.
        /// </summary>
        Texture2D[] overlays;

        /// <summary>
        /// The fonts used in static graphics, indexed by <c>FontName</c>.
        /// </summary>
        SpriteFont[] fonts;

        /// <summary>
        /// Arenas to play in one session.
        /// </summary>
        List<string> arenaPlaylist;

        /// <summary>
        /// Index of current arena in arena playlist,
        /// or -1 if there is no current arena.
        /// </summary>
        int arenaPlaylistI;

        /// <summary>
        /// The viewport we are currently drawing into.
        /// </summary>
        Viewport activeViewport;

        /// <summary>
        /// Creates a new data engine.
        /// </summary>
        public DataEngineImpl()
        {
            gobs = new LinkedList<Gob>();
            addedGobs = new List<Gob>();
            removedGobs = new List<Gob>();
            particleEngines = new LinkedList<ParticleEngine>();
            removedParticleEngines = new List<ParticleEngine>();
            weapons = new LinkedList<Weapon>();
            players = new LinkedList<Player>();
            viewports = new List<Viewport>();
            viewportSeparators = new List<ViewportSeparator>();
            models = new Dictionary<string, Model>();
            textures = new Dictionary<string, Texture2D>();
            templates = new Dictionary<TypeStringPair, object>();
            arenas = new Dictionary<string, Arena>();
            overlays = new Texture2D[Enum.GetValues(typeof(TextureName)).Length];
            fonts = new SpriteFont[Enum.GetValues(typeof(FontName)).Length];
            activeArena = null;
            arenaPlaylist = new List<string>();
            arenaPlaylistI = -1;
        }

        #region models

        /// <summary>
        /// Stores a 3D model by name, overwriting any model previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the 3D model.</param>
        /// <param name="model">The 3D model.</param>
        public void AddModel(string name, Model model)
        {
            if (models.ContainsKey(name))
                Log.Write("Overwriting model " + name);
            models[name] = model;
        }

        /// <summary>
        /// Returns a named 3D model.
        /// </summary>
        /// <param name="name">The name of the 3D model.</param>
        /// <returns>The 3D model.</returns>
        public Model GetModel(string name)
        {
            Model model;
            if (!models.TryGetValue(name, out model))
            {
                // Soft error handling; assign some default value and continue with the game.
                Log.Write("Missing 3D model " + name);
                if (!models.TryGetValue("dummymodel", out model))
                    throw new Exception("Missing models " + name + " and fallback dummymodel");
            }
            return model;
        }

        /// <summary>
        /// Tells if a name corresponds to any 3D model.
        /// </summary>
        /// <param name="name">The name of the 3D model.</param>
        /// <returns><c>true</c> if there is a 3D model with the name, <c>false</c> otherwise.</returns>
        public bool HasModel(string name)
        {
            return models.ContainsKey(name);
        }

        #endregion models

        #region textures

        /// <summary>
        /// Stores a 2D texture by name, overwriting any texture previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the 2D texture.</param>
        /// <param name="texture">The 2D texture.</param>
        public void AddTexture(string name, Texture2D texture)
        {
            if (textures.ContainsKey(name))
                Log.Write("Overwriting texture " + name);
            textures[name] = texture;
        }

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
        /// Returns a named 2D texture.
        /// </summary>
        /// <param name="name">The name of the 2D texture.</param>
        /// <returns>The 2D texture.</returns>
        public Texture2D GetTexture(string name)
        {
            Texture2D texture;
            if (!textures.TryGetValue(name, out texture))
            {
                // Soft error handling; assign some default value and continue with the game.
                Log.Write("Missing texture " + name);
                if (!textures.TryGetValue("dummytexture", out texture))
                    throw new Exception("Missing textures " + name + " and fallback dummytexture");
            }
            return texture;
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

        /// <summary>
        /// Tells if a name corresponds to any texture.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns><c>true</c> if there is a texture with the name, <c>false</c> otherwise.</returns>
        public bool HasTexture(string name)
        {
            return textures.ContainsKey(name);
        }

        /// <summary>
        /// Disposes of all textures.
        /// </summary>
        public void ClearTextures()
        {
            foreach (Texture2D texture in textures.Values)
                texture.Dispose();
            textures.Clear();
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

        #endregion fonts

        #region arenas

        /// <summary>
        /// The currently active arena.
        /// </summary>
        /// You can set this field by calling InitializeFromArena(string).
        public Arena Arena { get { return activeArena; } }

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
        public List<string> ArenaPlaylist
        {
            get { return arenaPlaylist; }
            set
            {
                arenaPlaylist = value;
                arenaPlaylistI = -1;
            }
        }

        /// <summary>
        /// Index of current arena in arena playlist,
        /// or -1 if there is no current arena.
        /// </summary>
        public int ArenaPlaylistI { get { return arenaPlaylistI; } set { arenaPlaylistI = value; } }

        /// <summary>
        /// Stores an arena by name, overwriting any arena previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        /// <param name="arena">The arena.</param>
        public void AddArena(string name, Arena arena)
        {
            if (arenas.ContainsKey(name))
                Log.Write("Overwriting arena " + name);
            arenas[name] = arena;
        }

        /// <summary>
        /// Returns a named arena.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        /// <returns>The arena.</returns>
        public Arena GetArena(string name)
        {
            Arena arena;
            if (!arenas.TryGetValue(name, out arena))
            {
                // Soft error handling; assign some default value and continue with the game.
                Log.Write("Missing arena " + name);
                if (!arenas.TryGetValue("dummyarena", out arena))
                    throw new Exception("Missing arenas " + name + " and fallback dummyarena");
            }
            return arena;
        }

        /// <summary>
        /// Tells if a name corresponds to any arena.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        /// <returns><c>true</c> if there is a arena with the name, <c>false</c> otherwise.</returns>
        public bool HasArena(string name)
        {
            return arenas.ContainsKey(name);
        }

        public Arena getNextPlayableArena()
        {
            if ((arenaPlaylistI+1) >= arenaPlaylist.Count)
                return null;
            else
                return GetArena(arenaPlaylist[arenaPlaylistI+1]);
                
        }
        /// <summary>
        /// Initialises the data engine with the next arena in the playlist.
        /// </summary>
        /// <returns><b>false</b> if the initialisation succeeded,
        /// <b>true</b> otherwise.</returns>
        public bool NextArena()
        {
            if (++arenaPlaylistI >= arenaPlaylist.Count)
            {
                activeArena = null;
                return true;
            }
            InitializeFromArena(arenaPlaylist[arenaPlaylistI]);
            return false;
        }

        /// <summary>
        /// Initialises the game data from a previously stored arena.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        public void InitializeFromArena(string name)
        {
            Arena arena = GetArena(name);

            // Clear old data.
            // TODO: First make sure nobody else refers to gobs in 'gobs' 
            // or other objects in other lists.
            gobs.Clear();
            particleEngines.Clear();
            weapons.Clear();
            addedGobs.Clear();
            removedGobs.Clear();



            // Create initial objects.
            foreach (Gob gob in arena.Gobs)
                AddGob(Gob.CreateGob(gob));

            // Reset players.
            ForEachPlayer(delegate(Player player)
            {
                player.Reset();
                player.Lives = 3;
            });

            activeArena = arena;
            RefreshArenaToRadarTransform();

            // Create arena silhouette not until the freshly added gobs
            // have really been added to field 'gobs'.
            CustomOperations += delegate(object obj) { RefreshArenaRadarSilhouette(); };
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

        #region gobs

        /// <summary>
        /// Adds a gob to the game.
        /// </summary>
        /// <param name="gob">The gob to add.</param>
        public void AddGob(Gob gob)
        {
            // HACK: We treat particle engines specially, although they are gobs.
            if (gob is ParticleEngine)
            {
                AddParticleEngine((ParticleEngine)gob);
                return;
            }

            addedGobs.Add(gob);
            gob.Activate();
        }

        /// <summary>
        /// Removes a gob from the game.
        /// </summary>
        /// <param name="gob">The gob to remove.</param>
        public void RemoveGob(Gob gob)
        {
            // HACK: We treat particle engines specially, although they are gobs.
            if (gob is ParticleEngine)
            {
                RemoveParticleEngine((ParticleEngine)gob);
                return;
            }

            removedGobs.Add(gob);
        }

        /// <summary>
        /// Performs the specified action on each gob.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each gob.</param>
        public void ForEachGob(Action<Gob> action)
        {
            foreach (Gob gob in gobs)
                action(gob);
        }

        #endregion gobs

        #region particles

        /// <summary>
        /// Adds a particle generator to the game.
        /// </summary>
        /// <param name="pEng">Particle generator to add to update and draw cycle</param>
        public void AddParticleEngine(ParticleEngine pEng)
        {
            particleEngines.AddLast(pEng);
            pEng.Activate();
        }

        /// <summary>
        /// Removes a particle generator from the game.
        /// </summary>
        /// <param name="pEng">Particle generator to add to update and draw cycle</param>
        public void RemoveParticleEngine(ParticleEngine pEng)
        {
            removedParticleEngines.Add(pEng);
        }

        /// <summary>
        /// Performs the specified action on each particle engine.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each particle engine.</param>
        public void ForEachParticleEngine(Action<ParticleEngine> action)
        {
            foreach (ParticleEngine pEngine in particleEngines)
                action(pEngine);
        }

        #endregion particles

        #region weapons

        /// <summary>
        /// Adds a weapon to the game.
        /// </summary>
        /// <param name="weapon">Weapon to add to the update and draw cycle.</param>
        public void AddWeapon(Weapon weapon)
        {
            weapons.AddLast(weapon);
        }

        /// <summary>
        /// Removes a weapon from the game.
        /// </summary>
        /// <param name="weapon">Weapon to remove from the update and draw cycle.</param>
        public void RemoveWeapon(Weapon weapon)
        {
            weapon.Dispose();
            weapons.Remove(weapon);
        }

        /// <summary>
        /// Performs the specified action on each weapon.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each weapon.</param>
        public void ForEachWeapon(Action<Weapon> action)
        {
            foreach (Weapon weapon in weapons)
                action(weapon);
        }

        #endregion weapons

        #region players

        /// <summary>
        /// Performs the specified action on each player.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each player.</param>
        public void ForEachPlayer(Action<Player> action)
        {
            foreach (Player player in players)
                action(player);
        }

        /// <summary>
        /// Returns the player with the given name, or null if none exists.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <returns>The player.</returns>
        public Player GetPlayer(string playerName)
        {
            foreach (Player player in players)
                if (player.Name.Equals(playerName))
                    return player;
            return null;
        }

        /// <summary>
        /// Adds a player to the game.
        /// </summary>
        /// <param name="player">The player to add.</param>
        public void AddPlayer(Player player)
        {
            players.AddLast(player);
        }

        /// <summary>
        /// Removes a player from the game.
        /// </summary>
        /// <param name="player">The player to remove.</param>
        public void RemovePlayer(Player player)
        {
            // This is O(n)!!! Implement something O(1).
            players.Remove(player);
        }

        #endregion players

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
            TypeStringPair templateKey = new TypeStringPair(baseClass, typeName);
            if (templates.ContainsKey(templateKey))
                Log.Write("Overwriting user-defined type " + baseClass.Name + "/" + typeName);
            templates[templateKey] = template;
            Log.Write("Added user-defined type " + baseClass.Name + "/" + typeName + " of subclass " + template.GetType().Name);
        }

        /// <summary>
        /// Returns the template instance that defines the named user-defined type.
        /// </summary>
        /// <see cref="DataEngineImpl.AddTypeTemplate"/>
        /// <param name="baseClass">The base class of the user-defined type, e.g. Gob or Weapon.</param>
        /// <param name="typeName">The name of the user-defined type, e.g. "explosion" or "shotgun".</param>
        /// <returns>The template instance that defines the named user-defined type.</returns>
        public object GetTypeTemplate(Type baseClass, string typeName)
        {
            object template;
            if (!templates.TryGetValue(new TypeStringPair(baseClass, typeName), out template))
            {
                // Soft error handling; assign some default value and continue with the game.
                Log.Write("Missing template for user-defined type " + baseClass.Name + "/" + typeName);
                string fallbackTypeName = "dummy" + baseClass.Name.ToLower() + "type";
                if (!templates.TryGetValue(new TypeStringPair(baseClass, fallbackTypeName), out template))
                    throw new Exception("Missing templates for user-defined type " + baseClass.Name + "/" + typeName + " and fallback " + fallbackTypeName);
            }
            return template;
        }

        /// <summary>
        /// Tells if a name corresponds to any user-defined type template.
        /// </summary>
        /// <param name="baseClass">The base class of the user-defined type template, e.g. Gob or Weapon.</param>
        /// <param name="typeName">The name of the user-defined type template.</param>
        /// <returns><c>true</c> if there is a user-defined type template with the name, <c>false</c> otherwise.</returns>
        public bool HasTypeTemplate(Type baseClass, string typeName)
        {
            return templates.ContainsKey(new TypeStringPair(baseClass, typeName));
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
            foreach (KeyValuePair<TypeStringPair, object> pair in templates)
                if (pair.Key.Key == typeof(T))
                    action((T)pair.Value);
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
        /// Custom operations to perform once at the end of a frame.
        /// </summary>
        /// This event is called by the data engine after all updates of a frame.
        /// You can add your own delegate to this event to postpone it after 
        /// everything else is calculated in the frame.
        /// The parameter to the Action delegate is undefined.
        public event Action<object> CustomOperations;

        /// <summary>
        /// Sets lighting for the effect.
        /// </summary>
        /// <param name="effect">The effect to modify.</param>
        public void PrepareEffect(BasicEffect effect)
        {
            activeArena.PrepareEffect(effect);
        }

        /// <summary>
        /// Commits pending operations. This method should be called at the end of each frame.
        /// </summary>
        public void CommitPending()
        {
            PhysicsEngine physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));

            // Add gobs to add.
            foreach (Gob gob in addedGobs)
            {
                physics.Register(gob);
                gobs.AddLast(gob);
            }
            addedGobs.Clear();

            // Apply custom operations.
            if (CustomOperations != null)
                CustomOperations(null);
            CustomOperations = null;

            // Remove gobs to remove.
            // Don't use foreach because removed gobs may still add more items
            // to 'removedGobs'.
            for (int i = 0; i < removedGobs.Count; ++i)
            {
                Gob gob = removedGobs[i];
                physics.Unregister(gob);
                gob.Dispose();
                gobs.Remove(gob);
            }
            removedGobs.Clear();

            // Remove particle engines to remove.
            // Don't use foreach because removed particle engines may still add more items
            // to 'removedParticleEngines'.
            for (int i = 0; i < removedParticleEngines.Count; ++i)
            {
                ParticleEngine pEng = removedParticleEngines[i];
                particleEngines.Remove(pEng);
            }
            removedParticleEngines.Clear();

#if DEBUG_PROFILE
            AssaultWing.Instance.gobCount = gobs.Count;
#endif
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
            if (activeArena == null)
                throw new InvalidOperationException("No active arena");
            Vector2 radarDisplayDimensions = new Vector2(162, 150); // TODO: Make this constant configurable
            Vector2 arenaDimensions = activeArena.Dimensions;
            float arenaToRadarScale = Math.Min(
                radarDisplayDimensions.X / arenaDimensions.X,
                radarDisplayDimensions.Y / arenaDimensions.Y);
            arenaDimensionsOnRadar = arenaDimensions * arenaToRadarScale;
            arenaToRadarTransform =
                Matrix.CreateScale(arenaToRadarScale, -arenaToRadarScale, 1) *
                Matrix.CreateTranslation(0, arenaDimensionsOnRadar.Y, 0);
        }

        /// <summary>
        /// Refreshes <c>ArenaRadarSilhouette</c> according to the contents 
        /// of the currently active arena.
        /// </summary>
        /// To be called whenever arena (or arena walls) change,
        /// after <c>RefreshArenaToRadarTransform</c>.
        /// <seealso cref="ArenaRadarSilhouette"/>
        /// <seealso cref="RefreshArenaToRadarTransform"/>
        void RefreshArenaRadarSilhouette()
        {
            if (activeArena == null)
                throw new InvalidOperationException("No active arena");

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
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, activeArena.Dimensions.X,
                0, activeArena.Dimensions.Y, 10, 1000);

            // Set and clear our own render target.
            gfx.SetRenderTarget(0, maskTarget);
            gfx.Clear(ClearOptions.Target, Color.TransparentBlack, 0, 0);

            // Draw the arena's walls.
            SpriteBatch spriteBatch = new SpriteBatch(gfx);
            spriteBatch.Begin();
            foreach (Gob gob in gobs)
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

        #endregion Private methods
    }
}
