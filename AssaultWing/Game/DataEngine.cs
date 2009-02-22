using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Particles;
using AW2.Graphics;
using Viewport = AW2.Graphics.AWViewport;

namespace AW2.Game
{
    /// <summary>
    /// Interface for game data.
    /// </summary>
    /// Gobs in an arena are kept on several arena layers. One of the layers
    /// is where the actual gameplay takes place. The rest are just for the looks.
    /// The gameplay layer is the default for all gob-related actions.
    /// To deal with some other layer, you need to know its layer index.
    interface DataEngine
    {
        #region models

        /// <summary>
        /// Stores a 3D model by name, overwriting any model previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the 3D model.</param>
        /// <param name="model">The 3D model.</param>
        void AddModel(string name, Model model);

        /// <summary>
        /// Returns a named 3D model.
        /// </summary>
        /// <param name="name">The name of the 3D model.</param>
        /// <returns>The 3D model.</returns>
        Model GetModel(string name);

        /// <summary>
        /// Tells if a name corresponds to any 3D model.
        /// </summary>
        /// <param name="name">The name of the 3D model.</param>
        /// <returns><c>true</c> if there is a 3D model with the name, <c>false</c> otherwise.</returns>
        bool HasModel(string name);

        /// <summary>
        /// Disposes of all 3D models.
        /// </summary>
        void ClearModels();

        #endregion

        #region textures

        /// <summary>
        /// Stores a 2D texture by name, overwriting any texture previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the 2D texture.</param>
        /// <param name="texture">The 2D texture.</param>
        void AddTexture(string name, Texture2D texture);

        /// <summary>
        /// Stores a static 2D texture by name, overwriting any texture previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the 2D texture.</param>
        /// <param name="texture">The 2D texture.</param>
        void AddTexture(TextureName name, Texture2D texture);

        /// <summary>
        /// Returns a named 2D texture.
        /// </summary>
        /// <param name="name">The name of the 2D texture.</param>
        /// <returns>The 2D texture.</returns>
        Texture2D GetTexture(string name);

        /// <summary>
        /// Returns a texture used in static graphics.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        Texture2D GetTexture(TextureName name);

        /// <summary>
        /// Tells if a name corresponds to any texture.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns><c>true</c> if there is a texture with the name, <c>false</c> otherwise.</returns>
        bool HasTexture(string name);

        /// <summary>
        /// Disposes of all textures.
        /// </summary>
        void ClearTextures();

        #endregion

        #region fonts

        /// <summary>
        /// Stores a static font by name, overwriting any font previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <param name="font">The font.</param>
        void AddFont(FontName name, SpriteFont font);

        /// <summary>
        /// Returns a font used in static graphics.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <returns>The font.</returns>
        SpriteFont GetFont(FontName name);

        /// <summary>
        /// Disposes of all fonts.
        /// </summary>
        void ClearFonts();

        #endregion fonts

        #region arenas

        /// <summary>
        /// The currently active arena.
        /// </summary>
        /// You can set this field by calling InitializeFromArena(string).
        Arena Arena { get; }

        /// <summary>
        /// The currently active arena's silhouette, scaled and ready to be 
        /// drawn in a player's viewport's radar display.
        /// </summary>
        Texture2D ArenaRadarSilhouette { get; }

        /// <summary>
        /// The transformation to map coordinates in the current arena 
        /// into player viewport radar display coordinates.
        /// </summary>
        /// Arena origin is the lower left corner, positive X is to the right,
        /// and positive Y is up. Radar display origin is the top left corner
        /// of the radar display area, positive X is to the right, and positive
        /// Y is down.
        Matrix ArenaToRadarTransform { get; }

        /// <summary>
        /// Stores 2D texture by arena name 
        /// </summary>
        /// <param name="arenaName">The name of the arena.</param>
        /// <param name="texture">The 2D texture.</param>
        void AddArenaPreview(string arenaName, Texture2D texture);
        
        /// <summary>
        /// Returns preview picture from arena (2D texture). If no preview is available, then return's no preview picture   
        /// </summary>
        /// <param name="arena">The name of the arena.</param>
        /// <returns>The 2D texture.</returns>
        Texture2D GetArenaPreview(string arena);

        /// <summary>
        /// Arenas to play in one session.
        /// </summary>
        List<string> ArenaPlaylist { get; set; }

