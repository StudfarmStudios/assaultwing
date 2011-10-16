using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Game.GobUtils;
using Microsoft.Xna.Framework;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// Semi-intellectual armed flying gob.
    /// </summary>
    public class Bot : Gob
    {
        [TypeParameter]
        private CanonicalString _weaponName;

        private Weapon _weapon;
        private float _rotationSpeed; // radians/second

        /// <summary>
        /// Only for deserialization.
        /// </summary>
        public Bot()
        {
            _weaponName = (CanonicalString)"dummyweapontype";
        }

        public Bot(CanonicalString typeName)
           : base(typeName)
        {
            Gravitating = false;
        }

        public override void Activate()
        {
            base.Activate();
            _weapon = Weapon.Create(_weaponName);
            _weapon.AttachTo(this, ShipDevice.OwnerHandleType.PrimaryWeapon);
            Game.DataEngine.Devices.Add(_weapon);
            if (Game.NetworkMode != Core.NetworkMode.Client)
                _rotationSpeed = RandomHelper.GetRandomSign() * (MathHelper.TwoPi / 10 + RandomHelper.GetRandomFloat(-MathHelper.TwoPi / 100, MathHelper.TwoPi / 100));
        }

        public override void Update()
        {
            base.Update();
            Aim();
            Shoot();
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                base.Serialize(writer, mode);
                if (mode.HasFlag(SerializationModeFlags.ConstantData))
                {
                    writer.Write((float)_rotationSpeed);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if (mode.HasFlag(SerializationModeFlags.ConstantData))
            {
                _rotationSpeed = reader.ReadSingle();
            }
        }

        private void Aim()
        {
            Rotation += _rotationSpeed * (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
        }

        private void Shoot()
        {
            if (Game.NetworkMode == Core.NetworkMode.Client) return;
            _weapon.TryFire(new UI.ControlState(1, true));
        }
    }
}
