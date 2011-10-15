using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Game.GobUtils;

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
        }

        public override void Update()
        {
            base.Update();
            Shoot();
        }

        private void Shoot()
        {
            _weapon.TryFire(new UI.ControlState(1, true));
        }
    }
}
