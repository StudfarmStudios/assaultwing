using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Net;

namespace AW2.Game
{
    /// <summary>
    /// A device that a ship can use. 
    /// </summary>
    /// <seealso cref="Weapon"/>
    [LimitedSerialization]
    public abstract class ShipDevice : Clonable, INetworkSerializable
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
        /// Bonus Multiplier for loadtime
        /// </summary>
        protected float loadTimeMultiplier;

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

        ChargeProvider _chargeProvider;
        float _charge;

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
        /// Multiplier for loadtime
        /// multiplier<1 == bonus
        /// multiplier>1 == negative
        /// </summary>
        public float LoadTimeMultiplier{get{ return loadTimeMultiplier;}set{ loadTimeMultiplier = value;}}
        
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
                    /*
                    if (ownerHandle == OwnerHandleType.PrimaryWeapon && PlayerOwner.HasBonus(PlayerBonusTypes.Weapon1LoadTime))
                        return loadTime / 2;
                    if (ownerHandle == OwnerHandleType.SecondaryWeapon && PlayerOwner.HasBonus(PlayerBonusTypes.Weapon2LoadTime))
                        return loadTime / 2;
                     * */
                }
                return loadTime;
            }
        }

        /// <summary>
        /// Time from which on the weapon is loaded, in game time.
        /// </summary>
        public TimeSpan LoadedTime { get; protected set; }

        /// <summary>
        /// Is the weapon loaded. The setter is for game clients only.
        /// </summary>
        public bool Loaded
        {
            get { return FireMode == FireModeType.Continuous || LoadedTime <= AssaultWing.Instance.GameTime.TotalArenaTime; }
            set
            {
                if (value && !Loaded) LoadedTime = AssaultWing.Instance.GameTime.TotalArenaTime;
                if (!value && Loaded) LoadedTime = AssaultWing.Instance.GameTime.TotalArenaTime + TimeSpan.FromSeconds(1);
            }
        }

        /// <summary>
        /// Current amount of charge.
        /// </summary>
        public float Charge
        {
            get { return _charge; }
            set { _charge = MathHelper.Clamp(value, 0, ChargeMax); }
        }

        /// <summary>
        /// Maximum amount of charge.
        /// </summary>
        public float ChargeMax { get { return _chargeProvider.ChargeMax(); } }

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
                return Loaded && neededCharge <= Charge;
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
            loadTimeMultiplier = 1;
        }

        public ShipDevice(CanonicalString typeName)
            : base(typeName)
        {
            owner = null;
            ownerHandle = 0;
            LoadedTime = new TimeSpan(0);
            loadTimeMultiplier = 1;
        }

        /// <summary>
        /// Creates a new instance of a named ship device type. If the device is a weapon,
        /// it is instantiated at each gun barrel on the ship's 3D model.
        /// </summary>
        /// <param name="deviceName">Name of the device type.</param>
        /// <param name="ownerHandle">A handle for identifying the device at the owner.</param>
        /// <param name="ship">The ship to own the device.</param>
        /// <returns>The created device.</returns>
        public static ShipDevice CreateDevice(CanonicalString deviceName, ShipDevice.OwnerHandleType ownerHandle, Ship ship)
        {
            var device = (ShipDevice)Clonable.Instantiate(deviceName);
            if (ownerHandle == ShipDevice.OwnerHandleType.PrimaryWeapon ||
                ownerHandle == ShipDevice.OwnerHandleType.SecondaryWeapon)
            {
                KeyValuePair<string, int>[] boneIs = ship.GetNamedPositions("Gun");
                if (boneIs.Length == 0) Log.Write("Warning: Ship found no gun barrels in its 3D model");
                int[] boneIndices = Array.ConvertAll<KeyValuePair<string, int>, int>(boneIs, pair => pair.Value);
                ((Weapon)device).AttachTo(ship, ownerHandle, boneIndices);
            }
            else
                device.AttachTo(ship, ownerHandle);
            AssaultWing.Instance.DataEngine.Devices.Add(device);
            return device;
        }

        #region Public methods

        /// <param name="owner">The ship to attach to.</param>
        /// <param name="ownerHandle">A handle for identifying the device at the owner.</param>
        public void AttachTo(Ship owner, OwnerHandleType ownerHandle)
        {
            this.owner = owner;
            this.ownerHandle = ownerHandle;
            _chargeProvider = owner.GetChargeProvider(ownerHandle);
            _charge = ChargeMax;
        }

        /// <summary>
        /// Called when the device is added to a game. Subclasses can initialize here things
        /// that couldn't be initialized in the constructor e.g. due to lack of data.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Fires (uses) the device.
        /// </summary>
        public void Fire(AW2.UI.ControlState triggerState)
        {
            if (owner.Disabled) return;
            FireImpl(triggerState);
        }

        protected abstract void FireImpl(AW2.UI.ControlState triggerState);

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
                    Charge -= FireCharge;
                    // Make the weapon unloaded for eternity until subclass calls DoneFiring().
                    LoadedTime = TimeSpan.MaxValue;
                    break;
                case FireModeType.Continuous:
                    Charge -= FireChargePerSecond * (float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds;
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
                    LoadedTime = AssaultWing.Instance.GameTime.TotalArenaTime + TimeSpan.FromSeconds(LoadTime*LoadTimeMultiplier);
                    break;
                case FireModeType.Continuous:
                    break;
            }
        }

        #endregion Protected methods

        #region INetworkSerializable Members

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, TimeSpan messageAge)
        {
        }

        #endregion
    }
}
