using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Game.Players;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// A device that a ship can use. 
    /// </summary>
    /// <seealso cref="Weapon"/>
    [LimitedSerialization]
    public abstract class ShipDevice : Clonable
    {
        public enum OwnerHandleType { PrimaryWeapon = 1, SecondaryWeapon = 2, ExtraDevice = 3 }
        public enum FiringResult { Void, Success, Failure, NotReady };

        public enum FiringEffectPlayType
        {
            /// <summary>
            /// Play effect once at the beginning of the burst.
            /// </summary>
            Once,

            /// <summary>
            /// Play effect once for every shot but at most once each frame.
            /// </summary>
            EveryShot,
        }

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
        private FiringEffectPlayType _fireSoundType;

        /// <summary>
        /// How to play the firing visual effect.
        /// </summary>
        [TypeParameter]
        private FiringEffectPlayType _fireEffectType;

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
        private bool _previousCanFire;

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
        /// The gob this weapon is attached to.
        /// </summary>
        public Gob Owner { get; protected set; }

        /// <summary>
        /// The player who owns the ship who owns this device, or <c>null</c> if none exists.
        /// </summary>
        public Player PlayerOwner { get { return SpectatorOwner as Player; } }
        public Spectator SpectatorOwner { get { return Owner == null ? null : Owner.Owner; } }

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
        // TODO: Move FireCharge and fireCharge to FiringOperator
        public float FireCharge { get { return _fireCharge; } }

        /// <summary>
        /// Amount of charge required for one second of rapid firing the weapon.
        /// </summary>
        // TODO: Move FireChargePerSecond and fireChargePerSecond to FiringOperator
        public float FireChargePerSecond { get { return _fireChargePerSecond; } }

        public FiringOperator FiringOperator { get; set; }
        public int ShotCount { get { return _shotCount; } }
        public float ShotSpacing { get { return _shotSpacing; } }
        protected bool SendDeviceReadyMessages { get; set; }
        private bool ShootOnceAFrame { get { return _shotSpacing <= 0; } }

        #endregion Properties

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public ShipDevice()
        {
            _iconName = (CanonicalString)"dummytexture";
            _fireSound = "dummysound";
            _fireSoundType = FiringEffectPlayType.EveryShot;
            _fireEffectType = FiringEffectPlayType.EveryShot;
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
            SendDeviceReadyMessages = true;
        }

        public static ShipDevice Create(AssaultWingCore game, CanonicalString typeName)
        {
            return (ShipDevice)Clonable.Instantiate(game, typeName);
        }

        #region Public methods

        /// <param name="owner">The ship to attach to.</param>
        /// <param name="ownerHandle">A handle for identifying the device at the owner.</param>
        public void AttachTo(Gob owner, OwnerHandleType ownerHandle)
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
        public virtual void Activate()
        {
            _previousCanFire = true;
            FiringOperator = new FiringOperator(this);
        }

        public virtual FiringResult TryFire(AW2.UI.ControlState triggerState)
        {
            var result = Owner.Disabled || !triggerState.Pulse ? FiringResult.Void
                : !FiringOperator.Loaded || !FiringOperator.Charged ? FiringResult.NotReady
                : !PermissionToFire() ? FiringResult.Failure
                : FiringResult.Success;
            ExecuteFiring(result);
            return result;
        }

        public void ExecuteFiring(FiringResult result)
        {
            switch (result)
            {
                case FiringResult.Success:
                    FiringOperator.StartFiring();
                    if (_fireSoundType == FiringEffectPlayType.Once) PlayFiringSound();
                    if (_fireEffectType == FiringEffectPlayType.Once) CreateVisuals();
                    var stats = Owner.Game.Stats;
                    stats.Send(new
                    {
                        Fired = stats.GetStatsString(SpectatorOwner),
                        Role = OwnerHandle,
                        Type = TypeName.Value,
                        Pos = Owner.Pos,
                    });
                    if (PlayerOwner != null) PlayerOwner.OnWeaponFired(OwnerHandle); // TODO !!! Make PlayerOwner work with BotPlayer, too.
                    break;
                case FiringResult.Failure:
                    PlayFiringFailedSound();
                    ShowFiringFailedEffect();
                    break;
                case FiringResult.NotReady:
                    PlayFiringFailedSound();
                    break;
                case FiringResult.Void:
                    break;
                default: throw new ApplicationException();
            }
        }

        public virtual void Update()
        {
            if (Owner.Disabled) FiringOperator.DoneFiring();
            PerformFiring();
            CheckWeaponLoadedMessage();
        }

        private void PerformFiring()
        {
            while (FiringOperator.IsItTimeToShoot)
            {
                if (_fireSoundType == FiringEffectPlayType.EveryShot) PlayFiringSound();
                if (_fireEffectType == FiringEffectPlayType.EveryShot) CreateVisuals();
                ShootImpl();
                FiringOperator.ShotFired();
                if (ShootOnceAFrame) break;
            }
        }

        private void CheckWeaponLoadedMessage()
        {
            if (!SendDeviceReadyMessages) return;
            if (Owner.Game.NetworkMode == NetworkMode.Client) return;
            if (OwnerHandle == ShipDevice.OwnerHandleType.PrimaryWeapon) return;
            var canFire = FiringOperator.Loaded && FiringOperator.Charged;
            if (canFire && !_previousCanFire)
                PlayerOwner.Messages.Add(new PlayerMessage(TypeName.Value.Capitalize() + " ready to use", PlayerMessage.PLAYER_STATUS_COLOR));
            _previousCanFire = canFire;
        }

        /// <summary>
        /// Releases all resources allocated by the device.
        /// </summary>
        public virtual void Dispose() { }

        #endregion Public methods

        #region Nonpublic methods

        protected abstract void ShootImpl();
        protected virtual void CreateVisuals() { }
        protected virtual bool PermissionToFire() { return true; }
        protected virtual void ShowFiringFailedEffect() { }

        private void PlayFiringFailedSound()
        {
            if (PlayerOwner != null && PlayerOwner.IsLocal)
                PlayerOwner.Game.SoundEngine.PlaySound("WeaponFail");
        }

        private void PlayFiringSound()
        {
            if (_fireSound != "") Owner.Game.SoundEngine.PlaySound(_fireSound, Owner);
        }

        #endregion Nonpublic methods
    }
}
