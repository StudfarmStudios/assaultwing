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
        LinkedList<Player> players;
        List<Viewport> viewports;
        Dictionary<string, Model> models;
        Dictionary<string, Texture2D> textures;
        Dictionary<TypeStringPair, object> templates;
        Dictionary<string, Arena> arenas;
        Arena activeArena;

        /// <summary>
        /// The viewport we are currently drawing into.
        /// </summary>
        Viewport activeViewport;

        /// <summary>
        /// The currently active arena.
        /// </summary>
        /// You can set this field by calling InitializeFromArena(string).
        public Arena Arena { get { return activeArena; } }
        
        /// <summary>
        /// The viewport we are currently drawing into.
        /// </summary>
        public Viewport Viewport { get { return activeViewport; } }

        /// <summary>
        /// Creates a new data engine.
        /// </summary>
        public DataEngineImpl()
        {
            gobs = new LinkedList<Gob>();
            addedGobs = new List<Gob>();
            removedGobs = new List<Gob>();
            particleEngines = new LinkedList<ParticleEngine>();
            weapons = new LinkedList<Weapon>();
            players = new LinkedList<Player>();
            viewports = new List<Viewport>();
            models = new Dictionary<string, Model>();
            textures = new Dictionary<string, Texture2D>();
            templates = new Dictionary<TypeStringPair, object>();
            arenas = new Dictionary<string, Arena>();
            activeArena = null;
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

        #endregion textures

        #region arenas

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
        /// Initialises the game data from a previously stored arena.
        /// </summary>
        /// <param name="name">The name of the arena.</param>
        public void InitializeFromArena(string name)
        {
            Arena arena = GetArena(name);
            // TODO: First make sure nobody else refers to gobs in 'gobs'.
            gobs.Clear();
            foreach (Gob gob in arena.Gobs)
                AddGob(Gob.CreateGob(gob));
            activeArena = arena;
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
            addedGobs.Add(gob);
            gob.Activate();
        }

        /// <summary>
        /// Removes a gob from the game.
        /// </summary>
        /// <param name="gob">The gob to remove.</param>
        public void RemoveGob(Gob gob)
        {
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

        /// <summary>
        /// Performs the specified action on each gob of the specified category.
        /// </summary>
        /// The intended values for the type parameter are subinterfaces of IGob.
        /// <param name="action">The Action delegate to perform on each gob of the specified category.</param>
        /// <typeparam name="T">The kind of gobs to loop through.</typeparam>
        public void ForEachGob<T>(Action<Gob> action) where T : IGob
        {
            foreach (Gob gob in gobs)
                if (gob is T)
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
        }

        /// <summary>
        /// Removes a particle generator from the game.
        /// </summary>
        /// <param name="pEng">Particle generator to add to update and draw cycle</param>
        public void RemoveParticleEngine(ParticleEngine pEng)
        {
            particleEngines.Remove(pEng);
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
        /// Adds a viewport.
        /// </summary>
        /// <param name="viewport">Viewport to add.</param>
        public void AddViewport(Viewport viewport)
        {
            viewports.Add(viewport);
        }

        /// <summary>
        /// Removes all viewports.
        /// </summary>
        public void ClearViewports()
        {
            viewports.Clear();
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
            foreach (Gob gob in removedGobs)
            {
                physics.Unregister(gob);
                gob.Dispose();
                gobs.Remove(gob);
            }
            removedGobs.Clear();
        }

        #endregion miscellaneous

    }
}
