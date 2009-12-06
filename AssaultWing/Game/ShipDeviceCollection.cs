using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;

namespace AW2.Game
{
    /// <summary>
    /// The primary weapon, secondary weapon, and extra device of a ship.
    /// </summary>
    public class ShipDeviceCollection
    {
        #region Fields

        static readonly CanonicalString WEAPON_DEFAULT_NAME = (CanonicalString)"no weapon";

        /// <summary>
        /// The ship this collection belongs to.
        /// </summary>
        Ship ship;

        /// <summary>
        /// Amount of charge for extra devices, between <b>0</b> and <b>extraDeviceChargeMax</b>.
        /// </summary>
        float extraDeviceCharge;

        /// <summary>
        /// Amount of charge for secondary weapons, between <b>0</b> and <b>weapon2ChargeMax</b>.
        /// </summary>
        float weapon2Charge;

        /// <summary>
        /// Speed of charging for extra device charge, measured in charge units per second.
        /// </summary>
        float extraDeviceChargeSpeed;

        /// <summary>
        /// Speed of charging for secondary weapon charge, measured in charge units per second.
        /// </summary>
        float weapon2ChargeSpeed;

        // Names of weapons to create when possible.
        CanonicalString weapon1Name, weapon2Name;

        // These flags signal visual things over the network.
        bool visualWeapon1Fired;
        bool visualWeapon2Fired;

        #endregion

        #region Properties

        public ShipDevice ExtraDevice { get; private set; }

        /// <summary>
        /// The primary weapon of the ship.
        /// </summary>
        public Weapon Weapon1 { get; private set; }

        /// <summary>
        /// Name of the type of main weapon the ship is using.
        /// </summary>
        public CanonicalString Weapon1Name
        {
            get { return Weapon1 == null ? WEAPON_DEFAULT_NAME : Weapon1.TypeName; }
            set
            {
                // Null weapon means we're not yet activated. Then create weapon later.
                if (Weapon1 != null)
                {
                    AssaultWing.Instance.DataEngine.Devices.Remove(Weapon1);
                    Weapon1.Dispose();
                    Weapon1 = (Weapon)CreateDevice(value, Weapon.OwnerHandleType.PrimaryWeapon);
                }
                else
                    weapon1Name = value;
            }
        }

        /// <summary>
        /// Amount of charge for extra devices,
        /// between <b>0</b> and <b>ExtraDeviceChargeMax</b>.
        /// </summary>
        public float ExtraDeviceCharge
        {
            get { return extraDeviceCharge; }
            set { extraDeviceCharge = MathHelper.Clamp(value, 0, ExtraDeviceChargeMax); }
        }

        /// <summary>
        /// Maximum amount of charge for extra devices.
        /// </summary>
        public float ExtraDeviceChargeMax { get; private set; }

        /// <summary>
        /// Is the extra device loaded.
        /// </summary>
        public bool ExtraDeviceLoaded { get { return ExtraDevice.Loaded; } }


        public CanonicalString ExtraDeviceName { get; set; }

        /// <summary>
        /// Is any of the primary weapons loaded.
        /// </summary>
        public bool Weapon1Loaded { get { return Weapon1.Loaded; } }

        /// <summary>
        /// The secondary weapon of the ship.
        /// </summary>
        public Weapon Weapon2 { get; private set; }

        /// <summary>
        /// Name of the type of secondary weapon the ship is using.
        /// </summary>
        public CanonicalString Weapon2Name
        {
            get { return Weapon2 == null ? WEAPON_DEFAULT_NAME : Weapon2.TypeName; }
            set
            {
                // Null weapon means we're not yet activated. Then create weapon later.
                if (Weapon2 != null)
                {
                    AssaultWing.Instance.DataEngine.Devices.Remove(Weapon2);
                    Weapon2.Dispose();
                    Weapon2 = (Weapon)CreateDevice(value, Weapon.OwnerHandleType.SecondaryWeapon);
                }
                else
                    weapon2Name = value;
            }
        }

