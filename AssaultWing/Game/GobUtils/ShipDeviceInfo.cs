using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    public class ShipDeviceInfo : EquipInfo
    {
        [TypeParameter]
        private EquipInfoAmountType _reloadTime;

        [TypeParameter]
        private EquipInfoAmountType _energyUsage;

        [TypeParameter]
        private EquipInfoAmountType _usageType;

        public EquipInfoAmountType ReloadTime { get { return _reloadTime; } set { _reloadTime = value; } }
        public EquipInfoAmountType EnergyUsage { get { return _energyUsage; } set { _energyUsage = value; } }
        public EquipInfoAmountType usageType { get { return _usageType; } set { _usageType = value; } }
    }
}
