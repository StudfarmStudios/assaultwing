using System;
using AW2.Helpers.Serialization;

namespace AW2.Game.GobUtils
{
    public class ShipInfo : EquipInfo
    {
        [TypeParameter]
        private EquipInfoAmountType _hull;
        
        [TypeParameter]
        private EquipInfoAmountType _armor;

        [TypeParameter]
        private EquipInfoAmountType _speed;

        [TypeParameter]
        private EquipInfoAmountType _steering;

        [TypeParameter]
        private EquipInfoAmountType _modEnergy;

        [TypeParameter]
        private EquipInfoAmountType _specialEnergy;

        public EquipInfoAmountType Armor { get { return _armor; } set { _armor = value; } }
        public EquipInfoAmountType Hull { get { return _hull; } set { _hull = value; } }
        public EquipInfoAmountType Speed { get { return _speed; } set { _speed = value; } }
        public EquipInfoAmountType Steering { get { return _steering; } set { _steering = value; } }
        public EquipInfoAmountType ModEnergy { get { return _modEnergy; } set { _modEnergy = value; } }
        public EquipInfoAmountType SpecialEnergy { get { return _specialEnergy; } set { _specialEnergy = value; } }
    }
}