        /// <summary>
        /// Amount of charge for secondary weapons, between <b>0</b> and <b>Weapon2ChargeMax</b>.
        /// </summary>
        public float Weapon2Charge
        {
            get { return weapon2Charge; }
            set { weapon2Charge = MathHelper.Clamp(value, 0, Weapon2ChargeMax); }
        }

        /// <summary>
        /// Maximum amount of charge for secondary weapons.
        /// </summary>
        public float Weapon2ChargeMax { get; private set; }

        /// <summary>
        /// Is any of the secondary weapons loaded.
        /// </summary>
        public bool Weapon2Loaded { get { return Weapon2.Loaded; } }

        #endregion

        public ShipDeviceCollection(Ship ship)
        {
            this.ship = ship;
        }

        #region Public methods

        /// <summary>
        /// To be called from <see cref="Ship.Activate"/>
        /// </summary>
        public void Activate(float extraDeviceChargeMax, float weapon2ChargeMax,
            float extraDeviceChargeSpeed, float weapon2ChargeSpeed)
        {
            ExtraDeviceChargeMax = extraDeviceChargeMax;
            Weapon2ChargeMax = weapon2ChargeMax;
            this.extraDeviceChargeSpeed = extraDeviceChargeSpeed;
            this.weapon2ChargeSpeed = weapon2ChargeSpeed;
            if (weapon1Name != CanonicalString.Null) Weapon1 = (Weapon)CreateDevice(weapon1Name, Weapon.OwnerHandleType.PrimaryWeapon);
            if (weapon2Name != CanonicalString.Null) Weapon2 = (Weapon)CreateDevice(weapon2Name, Weapon.OwnerHandleType.SecondaryWeapon);
            if (ExtraDeviceName != CanonicalString.Null) ExtraDevice = CreateDevice(ExtraDeviceName, Weapon.OwnerHandleType.ExtraDevice);
            ExtraDeviceCharge = ExtraDeviceChargeMax;
            Weapon2Charge = Weapon2ChargeMax;
        }

        /// <summary>
        /// To be called from <see cref="Ship.Dispose"/>
        /// </summary>
        public void Dispose()
        {
            AssaultWing.Instance.DataEngine.Devices.Remove(Weapon1);
            AssaultWing.Instance.DataEngine.Devices.Remove(Weapon2);
            // TODO: AssaultWing.Instance.DataEngine.Devices.Remove(ExtraDevice);
        }

        /// <summary>
        /// To be called from <see cref="Ship.Update"/>
        /// </summary>
        public void Update(TimeSpan elapsedGameTime)
        {
            ExtraDeviceCharge += extraDeviceChargeSpeed * (float)elapsedGameTime.TotalSeconds;
            ExtraDeviceCharge = MathHelper.Clamp(ExtraDeviceCharge, 0, ExtraDeviceChargeMax);
            Weapon2Charge += weapon2ChargeSpeed * (float)elapsedGameTime.TotalSeconds;
            Weapon2Charge = MathHelper.Clamp(Weapon2Charge, 0, Weapon2ChargeMax);
        }

