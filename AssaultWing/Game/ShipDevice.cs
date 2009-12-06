using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game.Gobs;
using AW2.Helpers;

namespace AW2.Game
{
    /// <summary>
    /// A device that a ship can use. 
    /// </summary>
    /// <seealso cref="Weapon"/>
    [LimitedSerialization]
    public abstract class ShipDevice : Clonable
    {
        public enum OwnerHandleType { PrimaryWeapon = 1, SecondaryWeapon = 2, ExtraDevice = 3 }

        public enum FireModeType { Single, Continuous };

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
        protected OwnerHandleType ownerHandle;

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
        protected TimeSpan loadedTime;

        /// <summary>
        /// Amount of charge required to fire the weapon once.
        /// </summary>
        [TypeParameter]
        float fireCharge;

        /// <summary>
        /// Amount of charge required for one second of rapid firing the weapon.
        /// </summary>
        [TypeParameter]
        float fireChargePerSecond;

        #region Properties

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
        /// The arena in which the weapon lives.
        /// </summary>
        public Arena Arena { get; set; }

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
                    if (ownerHandle == OwnerHandleType.PrimaryWeapon && PlayerOwner.HasBonus(PlayerBonusTypes.Weapon1LoadTime))
                        return loadTime / 2;
                    if (ownerHandle == OwnerHandleType.SecondaryWeapon && PlayerOwner.HasBonus(PlayerBonusTypes.Weapon2LoadTime))
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
            get { return FireMode == FireModeType.Continuous || loadedTime <= AssaultWing.Instance.GameTime.TotalGameTime; }
            set
            {
                if (value && !Loaded) loadedTime = AssaultWing.Instance.GameTime.TotalGameTime;
                if (!value && Loaded) loadedTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(1);
            }
        }

        /// <summary>
        /// Amount of charge required to fire the weapon once.
        /// </summary>
        public float FireCharge { get { return fireCharge; } }

        /// <summary>
        /// Amount of charge required for one second of rapid firing the weapon.
        /// </summary>
        public float FireChargePerSecond { get { return fireChargePerSecond; } }

        public FireModeType FireMode { get; protected set; }

        /// <summary>
        /// <b>true</b> iff there is no obstruction to the weapon being fired.
        /// </summary>
        public bool CanFire
        {
            get
            {
                float chargeNow = owner.Devices.GetCharge(ownerHandle);
                float neededCharge;
                switch (FireMode)
                {
                    case FireModeType.Single:
                        neededCharge = FireCharge;
                        break;
                    case FireModeType.Continuous:
                        neededCharge = FireChargePerSecond * (float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds;
                        break;
                    default: throw new ApplicationException("Unexpected FireModeType: " + FireMode);
                }
                return Loaded && neededCharge <= chargeNow;
            }
        }

        #endregion Properties

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public ShipDevice()
        {
            iconName = (CanonicalString)"dummytexture";
            iconEquipName = (CanonicalString)"dummytexture";
            fireCharge = 100;
            fireChargePerSecond = 500;
        }

        public ShipDevice(CanonicalString typeName)
            : base(typeName)
        {
            owner = null;
            ownerHandle = 0;
            loadedTime = new TimeSpan(0);
        }

        #region Public methods

        /// <summary>
        /// Attaches the device to a ship.
        /// </summary>
        /// <param name="owner">The ship to attach to.</param>
        /// <param name="ownerHandle">A handle for identifying the device at the owner.</param>
        public void AttachTo(Ship owner, OwnerHandleType ownerHandle)
        {
            this.owner = owner;
            this.ownerHandle = ownerHandle;
        }

        /// <summary>
        /// Called when the device is added to a game. Subclasses can initialize here things
        /// that couldn't be initialized in the constructor e.g. due to lack of data.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Fires (uses) the device.
        /// </summary>
        public abstract void Fire(AW2.UI.ControlState triggerState);

        /// <summary>
        /// Updates the device's state. This method is called regularly.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Releases all resources allocated by the device.
        /// </summary>
        public abstract void Dispose();

        #endregion Public methods

        #region Protected methods

        /// <summary>
        /// Prepares the device for firing/using.
        /// Subclasses should call this method when they start a new firing action.
        /// </summary>
        /// A call to <b>StartFiring</b> must be matched by a later call to
        /// <b>DoneFiring</b>.
        protected void StartFiring()
        {
            if (!CanFire) throw new InvalidOperationException("This weapon cannot be fired now");
            switch (FireMode)
            {
                case FireModeType.Single:
                    owner.Devices.UseCharge(ownerHandle, FireCharge);
                    // Make the weapon unloaded for eternity until subclass calls DoneFiring().
                    loadedTime = TimeSpan.MaxValue;
                    break;
                case FireModeType.Continuous:
                    {
                        float seconds = (float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds;
                        owner.Devices.UseCharge(ownerHandle, FireChargePerSecond * seconds);
                    }
                    break;
            }
        }

        /// <summary>
        /// Wraps up a finished firing/using of the device.
        /// Subclasses should call this method when their firing action has stopped.
        /// </summary>
        /// A call to <b>DoneFiring</b> must be matched by an earlier call to
        /// <b>StartFiring</b>.
        protected void DoneFiring()
        {
            switch (FireMode)
            {
                case FireModeType.Single:
                    loadedTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(LoadTime);
                    break;
                case FireModeType.Continuous:
                    break;
            }
        }

        #endregion Protected methods
    }
}
