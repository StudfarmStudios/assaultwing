using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Ship = AW2.Game.Gobs.Ship;
using AW2.Helpers;
using Microsoft.Xna.Framework;

namespace AW2.Game
{
    /// <summary>
    /// A weapon, usually used by a ship it is attached to.
    /// </summary>
    /// A weapon can be fired only when it's loaded and it has a sufficient amount
    /// of charge for firing. When a weapon is fired, it becomes unloaded and a
    /// fixed amount of charge is reduced from it. The weapon will be loaded again
    /// after a fixed amount of time, and the level of charge regenerates
    /// at a fixed rate. Charge is managed totally by <b>Gobs.Ship</b>.
    /// 
    /// The weapon's functionality is implemented in a subclass.
    /// An instance of a subclass of Weapon represents one weapon instance
    /// that is usually attached to a ship and has its own load meters and other
    /// gametime properties.
    ///
    /// There can be several special instances of each subclass of Weapon. Each of
    /// these 'template instances' defines a weapon type by specifying values for the
    /// type parameters of that Weapon subclass. Newly created weapon instances automatically
    /// initialise their type parameter fields by copying them from a template instance.
    /// Template instances are referred to by human-readable names such as "shotgun".
    /// 
    /// Class Weapon and its subclasses use limited (de)serialisation for
    /// for saving and loading weapon types. Therefore only those fields
    /// that describe the weapon type -- not fields that describe the weapon's 
    /// state during gameplay -- should be marked as 'type parameters' by 
    /// <b>TypeParameterAttribute</b>.
    /// 
    /// Each Weapon subclass must provide a parameterless constructor that initialises all
    /// of its type parameters to descriptive and exemplary default values.
    /// <see cref="AW2.Helpers.TypeParameterAttribute"/>
    [LimitedSerialization]
    public abstract class Weapon
    {

        #region Weapon fields

        /// <summary>
        /// Weapon type name.
        /// </summary>
        [TypeParameter]
        string typeName;

        /// <summary>
        /// Names of the weapon type upgrades of the weapon.
        /// </summary>
        [TypeParameter]
        string[] upgradeNames;

        /// <summary>
        /// The ship this weapon is attached to.
        /// </summary>
        protected Ship owner;

        /// <summary>
        /// Indices of the bones that defines the weapon's barrels' locations 
        /// on the owning ship.
        /// </summary>
        protected int[] boneIndices;

        /// <summary>
        /// What type of gobs the weapon shoots out.
        /// </summary>
        /// This gob type is assumed to be an instance of class AW2.Game.Gobs.Bullet.
        [TypeParameter]
        protected string shotTypeName;
        
        /// <summary>
        /// The time in seconds that it takes for the weapon to fire again after being fired once.
        /// </summary>
        [TypeParameter]
        protected float loadTime;

        /// <summary>
        /// Time from which on the weapon is loaded, in game time.
        /// </summary>
        [RuntimeState]
        protected TimeSpan loadedTime;

        /// <summary>
        /// Amount of charge required to fire the weapon.
        /// </summary>
        [TypeParameter]
        protected float fireCharge;

        /// <summary>
        /// Recoil momentum of the weapon, measured in Newton seconds.
        /// Recoil pushes the shooter to the opposite direction.
        /// </summary>
        [TypeParameter]
        float recoilMomentum;

        /// <summary>
        /// The physics engine of the game instance this gob belongs to.
        /// </summary>
        protected PhysicsEngine physics;

        #endregion // Weapon fields

        #region Weapon properties

        /// <summary>
        /// Get the weapon type name.
        /// </summary>
        public string TypeName { get { return typeName; } }
        
        /// <summary>
        /// Names of the weapon type upgrades of the weapon, in order of upgrades.
        /// </summary>
        public string[] UpgradeNames { get { return upgradeNames; } }

        /// <summary>
        /// The ship this weapon is attached to.
        /// </summary>
        public Ship Owner { get { return owner; } set { owner = value; } } // !!! hack

        /// <summary>
        /// Is the weapon loaded.
        /// </summary>
        public bool Loaded { get { return loadedTime <= physics.TimeStep.TotalGameTime; } }

        /// <summary>
        /// Amount of charge required to fire the weapon.
        /// </summary>
        public float FireCharge { get { return fireCharge; } }

        #endregion // Weapon properties

        static Weapon()
        {
            // Check that important constructors have been declared
            Helpers.Log.Write("Checking weapon constructors");
            foreach (Type type in Array.FindAll<Type>(System.Reflection.Assembly.GetExecutingAssembly().GetTypes(),
                delegate(Type t) { return typeof(Weapon).IsAssignableFrom(t); }))
            {
                if (null == type.GetConstructor(Type.EmptyTypes))
                    throw new Exception("Missing constructor " + type.Name + "()");
                if (null == type.GetConstructor(new Type[] { 
                    typeof(string), typeof(Ship), typeof(int[]), }))
                    throw new Exception("Missing constructor " + type.Name + "(string, Ship, int[])");
            }
        }

