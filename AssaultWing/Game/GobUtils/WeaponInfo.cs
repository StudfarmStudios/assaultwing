using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    public class WeaponInfo : EquipInfo
    {
        [TypeParameter]
        private EquipInfoAmountType _singleShotDamage;

        [TypeParameter]
        private EquipInfoAmountType _shotSpeed;

        [TypeParameter]
        private EquipInfoAmountType _recoilMomentum;

        public EquipInfoAmountType SingleShotDamage { get { return _singleShotDamage; } set { _singleShotDamage = value; } }
        public EquipInfoAmountType ShotSpeed { get { return _shotSpeed; } set { _shotSpeed = value; } }
        public EquipInfoAmountType RecoilMomentum { get { return _recoilMomentum; } set { _recoilMomentum = value; } }
    }
}
