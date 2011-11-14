using System;
using AW2.Core;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.BonusActions
{
    public class Weapon2UpgradeBonusAction : Gobs.BonusAction
    {
        private static readonly CanonicalString DUMMY_ICON_NAME = (CanonicalString)"dummytexture";

        [TypeParameter]
        private CanonicalString _fixedWeaponName;
        [TypeParameter]
        private CanonicalString _effectName;

        private string _bonusText;
        private CanonicalString _bonusIconName;

        public override string BonusText
        {
            get
            {
                if (string.IsNullOrEmpty(_bonusText))
                    _bonusText = Owner.Ship == null ? "" : Owner.Ship.Weapon2Name;
                return _bonusText;
            }
        }

        public override CanonicalString BonusIconName
        {
            get
            {
                if (_bonusIconName.IsNull || _bonusIconName.Equals(DUMMY_ICON_NAME))
                    _bonusIconName = Owner.Ship == null ? DUMMY_ICON_NAME : Owner.Ship.Weapon2.IconName;
                return _bonusIconName;
            }
        }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Weapon2UpgradeBonusAction()
        {
            _fixedWeaponName = (CanonicalString)"";
            _effectName = (CanonicalString)"";
        }

        public Weapon2UpgradeBonusAction(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            base.Activate();
            UpgradeWeapon();
        }

        public override void Dispose()
        {
            if (Owner.Ship != null)
                Owner.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, Owner.Weapon2Name);
            if (_effectName != "") Owner.PostprocessEffectNames.Remove(_effectName);
            base.Dispose();
        }

        private void UpgradeWeapon()
        {
            if (Owner.Ship == null)
                Die();
            else
            {
                var upgradeName = _fixedWeaponName != "" ? _fixedWeaponName : Owner.Ship.Weapon2.UpgradeNames[0];
                Owner.Ship.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, upgradeName);
                if (_effectName != "") Owner.PostprocessEffectNames.EnsureContains(_effectName);
            }
        }
    }
}