        /// <summary>
        /// Creates an uninitialised weapon.
        /// </summary>
        /// This constructor is only for serialisation.
        public Weapon()
        {
            this.typeName = "dummyweapontype";
            this.upgradeNames = new string[] { "dummyweapontype", };
            this.owner = null;
            this.boneIndices = new int[] { 0 };
            this.shotTypeName = "dummygobtype";
            this.loadTime = 0.5f;
            this.loadedTime = new TimeSpan(1, 2, 3);
            this.fireCharge = 100;
            this.recoilMomentum = 10000;
            this.physics = null;
        }

        /// <summary>
        /// Creates a new weapon of the specified type.
        /// </summary>
        /// <param name="typeName">The type of the weapon.</param>
        public Weapon(string typeName)
        {
            // Initialise fields from the weapon type's template.
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon template = (Weapon)data.GetTypeTemplate(typeof(Weapon), typeName);
            if (template.GetType() != this.GetType())
                throw new Exception("Silly programmer tries to create a weapon (type " +
                    typeName + ") using a wrong Weapon subclass (class " + this.GetType().Name);
            foreach (FieldInfo field in Serialization.GetFields(this, typeof(TypeParameterAttribute)))
                field.SetValue(this, field.GetValue(template));

            this.owner = null;
            this.boneIndices = new int[] { 0 };
            this.loadedTime = new TimeSpan(0);
            this.physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));
        }

        /// <summary>
        /// Creates a new weapon of the specified type.
        /// </summary>
        /// <param name="typeName">The type of the weapon.</param>
        /// <param name="owner">The ship that owns this weapon.</param>
        /// <param name="boneIndices">Indices of the bones that define the weapon's
        /// barrels' locations on the owning ship.</param>
        public Weapon(string typeName, Ship owner, int[] boneIndices)
            : this(typeName)
        {
            this.owner = owner;
            this.boneIndices = boneIndices;
            this.physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));
        }
  
        /// <summary>
        /// Creates a weapon of the given type.
        /// </summary>
        /// Note that you cannot call new Weapon(typeName) because then the created object
        /// won't have the fields of the subclass that 'typeName' requires. This static method
        /// takes care of finding the correct subclass.
        /// <param name="typeName">The type of the weapon.</param>
        /// <param name="owner">The ship that owns this weapon.</param>
        /// <param name="boneIndices">Indices of the bones that define the weapon's
        /// barrels' locations on the owning ship.</param>
        /// <param name="args">Any arguments to pass to the subclass' constructor.</param>
        /// <returns>The newly created weapon.</returns>
        public static Weapon CreateWeapon(string typeName, Ship owner, int[] boneIndices, params object[] args)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon template = (Weapon)data.GetTypeTemplate(typeof(Weapon), typeName);
            Type type = template.GetType();
            if (args.Length == 0)
                return (Weapon)Activator.CreateInstance(type, typeName, owner, boneIndices);
            else
            {
                object[] newArgs = new object[args.Length + 3];
                newArgs[0] = typeName;
                newArgs[1] = owner;
                newArgs[2] = boneIndices;
                Array.Copy(args, 0, newArgs, 3, args.Length);
                return (Weapon)Activator.CreateInstance(type, newArgs);
            }
        }

        /// <summary>
        /// Fires the weapon.
        /// </summary>
        /// Overriding methods should first call <b>base.Fire()</b> and only
        /// perform firing action if it returns <b>true</b>.
        /// <returns><b>true</b> iff firing was possible.</returns>
        public virtual bool Fire()
        {
            if (Loaded)
            {
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

                // Apply recoil momentum.
                Vector2 momentum = new Vector2((float)Math.Cos(Owner.Rotation), (float)Math.Sin(Owner.Rotation))
                    * -recoilMomentum;
                data.CustomOperations += delegate(object obj)
                {
                    physics.ApplyMomentum(Owner, momentum);
                };

                // Make the weapon unloaded for eternity until subclass calls DoneFiring().
                loadedTime = new TimeSpan(long.MaxValue);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates the weapon's state and performs actions true to its nature.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Resets the weapon's load time counter. Subclasses should call this
        /// method when their firing action has stopped.
        /// </summary>
        protected void DoneFiring()
        {
            long ticks = (long)(10 * 1000 * 1000 * loadTime);
            loadedTime = physics.TimeStep.TotalGameTime.Add(new TimeSpan(ticks));
        }
    }
}
