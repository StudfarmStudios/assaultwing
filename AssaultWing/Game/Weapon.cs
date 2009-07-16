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
        CanonicalString typeName;

        /// <summary>
        /// Names of the weapon type upgrades of the weapon.
        /// </summary>
        [TypeParameter]
        CanonicalString[] upgradeNames;

        /// <summary>
        /// Name of the icon of the weapon, to be displayed in weapon selection 
        /// and bonus display.
        /// </summary>
        [TypeParameter]
        CanonicalString iconName;

        /// <summary>
        /// Name of the weapon's icon in the equip menu main display.
        /// </summary>
        [TypeParameter]
        CanonicalString iconEquipName;

        /// <summary>
        /// The ship this weapon is attached to.
        /// </summary>
        protected Ship owner;

        /// <summary>
        /// A handle for identifying us at the owner.
        /// </summary>
        /// In practice this is <b>1</b> for primary weapons and
        /// <b>2</b> for secondary weapons.
        [RuntimeState]
        protected int ownerHandle;

        /// <summary>
        /// Indices of the bones that defines the weapon's barrels' locations 
        /// on the owning ship.
        /// </summary>
        protected int[] boneIndices;

        /// <summary>
        /// What type of gobs the weapon shoots out.
        /// </summary>
        /// This gob type is assumed to be an instance of class <see cref="AW2.Game.Gobs.Bullet"/>.
        [TypeParameter]
        protected CanonicalString shotTypeName;
        
        /// <summary>
        /// The time in seconds that it takes for the weapon to fire again after being fired once.
        /// Use the property <see cref="LoadTime"/> to see the current load time
        /// with applied bonuses.
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

        #endregion // Weapon fields

        #region Weapon properties

        /// <summary>
        /// Get the weapon type name.
        /// </summary>
        public CanonicalString TypeName { get { return typeName; } }
        
        /// <summary>
        /// Names of the weapon type upgrades of the weapon, in order of upgrades.
        /// </summary>
        public CanonicalString[] UpgradeNames { get { return upgradeNames; } }

        /// <summary>
        /// Name of the icon of the weapon, to be displayed in weapon selection 
        /// and bonus display.
        /// </summary>
        public CanonicalString IconName { get { return iconName; } set { iconName = value; } }

        /// <summary>
        /// Name of the weapon's icon in the equip menu main display.
        /// </summary>
        public CanonicalString IconEquipName { get { return iconEquipName; } set { iconEquipName = value; } }

        /// <summary>
        /// Names of all textures that this weapon will ever use.
        /// </summary>
        public IEnumerable<CanonicalString> TextureNames
        {
            get { return new List<CanonicalString> { iconName, iconEquipName }; }
        }

        /// <summary>
        /// The ship this weapon is attached to.
        /// </summary>
        public Ship Owner { get { return owner; } set { owner = value; } } // !!! hack

        /// <summary>
        /// The arena in which the weapon lives.
        /// </summary>
        public Arena Arena { get; set; }

        /// <summary>
        /// A handle for identifying the weapon at the owner.
        /// </summary>
        public int OwnerHandle { get { return ownerHandle; } set { ownerHandle = value; } }

        /// <summary>
        /// The player who owns the ship who owns the weapon, or <c>null</c> if none exists.
        /// </summary>
        Player PlayerOwner { get { return owner == null ? null : owner.Owner; } }

        /// <summary>
        /// The time in seconds that it takes for the weapon to fire again 
        /// after being fired once.
        /// </summary>
        public float LoadTime
        {
            get
            {
                if (PlayerOwner != null)
                {
                    if (ownerHandle == 1 && PlayerOwner.HasBonus(PlayerBonus.Weapon1LoadTime))
                        return loadTime / 2;
                    if (ownerHandle == 2 && PlayerOwner.HasBonus(PlayerBonus.Weapon2LoadTime))
                        return loadTime / 2;
                }
                return loadTime;
            }
        }

        /// <summary>
        /// Time from which on the weapon is loaded, in game time.
        /// </summary>
        public TimeSpan LoadedTime { get { return loadedTime; } }

        /// <summary>
        /// Is the weapon loaded. The setter is for game clients only.
        /// </summary>
        public bool Loaded
        {
            get { return loadedTime <= AssaultWing.Instance.GameTime.TotalGameTime; }
            set
            {
                if (value && !Loaded) loadedTime = AssaultWing.Instance.GameTime.TotalGameTime;
                if (!value && Loaded) loadedTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(1);
            }
        }

        /// <summary>
        /// Amount of charge required to fire the weapon.
        /// </summary>
        public float FireCharge { get { return fireCharge; } }

        /// <summary>
        /// <b>true</b> iff there is no obstruction to the weapon being fired.
        /// </summary>
        public bool CanFire { get { return Loaded && FireCharge <= owner.GetCharge(ownerHandle); } }

        #endregion // Weapon properties

        static Weapon()
        {
            // Check that important constructors have been declared
            Helpers.Log.Write("Checking weapon constructors");
            foreach (Type type in Array.FindAll<Type>(System.Reflection.Assembly.GetExecutingAssembly().GetTypes(),
                t => typeof(Weapon).IsAssignableFrom(t)))
            {
                if (null == type.GetConstructor(Type.EmptyTypes))
                    throw new Exception("Missing constructor " + type.Name + "()");
                if (null == type.GetConstructor(new Type[] { 
                    typeof(CanonicalString), typeof(Ship), typeof(int), typeof(int[]), }))
                    throw new Exception("Missing constructor " + type.Name + "(CanonicalString, Ship, int, int[])");
            }
        }

        /// <summary>
        /// Creates an uninitialised weapon.
        /// </summary>
        /// This constructor is only for serialisation.
        public Weapon()
        {
            this.typeName = (CanonicalString)"unknown weapon type";
            this.upgradeNames = new CanonicalString[] { (CanonicalString)"dummyweapontype", };
            this.iconName = (CanonicalString)"dummytexture";
            this.iconEquipName = (CanonicalString)"dummytexture";
            this.owner = null;
            this.ownerHandle = 0;
            this.boneIndices = new int[] { 0 };
            this.shotTypeName = (CanonicalString)"dummygobtype";
            this.loadTime = 0.5f;
            this.loadedTime = new TimeSpan(1, 2, 3);
            this.fireCharge = 100;
            this.recoilMomentum = 10000;
        }

        /// <summary>
        /// Creates a new weapon of the specified type.
        /// </summary>
        /// <param name="typeName">The type of the weapon.</param>
        protected Weapon(CanonicalString typeName)
        {
            // Initialise fields from the weapon type's template.
            Weapon template = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(TypeTemplateType.Weapon, typeName);
            if (template.GetType() != this.GetType())
                throw new Exception("Silly programmer tries to create a weapon (type " +
                    typeName + ") using a wrong Weapon subclass (class " + this.GetType().Name);
            foreach (FieldInfo field in Serialization.GetFields(this, typeof(TypeParameterAttribute)))
                field.SetValue(this, field.GetValue(template));

            this.owner = null;
            this.ownerHandle = 0;
            this.boneIndices = new int[] { 0 };
            this.loadedTime = new TimeSpan(0);
        }

        /// <summary>
        /// Creates a new weapon of the specified type.
        /// </summary>
        /// <param name="typeName">The type of the weapon.</param>
        /// <param name="owner">The ship that owns this weapon.</param>
        /// <param name="ownerHandle">A handle for identifying the weapon at the owner.
        /// Use <b>1</b> for primary weapons and <b>2</b> for secondary weapons.</param>
        /// <param name="boneIndices">Indices of the bones that define the weapon's
        /// barrels' locations on the owning ship.</param>
        public Weapon(CanonicalString typeName, Ship owner, int ownerHandle, int[] boneIndices)
            : this(typeName)
        {
            this.owner = owner;
            this.ownerHandle = ownerHandle;
            this.boneIndices = boneIndices;
        }
  
        /// <summary>
        /// Creates a weapon of the given type.
        /// </summary>
        /// Note that you cannot call new Weapon(typeName) because then the created object
        /// won't have the fields of the subclass that 'typeName' requires. This static method
        /// takes care of finding the correct subclass.
        /// <param name="typeName">The type of the weapon.</param>
        /// <param name="owner">The ship that owns this weapon.</param>
        /// <param name="ownerHandle">A handle for identifying the weapon at the owner.
        /// Use <b>1</b> for primary weapons and <b>2</b> for secondary weapons.</param>
        /// <param name="boneIndices">Indices of the bones that define the weapon's
        /// barrels' locations on the owning ship.</param>
        /// <param name="args">Any arguments to pass to the subclass' constructor.</param>
        /// <returns>The newly created weapon.</returns>
        public static Weapon CreateWeapon(CanonicalString typeName, Ship owner, int ownerHandle, int[] boneIndices, params object[] args)
        {
            Weapon template = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(TypeTemplateType.Weapon, typeName);
            Type type = template.GetType();
            if (args.Length == 0)
                return (Weapon)Activator.CreateInstance(type, typeName, owner, ownerHandle, boneIndices);
            else
            {
                object[] newArgs = new object[args.Length + 4];
                newArgs[0] = typeName;
                newArgs[1] = owner;
                newArgs[2] = ownerHandle;
                newArgs[3] = boneIndices;
                Array.Copy(args, 0, newArgs, 4, args.Length);
                return (Weapon)Activator.CreateInstance(type, newArgs);
            }
        }

        #region Weapon public methods

        /// <summary>
        /// Fires the weapon.
        /// </summary>
        public abstract void Fire();

        /// <summary>
        /// Updates the weapon's state and performs actions true to its nature.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Releases all resources allocated by the weapon.
        /// </summary>
        public abstract void Dispose();

        #endregion Weapon public methods

        #region Weapon protected methods

        /// <summary>
        /// Prepares the weapon for firing.
        /// Subclasses should call this method when they start a new firing action.
        /// </summary>
        /// A call to <b>StartFiring</b> must be matched by a later call to
        /// <b>DoneFiring</b>.
        protected void StartFiring()
        {
            // Make the weapon unloaded for eternity until subclass calls DoneFiring().
            loadedTime = new TimeSpan(long.MaxValue);
        }

        /// <summary>
        /// Applies recoil to the owner of the weapon.
        /// Subclasses should call this method when they emit a new shot
        /// during a firing action.
        /// </summary>
        protected void ApplyRecoil()
        {
            Vector2 momentum = new Vector2((float)Math.Cos(Owner.Rotation), (float)Math.Sin(Owner.Rotation))
                * -recoilMomentum;
            AssaultWing.Instance.DataEngine.CustomOperations += () => { AssaultWing.Instance.PhysicsEngine.ApplyMomentum(Owner, momentum); };
        }

        /// <summary>
        /// Wraps up a finished firing of the weapon.
        /// Subclasses should call this method when their firing action has stopped.
        /// </summary>
        /// A call to <b>DoneFiring</b> must be matched by an earlier call to
        /// <b>StartFiring</b>.
        protected void DoneFiring()
        {
            // Reset the weapon's load time counter.
            loadedTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(LoadTime);
        }

        #endregion Weapon protected methods
    }
}
