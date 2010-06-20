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
        private EquipInfoAmountType _reloadSpeed;

        [TypeParameter]
        private EquipInfoAmountType _energyUsage;

        [TypeParameter]
        private EquipInfoUsageType _usageType;

        public EquipInfoAmountType ReloadSpeed { get { return _reloadSpeed; } set { _reloadSpeed = value; } }
        public EquipInfoAmountType EnergyUsage { get { return _energyUsage; } set { _energyUsage = value; } }
        public EquipInfoUsageType UsageType { get { return _usageType; } set { _usageType = value; } }
    }
}
