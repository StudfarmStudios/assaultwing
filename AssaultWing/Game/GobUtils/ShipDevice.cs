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

        public enum FiringSoundType
        {
            /// <summary>
            /// Play firing sound once at the beginning of the burst.
            /// </summary>
            Once,

            /// <summary>
            /// Play firing sound once for every shot but at most once each frame.
            /// </summary>
            EveryShot
        }

        #region Fields

        private const string FIRING_FAIL_SOUND = "WeaponFail";

        /// <summary>
        /// Name of the icon of the weapon, to be displayed in weapon selection 
        /// and bonus display.
        /// </summary>
        [TypeParameter]
        private CanonicalString iconName;

        /// <summary>
        /// The sound to play when firing.
        /// </summary>
        [TypeParameter]
        private string fireSound;

        /// <summary>
        /// How to play the firing sound.
        /// </summary>
        [TypeParameter]
        private FiringSoundType fireSoundType;

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

        [TypeParameter]
        private ShipDeviceInfo _deviceInfo;

        private ChargeProvider _chargeProvider;
        private float _charge;
        private bool _visualsCreatedThisFrame;
        private bool _soundPlayedThisFrame;

        #endregion

        #region Properties

        public ShipDeviceInfo DeviceInfo { get { return _deviceInfo; } set { _deviceInfo = value; } }

        /// <summary>
        /// Name of the icon of the weapon, to be displayed in weapon selection 
        /// and bonus display.
        /// </summary>
        public CanonicalString IconName { get { return iconName; } set { iconName = value; } }

        /// <summary>
        /// Names of all textures that this weapon will ever use.
        /// </summary>
        public IEnumerable<CanonicalString> TextureNames
        {
            get { return new List<CanonicalString> { iconName, DeviceInfo.IconEquipName }; }
        }

        /// <summary>
        /// The arena in which the weapon lives.
        /// </summary>
        public Arena Arena { get; set; }

        /// <summary>
        /// The player who owns the ship who owns this device, or <c>null</c> if none exists.
        /// </summary>
        public Player PlayerOwner { get { return owner == null ? null : owner.Owner; } }

        /// <summary>
        /// The purpose for which the owner is using this device.
        /// </summary>
        public OwnerHandleType OwnerHandle { get; protected set; }

        public float LoadTimeMultiplier { get { return loadTimeMultiplier; } set { loadTimeMultiplier = value; } }
        
        /// <summary>
        /// The time in seconds that it takes for the weapon to fire again 
        /// after being fired once.
        /// </summary>
        public float LoadTime { get { return loadTime; } }

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
        // TODO: Move FireCharge and fireCharge to FiringOperatorSingle
        public float FireCharge { get { return fireCharge; } }

        /// <summary>
        /// Amount of charge required for one second of rapid firing the weapon.
        /// </summary>
        // TODO: Move FireChargePerSecond and fireChargePerSecond to FiringOperatorContinuous
        public float FireChargePerSecond { get { return fireChargePerSecond; } }

        public FiringOperator FiringOperator { get; set; }

        // TODO: Move ShotCount and shotCount to FiringOperatorSingle
        public int ShotCount { get { return shotCount; } }
        // TODO: Move ShotSpacing and shotSpacing to FiringOperatorSingle
        public float ShotSpacing { get { return shotSpacing; } }

        #endregion Properties

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public ShipDevice()
        {
            iconName = (CanonicalString)"dummytexture";
            fireSound = "dummysound";
            fireSoundType = FiringSoundType.EveryShot;
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
            OwnerHandle = 0;
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
        public static ShipDevice CreateDevice(CanonicalString deviceName, OwnerHandleType ownerHandle, Ship ship)
        {
            var device = (ShipDevice)Clonable.Instantiate(deviceName);
            if (ownerHandle == OwnerHandleType.PrimaryWeapon ||
                ownerHandle == OwnerHandleType.SecondaryWeapon)
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
            device.PlayerOwner.Game.DataEngine.Devices.Add(device);
            return device;
        }

        #region Public methods

        /// <param name="owner">The ship to attach to.</param>
        /// <param name="ownerHandle">A handle for identifying the device at the owner.</param>
        public void AttachTo(Ship owner, OwnerHandleType ownerHandle)
        {
            this.owner = owner;
            this.OwnerHandle = ownerHandle;
            _chargeProvider = owner.GetChargeProvider(ownerHandle);
            _charge = ChargeMax;
        }

        /// <summary>
        /// Called when the device is added to a game. Subclasses can initialize here things
        /// that couldn't be initialized in the constructor e.g. due to lack of data.
        /// </summary>
        public virtual void Activate()
        {
            FiringOperator = new FiringOperatorSingle(this);
        }

        /// <summary>
        /// Fires (uses) the device.
        /// </summary>
        public void Fire(AW2.UI.ControlState triggerState)
        {
            if (owner.Disabled) return;
            if (!FiringOperator.IsFirePressed(triggerState)) return;
            bool success = PermissionToFire(FiringOperator.CanFire) && FiringOperator.TryFire();
            if (success)
            {
                if (fireSoundType == FiringSoundType.Once) PlayFiringSound();
            }
            else
                PlayerOwner.Game.SoundEngine.PlaySound(FIRING_FAIL_SOUND);
        }

        public virtual void Update()
        {
            _visualsCreatedThisFrame = false;
            _soundPlayedThisFrame = false;
            bool shootOnceAFrame = shotSpacing <= 0;
            bool shotThisFrame = false;
            while (FiringOperator.IsItTimeToShoot && !(shootOnceAFrame && shotThisFrame))
            {
                shotThisFrame = true;
                if (fireSoundType == FiringSoundType.EveryShot) PlayFiringSound();
                CreateVisuals();
                ShootImpl();
                FiringOperator.ShotFired();
            }
            FiringOperator.Update();
        }

        /// <summary>
        /// Releases all resources allocated by the device.
        /// </summary>
        public virtual void Dispose() { }

        public virtual void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((Half)_charge);
                writer.Write((bool)_visualsCreatedThisFrame);
                writer.Write((bool)_soundPlayedThisFrame);
            }
        }

        public virtual void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                _charge = reader.ReadHalf();
                bool mustCreateVisuals = reader.ReadBoolean();
                bool mustPlaySound = reader.ReadBoolean();
                if (mustCreateVisuals) CreateVisualsImpl();
                if (mustPlaySound) PlayFiringSoundImpl();
            }
        }

        #endregion Public methods

        #region Nonpublic methods

        protected abstract void CreateVisualsImpl();
        protected abstract void ShootImpl();
        protected virtual bool PermissionToFire(bool canFire) { return true; }

        private void CreateVisuals()
        {
            if (PlayerOwner.Game.NetworkMode == NetworkMode.Client) return;
            CreateVisualsImpl();
            _visualsCreatedThisFrame = true;
        }

        private void PlayFiringSound()
        {
            if (PlayerOwner.Game.NetworkMode == NetworkMode.Client) return;
            PlayFiringSoundImpl();
            _soundPlayedThisFrame = true;
        }

        private void PlayFiringSoundImpl()
        {
            if (fireSound != "") PlayerOwner.Game.SoundEngine.PlaySound(fireSound);
        }

        #endregion Nonpublic methods
    }
}