        /// <summary>
        /// ArenaFileNames needed for arena loading
        /// </summary>
        Dictionary<string,string> ArenaFileNameList { get; set; }

        /// <summary>
        /// Index of current arena in arena playlist,
        /// or -1 if there is no current arena.
        /// </summary>
        int ArenaPlaylistI { get; set; }
        /*
        /// <summary>
        /// Stores an arena by name, overwriting any arena previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        /// <param name="arena">The arena.</param>
        void AddArena(string name, Arena arena);
        */
        /// <summary>
        /// Returns a named arena.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        /// <returns>The arena.</returns>
        Arena GetArena(string name);
        /*
        /// <summary>
        /// Tells if a name corresponds to any arena.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        /// <returns><c>true</c> if there is a arena with the name, <c>false</c> otherwise.</returns>
        bool HasArena(string name);
        */
        /// <summary>
        /// Prepares the next arena in the playlist ready for playing.
        /// </summary>
        /// When the playing really should start, call <c>StartArena</c>.
        /// <returns><b>false</b> if the initialisation succeeded,
        /// <b>true</b> otherwise.</returns>
        bool NextArena();

        /// <summary>
        /// Sets a previously prepared arena as the active one.
        /// </summary>
        /// Call this method right before commencing play in the prepared arena.
        void StartArena();

        /// <summary>
        /// Prepares the game data for playing an arena.
        /// </summary>
        /// When the playing really should start, call <c>StartArena</c>.
        /// <param name="name">The name of the arena.</param>
        void InitializeFromArena(string name);
        /*
        /// <summary>
        /// Performs the specified action on each arena.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each arena.</param>
        void ForEachArena(Action<Arena> action);
        */
        #endregion

        #region arena layers

        /// <summary>
        /// The index of the layer of the currently active arena where the gameplay takes place.
        /// </summary>
        int GameplayLayer { get; }

        /// <summary>
        /// Performs an action on each arena layer of the active arena.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each arena layer.</param>
        void ForEachArenaLayer(Action<ArenaLayer> action);

        #endregion arena layers

        #region gobs

        /// <summary>
        /// Adds a gob to the game.
        /// </summary>
        /// <param name="gob">The gob to add.</param>
        void AddGob(Gob gob);

        /// <summary>
        /// Adds a gob to an arena layer of the game.
        /// </summary>
        /// <param name="gob">The gob to add.</param>
        /// <param name="layer">The arena layer index.</param>
        void AddGob(Gob gob, int layer);

        /// <summary>
        /// Removes a gob from the game.
        /// </summary>
        /// <param name="gob">The gob to remove.</param>
        void RemoveGob(Gob gob);

        /// <summary>
        /// Performs the specified action on each gob on each arena layer.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each gob.</param>
        void ForEachGob(Action<Gob> action);

        /// <summary>
        /// Returns a gob by its identifier, or <c>null</c> if no such gob exists.
        /// </summary>
        /// <param name="gobId">The identifier of the gob.</param>
        /// <returns>The gob, or <c>null</c> if the gob couldn't be found.</returns>
        /// <seealso cref="AW2.Game.Gob.Id"/>
        Gob GetGob(int gobId);

        #endregion

        #region weapons

        /// <summary>
        /// Adds a weapon to the game.
        /// </summary>
        /// <param name="weapon">Weapon to add to the update and draw cycle.</param>
        void AddWeapon(Weapon weapon);

        /// <summary>
        /// Removes a weapon from the game.
        /// </summary>
        /// <param name="weapon">Weapon to remove from the update and draw cycle</param>
        void RemoveWeapon(Weapon weapon);

        /// <summary>
        /// Performs the specified action on each weapon.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each weapon.</param>
        void ForEachWeapon(Action<Weapon> action);

        #endregion

        #region players

        /// <summary>
        /// Adds a player to the game.
        /// </summary>
        /// <param name="player">The player to add.</param>
        void AddPlayer(Player player);

        /// <summary>
        /// Removes a player from the game.
        /// </summary>
        /// <param name="player">The player to remove.</param>
        void RemovePlayer(Player player);

        /// <summary>
        /// Removes certain players from the game.
        /// </summary>
        /// <param name="criterion">The criterion which players to remove.</param>
        void RemovePlayers(Predicate<Player> criterion);

        /// <summary>
        /// Performs the specified action on each player.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each player.</param>
        void ForEachPlayer(Action<Player> action);

