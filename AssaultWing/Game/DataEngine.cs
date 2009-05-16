using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Gobs;
using AW2.Graphics;
using AW2.Helpers;
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
        List<ArenaLayer> arenaLayers;
        LinkedList<Weapon> weapons;
        List<Gob> addedGobs;
        List<Gob> removedGobs;
        List<Viewport> viewports;
        List<ViewportSeparator> viewportSeparators;
        Dictionary<string, Model> models;
        Dictionary<string, Texture2D> textures;
        Dictionary<string, Texture2D> arenaPreviews;
        Dictionary<string, string> arenaFileNameList;

        /// <summary>
        /// Type templates, as a list "indexed" by base class (such as 'typeof(Gob)'), 
        /// yielding a list "indexed" by template type name (such as "rocket"),
        /// yielding the template.
        /// </summary>
        List<Pair<Type, List<Pair<string, object>>>> templates;

        Dictionary<string, Arena> arenas;
        Arena activeArena, preparedArena;
        Texture2D arenaRadarSilhouette;
        Vector2 arenaDimensionsOnRadar;
        Matrix arenaToRadarTransform;
        ProgressBar progressBar;

        /// <summary>
        /// Index of the gameplay arena layer. Gameplay backlayer is this minus one.
        /// </summary>
        int gameplayLayer;

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
        /// Players who participate in the game session.
        /// </summary>
        public PlayerCollection Players { get; private set; }

        /// <summary>
        /// Creates a new data engine.
        /// </summary>
        public DataEngine()
        {
            arenaLayers = new List<ArenaLayer>();
            addedGobs = new List<Gob>();
            removedGobs = new List<Gob>();
            weapons = new LinkedList<Weapon>();
            Players = new PlayerCollection();
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
            viewports = new List<Viewport>();
            viewportSeparators = new List<ViewportSeparator>();
            models = new Dictionary<string, Model>();
            textures = new Dictionary<string, Texture2D>();
            templates = new List<Pair<Type, List<Pair<string, object>>>>();
            arenas = new Dictionary<string, Arena>();
            arenaPreviews = new Dictionary<string, Texture2D>();
            overlays = new Texture2D[Enum.GetValues(typeof(TextureName)).Length];
            fonts = new SpriteFont[Enum.GetValues(typeof(FontName)).Length];
            activeArena = preparedArena = null;
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

        /// <summary>
        /// Disposes of all 3D models.
        /// </summary>
        public void ClearModels()
        {
            models.Clear();
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

        #region arena previews

        /// <summary>
        /// Stores a preview texture for a named arena.
        /// </summary>
        /// <param name="arenaName">The name of the arena.</param>
        /// <param name="texture">The arena preview texture.</param>
        public void AddArenaPreview(string arenaName, Texture2D texture)
        {
            arenaPreviews.Add(arenaName, texture);
        }

        /// <summary>
        /// Returns preview picture from arena (2D texture). 
        /// If no preview is available, then returns "no preview" picture.
        /// </summary>
        /// <param name="arena">The name of the arena.</param>
        /// <returns>The arena preview.</returns>
        public Texture2D GetArenaPreview(string arena)
        {
            if (arenaPreviews.ContainsKey(arena))
                return arenaPreviews[arena];
            else
                return arenaPreviews["noPreview"];
        }

        /// <summary>
        /// Disposes of all arena previews.
        /// </summary>
        public void ClearArenaPreviews()
        {
            foreach (Texture2D preview in arenaPreviews.Values)
                preview.Dispose();
            arenaPreviews.Clear();
        }

        #endregion arena previews

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
        /// Mapping of all available arenas to the names of the files 
        /// where the arenas are defined.
        /// </summary>
        public Dictionary<string, string> ArenaFileNameList
        {
            get { return arenaFileNameList; }
            set { arenaFileNameList = value; }
        }

        /// <summary>
        /// Index of current arena in arena playlist,
        /// or -1 if there is no current arena.
        /// </summary>
        public int ArenaPlaylistI { get { return arenaPlaylistI; } set { arenaPlaylistI = value; } }
 
        /// <summary>
        /// Returns a named arena.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        /// <returns>The arena.</returns>
        public Arena GetArena(string name)
        {
            if (preparedArena == null || !preparedArena.Name.Equals(name))
            {

                TypeLoader arenaLoader = new TypeLoader(typeof(Arena), Paths.Arenas);
                preparedArena = (Arena)arenaLoader.LoadSpecifiedTypes(arenaFileNameList[name]);
            }
            /*TODO: Write error handling*/
            /*
            if (!arenas.TryGetValue(name, out arena))
            {
                // Soft error handling; assign some default value and continue with the game.
                Log.Write("Missing arena " + name);
                if (!arenas.TryGetValue("dummyarena", out arena))
                    throw new Exception("Missing arenas " + name + " and fallback dummyarena");
            }*/
            return preparedArena;
        }

        /// <summary>
        /// Returns the next playable arena.
        /// </summary>
        public Arena GetNextPlayableArena()
        {
            if ((arenaPlaylistI + 1) >= arenaPlaylist.Count)
                return null;
            else
                return GetArena(arenaPlaylist[arenaPlaylistI + 1]);
        }

        /// <summary>
        /// Prepares the next arena in the playlist ready for playing.
        /// </summary>
        /// When the playing really should start, call <c>StartArena</c>.
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

            // Clear remaining data from a possible previous arena.
            foreach (ArenaLayer layer in arenaLayers)
            {
                layer.ForEachGob(delegate(Gob gob) { gob.UnloadContent(); });
                layer.ClearGobs();
            }
            weapons.Clear();

            // Create layers for the arena.
            arenaLayers.Clear();
            for (int i = 0; i < preparedArena.Layers.Count; ++i)
            {
                ArenaLayer layer = preparedArena.Layers[i];
                arenaLayers.Add(layer.EmptyCopy());
                if (layer.IsGameplayLayer)
                    gameplayLayer = i;
            }
            if (gameplayLayer == -1)
                throw new ArgumentException("Arena doesn't have a gameplay layer");

            activeArena = preparedArena;
            preparedArena = null;
            RefreshArenaToRadarTransform();

            // Create arena silhouette not until the freshly added gobs
            // have really been added to field 'gobs'.
            CustomOperations += RefreshArenaRadarSilhouette;
        }

        /// <summary>
        /// Prepares the game data for playing an arena.
        /// </summary>
        /// When the playing really should start, call <c>StartArena</c>.
        /// <param name="name">The name of the arena.</param>
        public void InitializeFromArena(string name)
        {
            preparedArena = GetArena(name);

            // Clear old data.
            // We clear visible objects only after most of the initialisation is done.
            // This way the user can see the old arena until the new arena is ready.
            addedGobs.Clear();
            removedGobs.Clear();
            CustomOperations = null;

            // Find the gameplay layer.
            gameplayLayer = -1;
            for (int i = 0; i < preparedArena.Layers.Count; ++i)
                if (preparedArena.Layers[i].IsGameplayLayer)
                    gameplayLayer = i;
            if (gameplayLayer == -1)
                throw new ArgumentException("Arena " + preparedArena.Name + " doesn't have a gameplay layer");

            // Make sure the gameplay backlayer is located right before the gameplay layer.
            // We use a suitable layer if one is defined in the arena. 
            // Otherwise we create a new layer.
            if (gameplayLayer == 0 || preparedArena.Layers[gameplayLayer - 1].Z != 0)
            {
                preparedArena.Layers.Insert(gameplayLayer, new ArenaLayer(false, 0, null));
                ++gameplayLayer;
            }

            // Create initial objects. This is by far the most time consuming part
            // in initialising an arena for playing.
            // Note that the gobs will end up in 'arenaLayers' only after the game starts running again.
            int wallCount = 0;
            foreach (ArenaLayer layer in preparedArena.Layers)
                layer.ForEachGob(delegate(Gob gob)
                {
                    if (gob is Wall) ++wallCount;
                });
            progressBar.SetSubtaskCount(wallCount);
            for (int i = 0; i < preparedArena.Layers.Count; ++i)
                preparedArena.Layers[i].ForEachGob(gob => Gob.CreateGob(gob, newGob => AddGob(newGob, i)));
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

        #region arena layers

        /// <summary>
        /// The index of the layer of the currently active arena where the gameplay takes place.
        /// </summary>
        public int GameplayLayer { get { return gameplayLayer; } }

        /// <summary>
        /// Performs an action on each arena layer of the active arena.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each arena layer.</param>
        public void ForEachArenaLayer(Action<ArenaLayer> action)
        {
            foreach (ArenaLayer layer in arenaLayers)
                action(layer);
        }

        #endregion arena layers

        #region gobs

        /// <summary>
        /// Adds a gob to the game.
        /// </summary>
        /// <param name="gob">The gob to add.</param>
        public void AddGob(Gob gob)
        {
            if (gob.LayerPreference == Gob.LayerPreferenceType.Front)
                AddGob(gob, gameplayLayer);
            else
                AddGob(gob, gameplayLayer - 1);
        }

        /// <summary>
        /// Adds a gob to an arena layer of the game.
        /// </summary>
        /// <param name="gob">The gob to add.</param>
        /// <param name="layer">The arena layer index.</param>
        void AddGob(Gob gob, int layer)
        {
            gob.Layer = layer;
            addedGobs.Add(gob);
            gob.Activate();

            // If we are the game server, notify game clients of the new gob.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server && gob.IsRelevant)
            {
                NetworkEngine net = (NetworkEngine)AssaultWing.Instance.Services.GetService(typeof(NetworkEngine));
                var message = new GobCreationMessage();
                message.GobTypeName = gob.TypeName;
                message.Write(gob, SerializationModeFlags.All);
                net.SendToClients(message);
            }
        }

        /// <summary>
        /// Removes a gob from the game.
        /// </summary>
        /// <param name="gob">The gob to remove.</param>
        public void RemoveGob(Gob gob)
        {
            // If we are a game client, we remove relevant gobs only when the server tells us to.
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Client || !gob.IsRelevant)
                removedGobs.Add(gob);

            // If we are the game server, notify game clients of the removal of relevant gobs.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server && gob.IsRelevant)
            {
                NetworkEngine net = (NetworkEngine)AssaultWing.Instance.Services.GetService(typeof(NetworkEngine));
                var message = new GobDeletionMessage();
                message.GobId = gob.Id;
                net.SendToClients(message);
            }
        }

        /// <summary>
        /// Performs the specified action on each gob on each arena layer.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each gob.</param>
        public void ForEachGob(Action<Gob> action)
        {
            foreach (ArenaLayer layer in arenaLayers)
                layer.ForEachGob(delegate(Gob gob) { action(gob); });
        }

        /// <summary>
        /// Returns a gob by its identifier, or <c>null</c> if no such gob exists.
        /// </summary>
        /// <param name="gobId">The identifier of the gob.</param>
        /// <returns>The gob, or <c>null</c> if the gob couldn't be found.</returns>
        /// <seealso cref="AW2.Game.Gob.Id"/>
        public Gob GetGob(int gobId)
        {
            Gob returnGob = null;
            arenaLayers[gameplayLayer].ForEachGob(gob => returnGob = gob.Id == gobId ? gob : returnGob);
            return returnGob;
        }

        #endregion gobs

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
            activeArena.PrepareEffect(effect);
        }

        /// <summary>
        /// Commits pending operations. This method should be called at the end of each frame.
        /// </summary>
        public void CommitPending()
        {
            PhysicsEngine physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));
            NetworkEngine net = (NetworkEngine)AssaultWing.Instance.Services.GetService(typeof(NetworkEngine));

            // If we are a game client, create gobs as told by the game server.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                GobCreationMessage message;
                while ((message = net.ReceiveFromServer<GobCreationMessage>()) != null)
                {
                    Gob gob = Gob.CreateGob(message.GobTypeName);
                    message.Read(gob, SerializationModeFlags.All);
                    AddGob(gob, gob.Layer);

                    // Ships we set automatically as the ship the ship's owner is controlling.
                    Ship gobShip = gob as Ship;
                    if (gobShip != null)
                        gobShip.Owner.Ship = gobShip;
                }
            }

            // Add gobs to add.
            foreach (Gob gob in addedGobs)
            {
                if (gob.Layer == gameplayLayer)
                    physics.Register(gob);
                else
                {
                    // Gobs outside the gameplay layer cannot collide.
                    // To achieve this, we take away all the gob's collision areas.
                    gob.ClearCollisionAreas();
                }
                arenaLayers[gob.Layer].AddGob(gob);
            }
            addedGobs.Clear();

            // If we are a game client, update gobs and players as told by the game server.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                net.ReceiveFromServerWhile<GobUpdateMessage>(message =>
                {
                    message.ReadGobs(gobId => GetGob(gobId), SerializationModeFlags.VaryingData);
                    return true;
                });
                net.ReceiveFromServerWhile<GobDamageMessage>(message =>
                {
                    Gob gob = GetGob(message.GobId);
                    if (gob == null) return true; // Skip updates for gobs we haven't yet created.
                    gob.DamageLevel = message.DamageLevel;
                    return true;
                });
                net.ReceiveFromServerWhile<PlayerUpdateMessage>(message =>
                {
                    Player player = Players.FirstOrDefault(plr => plr.Id == message.PlayerId);
                    if (player == null) throw new ArgumentException("Update for unknown player ID " + message.PlayerId);
                    message.Read(player, SerializationModeFlags.VaryingData);
                    return true;
                });
            }

            // Apply custom operations.
            if (CustomOperations != null)
                CustomOperations();
            CustomOperations = null;

            // If we are a game client, remove gobs as told by the game server.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client)
            {
                net.ReceiveFromServerWhile<GobDeletionMessage>(message =>
                {
                    Gob gob = GetGob(message.GobId);
                    if (gob == null)
                    {
                        // The gob hasn't been created yet. This happens when the server
                        // has created a gob and deleted it on the same frame, and
                        // the creation and deletion messages arrived just after we 
                        // finished receiving creation messages but right before we 
                        // started receiving deletion messages for this frame.
                        return false;
                    }
                    gob.DieOnClient();
                    removedGobs.Add(gob);
                    return true;
                });
            }

            // Remove gobs to remove.
            // Don't use foreach because removed gobs may still add more items
            // to 'removedGobs'.
            for (int i = 0; i < removedGobs.Count; ++i)
            {
                Gob gob = removedGobs[i];
                if (gob.Layer == gameplayLayer)
                    physics.Unregister(gob);
                gob.Dispose();
                arenaLayers[gob.Layer].RemoveGob(gob);
            }
            removedGobs.Clear();

            // Send state updates about gobs to game clients if we are the game server.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                TimeSpan now = AssaultWing.Instance.GameTime.TotalGameTime;
                var message = new GobUpdateMessage();
                arenaLayers[gameplayLayer].ForEachGob(gob =>
                {
                    if (!gob.IsRelevant) return;
                    if (!gob.Movable) return;
                    if (gob.LastNetworkUpdate + gob.NetworkUpdatePeriod > now) return;
                    gob.LastNetworkUpdate = now;
                    message.AddGob(gob.Id, gob, SerializationModeFlags.VaryingData);
                });
                net.SendToClients(message);
            }

            // Send state updates about players to game clients if we are the game server.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                foreach (var player in Players)
                {
                    if (!player.MustUpdateToClients) return;
                    player.MustUpdateToClients = false;
                    var message = new PlayerUpdateMessage();
                    message.PlayerId = player.Id;
                    message.Write(player, SerializationModeFlags.VaryingData);
                    net.SendToClients(message);
                }
            }

#if DEBUG_PROFILE
            AssaultWing.Instance.gobCount = 0;
            foreach (ArenaLayer layer in arenaLayers)
                layer.ForEachGob(delegate(Gob gob) { ++AssaultWing.Instance.gobCount; });
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
            activeArena = null;
            ForEachArenaLayer(layer => layer.ClearGobs());
            ClearViewports();
        }

        /// <summary>
        /// Loads content needed by the currently active arena.
        /// </summary>
        public void LoadContent()
        {
            if (activeArena != null)
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
            if (activeArena == null)
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
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, activeArena.Dimensions.X,
                0, activeArena.Dimensions.Y, 10, 1000);

            // Set and clear our own render target.
            gfx.SetRenderTarget(0, maskTarget);
            gfx.Clear(ClearOptions.Target, Color.TransparentBlack, 0, 0);

            // Draw the arena's walls.
            SpriteBatch spriteBatch = new SpriteBatch(gfx);
            spriteBatch.Begin();
            arenaLayers[gameplayLayer].ForEachGob(delegate(Gob gob)
            {
                Wall wall = gob as Wall;
                if (wall != null)
                    wall.DrawSilhouette(view, projection, spriteBatch);
            });
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

        #endregion Private methods
    }
}
