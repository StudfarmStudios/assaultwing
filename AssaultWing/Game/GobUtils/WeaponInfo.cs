using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    public class WeaponInfo
    {
        [TypeParameter]
        private EquipInfo.EquipInfoAmountType _singleShotDamage;

        [TypeParameter]
        private EquipInfo.EquipInfoAmountType _shotSpeed;

        [TypeParameter]
        private EquipInfo.EquipInfoAmountType _recoilMomentum;

        public EquipInfo.EquipInfoAmountType SingleShotDamage { get { return _singleShotDamage; } set { _singleShotDamage = value; } }
        public EquipInfo.EquipInfoAmountType ShotSpeed { get { return _shotSpeed; } set { _shotSpeed = value; } }
        public EquipInfo.EquipInfoAmountType RecoilMomentum { get { return _recoilMomentum; } set { _recoilMomentum = value; } }
    }
}