        /// <summary>
        /// Returns the player with the given name, or null if none exists.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <returns>The player.</returns>
        Player GetPlayer(string playerName);

        /// <summary>
        /// Returns a player by his identifier, or <c>null</c> if no such player exists.
        /// </summary>
        /// <param name="playerId">The identifier of the player.</param>
        /// <returns>The player, or <c>null</c> if the player couldn't be found.</returns>
        /// <seealso cref="AW2.Game.Player.Id"/>
        Player GetPlayer(int playerId);

        #endregion

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
        void AddTypeTemplate(Type baseClass, string typeName, object template);

        /// <summary>
        /// Returns the template instance that defines the named user-defined type.
        /// </summary>
        /// <see cref="DataEngine.AddTypeTemplate"/>
        /// <param name="baseClass">The base class of the user-defined type, e.g. Gob or Weapon.</param>
        /// <param name="typeName">The name of the user-defined type, e.g. "explosion" or "shotgun".</param>
        /// <returns>The template instance that defines the named user-defined type.</returns>
        object GetTypeTemplate(Type baseClass, string typeName);

        /// <summary>
        /// Tells if a name corresponds to any user-defined type template.
        /// </summary>
        /// <param name="baseClass">The base class of the user-defined type template, e.g. Gob or Weapon.</param>
        /// <param name="typeName">The name of the user-defined type template.</param>
        /// <returns><c>true</c> if there is a user-defined type template with the name, <c>false</c> otherwise.</returns>
        bool HasTypeTemplate(Type baseClass, string typeName);

        /// <summary>
        /// Performs the specified action on each user-defined type template
        /// that was stored under the given base class.
        /// </summary>
        /// <see cref="DataEngine.AddTypeTemplate"/>
        /// <typeparam name="T">Base class of templates to loop through.</typeparam>
        /// <param name="action">The Action delegate to perform on each template.</param>
        void ForEachTypeTemplate<T>(Action<T> action);

        #endregion

        #region viewports

        /// <summary>
        /// The viewport we are currently drawing into.
        /// </summary>
        Viewport Viewport { get; }

        /// <summary>
        /// Adds a viewport.
        /// </summary>
        /// <param name="viewport">Viewport to add.</param>
        void AddViewport(Viewport viewport);

        /// <summary>
        /// Adds a viewport separator to be displayed.
        /// </summary>
        /// <param name="separator">The viewport separator.</param>
        void AddViewportSeparator(ViewportSeparator separator);

        /// <summary>
        /// Removes all viewports and viewport separators.
        /// </summary>
        void ClearViewports();

        /// <summary>
        /// Performs the specified action on each viewport.
        /// </summary>
        /// The active viewport is set before each time the action is performed.
        /// <see cref="DataEngine.Viewport"/>
        /// <param name="action">The Action delegate to perform on each viewport.</param>
        void ForEachViewport(Action<Viewport> action);

        /// <summary>
        /// Performs the specified action on each viewport separator.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each viewport separator.</param>
        void ForEachViewportSeparator(Action<ViewportSeparator> action);

        #endregion

        #region miscellaneous

        /// <summary>
        /// The progress bar.
        /// </summary>
        /// Anybody can tell the progress bar that their tasks are completed.
        /// This way the progress bar knows how its running task is progressing.
        ProgressBar ProgressBar { get; }

        /// <summary>
        /// Custom operations to perform once at the end of a frame.
        /// </summary>
        /// This event is called by the data engine after all updates of a frame.
        /// You can add your own delegate to this event to postpone it after 
        /// everything else is calculated in the frame.
        /// The parameter to the Action delegate is undefined.
        event Action<object> CustomOperations;

        /// <summary>
        /// Sets lighting for the effect.
        /// </summary>
        /// <param name="effect">The effect to modify.</param>
        void PrepareEffect(BasicEffect effect);

        /// <summary>
        /// Commits pending operations. This method should be called at the end of each frame.
        /// </summary>
        void CommitPending();

        /// <summary>
        /// Clears all data about the state of the game session.
        /// </summary>
        /// Call this method after the game session has ended.
        void ClearGameState();

        /// <summary>
        /// Loads content needed by the currently active arena.
        /// </summary>
        void LoadContent();

        /// <summary>
        /// Unloads content needed by the currently active arena.
        /// </summary>
        void UnloadContent();

        #endregion
    }
}