        /// <summary>
        /// To be called from <see cref="Ship.Serialize"/>
        /// </summary>
        public void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)Weapon1.TypeName.Canonical);
                writer.Write((int)Weapon2.TypeName.Canonical);
            }
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((Half)ExtraDeviceCharge);
                writer.Write((Half)Weapon2Charge);
                byte flags = (byte)(
                    (Weapon1Loaded ? 0x01 : 0x00) |
                    (Weapon2Loaded ? 0x02 : 0x00) |
                    (visualWeapon1Fired ? 0x04 : 0x00) |
                    (visualWeapon2Fired ? 0x08 : 0x00));
                writer.Write((byte)flags);

                visualWeapon1Fired = false;
                visualWeapon2Fired = false;
            }
        }

        /// <summary>
        /// To be called from <see cref="Ship.Deserialize"/>
        /// </summary>
        public void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode, TimeSpan messageAge)
        {
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                Weapon1Name = (CanonicalString)reader.ReadInt32();
                Weapon2Name = (CanonicalString)reader.ReadInt32();
            }
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                ExtraDeviceCharge = reader.ReadHalf();
                Weapon2Charge = reader.ReadHalf();
                byte flags = reader.ReadByte();
                if (Weapon1 != null) Weapon1.Loaded = (flags & 0x01) != 0;
                if (Weapon2 != null) Weapon2.Loaded = (flags & 0x02) != 0;
                bool weapon1Fired = (flags & 0x04) != 0;
                bool weapon2Fired = (flags & 0x08) != 0;

                Update(messageAge);
                // TODO: Fire1() and Fire2() are intended to create muzzle pengs
                // but they don't. Fix this by inheriting Weapon from Gob and serialising
                // muzzleFireEngine state over the network.
                if (weapon1Fired) Fire1(new AW2.UI.ControlState(1, true)); // HACK !!!
                if (weapon2Fired) Fire2(new AW2.UI.ControlState(1, true)); // HACK !!!
            }
        }

        /// <summary>
        /// Returns the amount of charge available for a weapon with a certain handle.
        /// </summary>
        /// <param name="ownerHandle">The owner handle of the weapon.</param>
        /// <returns>The amount of charge available for the weapon.</returns>
        public float GetCharge(Weapon.OwnerHandleType ownerHandle)
        {
            switch (ownerHandle)
            {
                case Weapon.OwnerHandleType.PrimaryWeapon: return int.MaxValue;
                case Weapon.OwnerHandleType.SecondaryWeapon: return Weapon2Charge;
                case Weapon.OwnerHandleType.ExtraDevice: return ExtraDeviceCharge;
                default:
                    Log.Write("Warning: Someone inquired weapon charge for invalid owner handle " + ownerHandle);
                    return 0;
            }
        }

        /// <summary>
        /// Uses an amount of charge available for a weapon with a certain handle.
        /// </summary>
        /// <param name="ownerHandle">The owner handle of the weapon.</param>
        /// <param name="amount">The amount of charge to use.</param>
        public void UseCharge(Weapon.OwnerHandleType ownerHandle, float amount)
        {
            switch (ownerHandle)
            {
                case Weapon.OwnerHandleType.PrimaryWeapon:
                    // Primary weapon has no charge.
                    break;
                case Weapon.OwnerHandleType.SecondaryWeapon:
                    weapon2Charge = MathHelper.Clamp(weapon2Charge - amount, 0, Weapon2ChargeMax);
                    break;
                case Weapon.OwnerHandleType.ExtraDevice:
                    extraDeviceCharge = MathHelper.Clamp(extraDeviceCharge - amount, 0, ExtraDeviceChargeMax);
                    break;
                default:
                    Log.Write("Warning: Someone tried to use weapon charge for invalid owner handle " + ownerHandle);
                    break;
            }
        }

        /// <summary>
        /// Fires the main weapon.
        /// </summary>
        public void Fire1(AW2.UI.ControlState triggerState)
        {
            if (ship.Disabled) return;
            Weapon1.Fire(triggerState);
            visualWeapon1Fired = true;
        }

        /// <summary>
        /// Fires the secondary weapon.
        /// </summary>
        public void Fire2(AW2.UI.ControlState triggerState)
        {
            if (ship.Disabled) return;
            Weapon2.Fire(triggerState);
            visualWeapon2Fired = true;
        }

        /// <summary>
        /// Uses the extra device of the ship.
        /// </summary>
        public void DoExtra(AW2.UI.ControlState triggerState)
        {
            if (ship.Disabled) return;
            ExtraDevice.Fire(triggerState);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Creates a new instance of a named ship device type. If the device is a weapon,
        /// it is instantiated at each gun barrel on the ship's 3D model.
        /// </summary>
        /// <param name="deviceName">Name of the device type.</param>
        /// <param name="ownerHandle">A handle for identifying the device at the owner.</param>
        /// <returns>The created device.</returns>
        private ShipDevice CreateDevice(CanonicalString deviceName, ShipDevice.OwnerHandleType ownerHandle)
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

        #endregion
    }
}
