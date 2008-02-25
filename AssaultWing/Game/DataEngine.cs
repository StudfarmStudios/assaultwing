using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics;
using Viewport = AW2.Graphics.AWViewport;
using AW2.Game.Particles;

namespace AW2.Game
{
    /// <summary>
    /// Interface for game data.
    /// </summary>
    interface DataEngine
    {
        /// <summary>
        /// The currently active arena.
        /// </summary>
        /// You can set this field by calling InitializeFromArena(string).
        Arena Arena { get; }

        /// <summary>
        /// The viewport we are currently drawing into.
        /// </summary>
        Viewport Viewport { get; }

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

        #endregion

        #region textures

        /// <summary>
        /// Stores a 2D texture by name, overwriting any texture previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the 2D texture.</param>
        /// <param name="texture">The 2D texture.</param>
        void AddTexture(string name, Texture2D texture);

        /// <summary>
        /// Returns a named 2D texture.
        /// </summary>
        /// <param name="name">The name of the 2D texture.</param>
        /// <returns>The 2D texture.</returns>
        Texture2D GetTexture(string name);

        #endregion

        #region arenas

        /// <summary>
        /// Stores an arena by name, overwriting any arena previously stored by the same name.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        /// <param name="arena">The arena.</param>
        void AddArena(string name, Arena arena);

        /// <summary>
        /// Returns a named arena.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        /// <returns>The arena.</returns>
        Arena GetArena(string name);

        /// <summary>
        /// Initialises the game data from a previously stored arena.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        void InitializeFromArena(string name);

        /// <summary>
        /// Performs the specified action on each arena.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each arena.</param>
        void ForEachArena(Action<Arena> action);

        #endregion

        #region gobs

        /// <summary>
        /// Adds a gob to the game.
        /// </summary>
        /// <param name="gob">The gob to add.</param>
        void AddGob(Gob gob);

        /// <summary>
        /// Removes a gob from the game.
        /// </summary>
        /// <param name="gob">The gob to remove.</param>
        void RemoveGob(Gob gob);

        /// <summary>
        /// Performs the specified action on each gob.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each gob.</param>
        void ForEachGob(Action<Gob> action);

        /// <summary>
        /// Performs the specified action on each gob of the specified category.
        /// </summary>
        /// The intended values for the type parameter are subinterfaces of IGob.
        /// <param name="action">The Action delegate to perform on each gob of the specified category.</param>
        /// <typeparam name="T">The kind of gobs to loop through.</typeparam>
        void ForEachGob<T>(Action<Gob> action) where T : IGob;

        #endregion

        #region particles

        /// <summary>
        /// Adds a particle generator to the game.
        /// </summary>
        /// <param name="pEng">Particle generator to add to the update and draw cycle</param>
        void AddParticleEngine(ParticleEngine pEng);

        /// <summary>
        /// Removes a particle generator from the game.
        /// </summary>
        /// <param name="pEng">Particle generator to remove from the update and draw cycle</param>
        void RemoveParticleEngine(ParticleEngine pEng);

        /// <summary>
        /// Performs the specified action on each particle engine.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each particle engine.</param>
        void ForEachParticleEngine(Action<ParticleEngine> action);

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
        /// Adds a viewport.
        /// </summary>
        /// <param name="viewport">Viewport to add.</param>
        void AddViewport(Viewport viewport);

        /// <summary>
        /// Removes all viewports.
        /// </summary>
        void ClearViewports();

        /// <summary>
        /// Performs the specified action on each viewport.
        /// </summary>
        /// The active viewport is set before each time the action is performed.
        /// <see cref="DataEngine.Viewport"/>
        /// <param name="action">The Action delegate to perform on each viewport.</param>
        void ForEachViewport(Action<Viewport> action);

        #endregion

        #region miscellaneous

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

        #endregion
    }
}
