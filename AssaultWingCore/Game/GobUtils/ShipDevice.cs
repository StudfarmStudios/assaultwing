using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Helpers.Serialization;

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

        private enum SerializationState { DontSerialize, SerializeNextAvailableFrame, SerializedThisFrame };

        #region Fields

        /// <summary>
        /// Name of the icon of the weapon, to be displayed in weapon selection 
        /// and bonus display.
        /// </summary>
        [TypeParameter]
        private CanonicalString _iconName;

        /// <summary>
        /// The sound to play when firing.
        /// </summary>
        [TypeParameter]
        private string _fireSound;

        /// <summary>
        /// How to play the firing sound.
        /// </summary>
        [TypeParameter]
        private FiringSoundType _fireSoundType;

        /// <summary>
        /// Number of shots to shoot in a series.
        /// </summary>
        [TypeParameter]
        private int _shotCount;

        /// <summary>
        /// Temporal spacing between successive shots in a series, in seconds.
        /// Zero or less means once each frame.
        /// </summary>
        [TypeParameter]
        private float _shotSpacing;

        /// <summary>
        /// The time in seconds that it takes for the weapon to fire again after being fired once.
        /// Use the property <see cref="LoadTime"/> to see the current load time
        /// with applied bonuses.
        /// </summary>
        [TypeParameter]
        protected float _loadTime;

        /// <summary>
        /// Bonus Multiplier for loadtime
        /// </summary>
        protected float _loadTimeMultiplier;

        /// <summary>
        /// Amount of charge required to fire the weapon once.
        /// </summary>
        [TypeParameter]
        private float _fireCharge;

        /// <summary>
        /// Amount of charge required for one second of rapid firing the weapon.
        /// </summary>
        [TypeParameter]
        private float _fireChargePerSecond;

        [TypeParameter]
        private ShipDeviceInfo _deviceInfo;

        private ChargeProvider _chargeProvider;
        private float _charge;
        private SerializationState _visualsCreatedThisFrame;
        private SerializationState _soundPlayedThisFrame;

        #endregion

        #region Properties

        public ShipDeviceInfo DeviceInfo { get { return _deviceInfo; } set { _deviceInfo = value; } }

        /// <summary>
        /// Name of the icon of the weapon, to be displayed in weapon selection 
        /// and bonus display.
        /// </summary>
        public CanonicalString IconName { get { return _iconName; } set { _iconName = value; } }

        /// <summary>
        /// Names of all textures that this weapon will ever use.
        /// </summary>
        public IEnumerable<CanonicalString> TextureNames
        {
            get { return new List<CanonicalString> { _iconName, DeviceInfo.IconEquipName }; }
        }

        /// <summary>
        /// The arena in which the weapon lives.
        /// </summary>
        public Arena Arena { get; set; }

        /// <summary>
        /// The ship this weapon is attached to.
        /// </summary>
        public Ship Owner { get; protected set; }

        /// <summary>
        /// The player who owns the ship who owns this device, or <c>null</c> if none exists.
        /// </summary>
        public Player PlayerOwner { get { return Owner == null ? null : Owner.Owner; } }

        /// <summary>
        /// The purpose for which the owner is using this device.
        /// </summary>
        public OwnerHandleType OwnerHandle { get; protected set; }

        public float LoadTimeMultiplier { get { return _loadTimeMultiplier; } set { _loadTimeMultiplier = value; } }

        /// <summary>
        /// The time in seconds that it takes for the weapon to fire again 
        /// after being fired once.
        /// </summary>
        public float LoadTime { get { return _loadTime; } }

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
        public float FireCharge { get { return _fireCharge; } }

        /// <summary>
        /// Amount of charge required for one second of rapid firing the weapon.
        /// </summary>
        // TODO: Move FireChargePerSecond and fireChargePerSecond to FiringOperatorContinuous
        public float FireChargePerSecond { get { return _fireChargePerSecond; } }

        public FiringOperator FiringOperator { get; set; }

        // TODO: Move ShotCount and shotCount to FiringOperatorSingle
        public int ShotCount { get { return _shotCount; } }
        // TODO: Move ShotSpacing and shotSpacing to FiringOperatorSingle
        public float ShotSpacing { get { return _shotSpacing; } }

        #endregion Properties

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public ShipDevice()
        {
            _iconName = (CanonicalString)"dummytexture";
            _fireSound = "dummysound";
            _fireSoundType = FiringSoundType.EveryShot;
            _shotCount = 3;
            _shotSpacing = 0.2f;
            _loadTime = 0.5f;
            _fireCharge = 100;
            _fireChargePerSecond = 500;
            _loadTimeMultiplier = 1;
        }

        public ShipDevice(CanonicalString typeName)
            : base(typeName)
        {
            Owner = null;
            OwnerHandle = 0;
            _loadTimeMultiplier = 1;
        }

        #region Public methods

        /// <param name="owner">The ship to attach to.</param>
        /// <param name="ownerHandle">A handle for identifying the device at the owner.</param>
        public void AttachTo(Ship owner, OwnerHandleType ownerHandle)
        {
            Owner = owner;
            OwnerHandle = ownerHandle;
            _chargeProvider = owner.GetChargeProvider(ownerHandle);
            _charge = ChargeMax;
        }

        /// <summary>
        /// Called when the device is added to a game. Subclasses can initialize here things
        /// that couldn't be initialized in the constructor e.g. due to lack of data.
        /// </summary>
        public void Activate()
        {
            FiringOperator = new FiringOperator(this);
        }

        /// <summary>
        /// Fires (uses) the device.
        /// </summary>
        public virtual void Fire(AW2.UI.ControlState triggerState)
        {
            if (Owner.Disabled) return;
            if (!triggerState.Pulse) return;
            if (PermissionToFire() && FiringOperator.TryFire())
            {
                if (_fireSoundType == FiringSoundType.Once) PlayFiringSound();
            }
            else
                PlayFiringFailedSound();
        }

        public virtual void Update()
        {
            var shootOnceAFrame = _shotSpacing <= 0;
            var shotThisFrame = false;
            while (FiringOperator.IsItTimeToShoot && !(shootOnceAFrame && shotThisFrame))
            {
                shotThisFrame = true;
                if (_fireSoundType == FiringSoundType.EveryShot) PlayFiringSound();
                CreateVisuals();
                ShootImpl();
                FiringOperator.ShotFired();
            }
            FiringOperator.Update();
            if (_soundPlayedThisFrame == SerializationState.SerializedThisFrame)
                _soundPlayedThisFrame = SerializationState.DontSerialize;
            if (_visualsCreatedThisFrame == SerializationState.SerializedThisFrame)
                _visualsCreatedThisFrame = SerializationState.DontSerialize;
        }

        /// <summary>
        /// Releases all resources allocated by the device.
        /// </summary>
        public virtual void Dispose() { }

        public virtual void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            checked
            {
                if ((mode & SerializationModeFlags.VaryingData) != 0)
                {
                    byte data = (byte)(0x3f * Charge / ChargeMax);
                    if (_visualsCreatedThisFrame != SerializationState.DontSerialize) data |= 0x40;
                    if (_soundPlayedThisFrame != SerializationState.DontSerialize) data |= 0x80;
                    writer.Write((byte)data);
                    _visualsCreatedThisFrame = SerializationState.SerializedThisFrame;
                    _soundPlayedThisFrame = SerializationState.SerializedThisFrame;
                }
            }
        }

        public virtual void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                var data = reader.ReadByte();
                _charge = (data & 0x3f) * ChargeMax / 0x3f;
                bool mustCreateVisuals = (data & 0x40) != 0;
                bool mustPlaySound = (data & 0x80) != 0;
                if (mustCreateVisuals) CreateVisualsImpl();
                if (mustPlaySound) PlayFiringSoundImpl();
            }
        }

        #endregion Public methods

        #region Nonpublic methods

        protected abstract void CreateVisualsImpl();
        protected abstract void ShootImpl();
        protected virtual bool PermissionToFire() { return true; }

        private void CreateVisuals()
        {
            if (PlayerOwner.Game.NetworkMode == NetworkMode.Client) return;
            CreateVisualsImpl();
            _visualsCreatedThisFrame = SerializationState.SerializeNextAvailableFrame;
        }

        private void PlayFiringFailedSound()
        {
            if (PlayerOwner != null && !PlayerOwner.IsRemote)
                PlayerOwner.Game.SoundEngine.PlaySound("WeaponFail");
        }

        private void PlayFiringSound()
        {
            if (PlayerOwner.Game.NetworkMode == NetworkMode.Client) return;
            PlayFiringSoundImpl();
            _soundPlayedThisFrame = SerializationState.SerializeNextAvailableFrame;
        }

        private void PlayFiringSoundImpl()
        {
            if (_fireSound != "") Owner.Game.SoundEngine.PlaySound(_fireSound, Owner);
        }

        #endregion Nonpublic methods
    }
}
