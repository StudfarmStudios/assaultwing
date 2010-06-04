using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Net;

namespace AW2.Game.GobUtils
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

        #region Fields

        private const string FIRING_FAIL_SOUND = "WeaponFail";

        /// <summary>
        /// Name of the icon of the weapon, to be displayed in weapon selection 
        /// and bonus display.
        /// </summary>
        [TypeParameter]
        private CanonicalString iconName;

        /// <summary>
        /// Name of the weapon's icon in the equip menu main display.
        /// </summary>
        [TypeParameter]
        private CanonicalString iconEquipName;

        /// <summary>
        /// The sound to play when firing.
        /// </summary>
        [TypeParameter]
        private string fireSound;

        /// <summary>
        /// Number of shots to shoot in a series.
        /// </summary>
        [TypeParameter]
        private int shotCount;

        /// <summary>
        /// Temporal spacing between successive shots in a series, in seconds.
        /// Zero or less means once each frame.
        /// </summary>
        [TypeParameter]
        private float shotSpacing;

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
        private float fireCharge;

        /// <summary>
        /// Amount of charge required for one second of rapid firing the weapon.
        /// </summary>
        [TypeParameter]
        private float fireChargePerSecond;

        private ChargeProvider _chargeProvider;
        private float _charge;
        private bool _flashAndBangCreated;
        private TimeSpan nextShot;
        private int shotsLeft;

        #endregion

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
        private Player PlayerOwner { get { return owner == null ? null : owner.Owner; } }

        public float LoadTimeMultiplier { get { return loadTimeMultiplier; } set { loadTimeMultiplier = value; } }
        
        /// <summary>
        /// The time in seconds that it takes for the weapon to fire again 
        /// after being fired once.
        /// </summary>
        public float LoadTime { get { return loadTime; } }

        /// <summary>
        /// Time from which on the weapon is loaded, in game time.
        /// </summary>
        public TimeSpan LoadedTime { get; protected set; }

        /// <summary>
        /// Is the weapon loaded. The setter is for game clients only.
        /// </summary>
        public bool Loaded
        {
            get { return FireMode == FireModeType.Continuous || LoadedTime <= Arena.TotalTime; }
            set
            {
                if (value && !Loaded) LoadedTime = Arena.TotalTime;
                if (!value && Loaded) LoadedTime = Arena.TotalTime + TimeSpan.FromSeconds(1);
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
            fireSound = "dummysound";
            shotCount = 3;
            shotSpacing = 0.2f;
            loadTime = 0.5f;
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
                var boneIs = ship.GetNamedPositions("Gun");
                if (boneIs.Length == 0) Log.Write("Warning: Ship found no gun barrels in its 3D model");
                var boneIndices =
                    (from pair in boneIs
                    orderby pair.Key
                    select pair.Value).ToArray();
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
        public virtual void Activate()
        {
            FireMode = FireModeType.Single;
        }

        /// <summary>
        /// Fires (uses) the device.
        /// </summary>
        public void Fire(AW2.UI.ControlState triggerState)
        {
            if (owner.Disabled) return;
            bool fail = false;
            switch (FireMode)
            {
                case FireModeType.Single:
                    if (triggerState.Pulse)
                    {
                        if (PermissionToFire(CanFire) && CanFire)
                            StartFiring();
                        else
                            fail = true;
                    }
                    break;
                case FireModeType.Continuous:
                    if (triggerState.Force > 0)
                    {
                        if (PermissionToFire(CanFire) && CanFire) StartFiring();
                        // Note: Never play fail sound in continuous firing mode, it will repeat often and be annoying
                    }
                    break;
                default: throw new ApplicationException("Unknown FireMode " + FireMode);
            }
            if (fail) AssaultWing.Instance.SoundEngine.PlaySound(FIRING_FAIL_SOUND);
        }

        private bool cannotFireFlagged = false;

        public virtual void Update()
        {
            // Stuff for sending messages when device is loaded (done only for singlefire types)
            if (FireMode == FireModeType.Single && ownerHandle != OwnerHandleType.PrimaryWeapon)
            {
                if (!CanFire && !cannotFireFlagged)
                {
                    cannotFireFlagged = true;
                }
                else if (CanFire && cannotFireFlagged)
                {
                    cannotFireFlagged = false;
                    PlayerOwner.SendMessage(TypeName + " ready to use", Player.PLAYER_STATUS_COLOR);
                }
            }
            
            _flashAndBangCreated = false;
            bool shootOnceAFrame = shotSpacing <= 0;
            bool shotThisFrame = false;
            while (IsItTimeToShoot() && !(shootOnceAFrame && shotThisFrame))
            {
                shotThisFrame = true;
                if (AssaultWing.Instance.NetworkMode != NetworkMode.Client) CreateFlashAndBang();
                ShootImpl();
                nextShot += TimeSpan.FromSeconds(shotSpacing);
                switch (FireMode)
                {
                    case FireModeType.Single:
                        --shotsLeft;
                        if (shotsLeft == 0) DoneFiring();
                        break;
                    case FireModeType.Continuous:
                        shotsLeft = 1;
                        break;
                    default: throw new ApplicationException("Unknown FireMode " + FireMode);
                }
            }
            if (FireMode == FireModeType.Continuous)
            {
                shotsLeft = 0;
                DoneFiring();
            }
        }

        /// <summary>
        /// Releases all resources allocated by the device.
        /// </summary>
        public virtual void Dispose() { }

        #endregion Public methods

        #region Protected methods

        protected abstract void CreateVisuals();
        protected abstract void ShootImpl();
        protected virtual bool PermissionToFire(bool canFire) { return true; }

        #endregion Protected methods

        #region Private methods

        private bool IsItTimeToShoot()
        {
            if (shotsLeft <= 0) return false;
            if (nextShot > Arena.TotalTime) return false;
            return true;
        }

        private void StartFiring()
        {
            if (!CanFire) return;
            switch (FireMode)
            {
                case FireModeType.Single:
                    Charge -= FireCharge;
                    // Make the weapon unloaded for eternity until someone calls DoneFiring()
                    LoadedTime = TimeSpan.MaxValue;
                    shotsLeft = shotCount;
                    break;
                case FireModeType.Continuous:
                    Charge -= FireChargePerSecond * (float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds;
                    shotsLeft = 1;
                    break;
                default: throw new ApplicationException("Unknown FireMode " + FireMode);
            }

            // Load time doesn't pile up
            if (nextShot < Arena.TotalTime)
                nextShot = Arena.TotalTime;
        }

        private void DoneFiring()
        {
            switch (FireMode)
            {
                case FireModeType.Single:
                    LoadedTime = Arena.TotalTime + TimeSpan.FromSeconds(LoadTime * LoadTimeMultiplier);
                    break;
                case FireModeType.Continuous:
                    break;
            }
        }

        private void CreateFlashAndBang()
        {
            PlayFiringSound();
            CreateVisuals();
            _flashAndBangCreated = true;
        }

        private void PlayFiringSound()
        {
            if (_flashAndBangCreated) return;
            if (fireSound != "") AssaultWing.Instance.SoundEngine.PlaySound(fireSound);
        }

        #endregion

        #region INetworkSerializable Members

        public virtual void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((Half)_charge);
                writer.Write((bool)_flashAndBangCreated);
            }
        }

        public virtual void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, TimeSpan messageAge)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                _charge = reader.ReadHalf();
                bool mustCreateFlashAndBang = reader.ReadBoolean();
                if (mustCreateFlashAndBang) CreateFlashAndBang();
            }
        }

        #endregion
    }
}
