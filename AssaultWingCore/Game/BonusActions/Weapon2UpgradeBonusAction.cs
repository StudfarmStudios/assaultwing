using System;
using AW2.Core;
using AW2.Game.Gobs;
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

        public new Ship Host { get { return base.Host as Ship; } }

        public override string BonusText
        {
            get
            {
                if (string.IsNullOrEmpty(_bonusText))
                    _bonusText = Host == null ? "" : Host.Weapon2Name;
                return _bonusText;
            }
        }

        public override CanonicalString BonusIconName
        {
            get
            {
                if (_bonusIconName.IsNull || _bonusIconName.Equals(DUMMY_ICON_NAME))
                    _bonusIconName = Host == null ? DUMMY_ICON_NAME : Host.Weapon2.IconName;
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
            if (Host != null && Host.Owner != null && Host.Weapon2 != null)
            {
                // FIXME: A client might never run this. A better countermeasure is to delay activation until Host != null etc.
                // Totally skipping activation like this may result in a client seeing he has a "rockets" upgrade but shooting berserkers.
                var weapon2 = (Weapon)Host.Weapon2;
                var upgradeName = _fixedWeaponName != "" ? _fixedWeaponName
                    : weapon2.UpgradeNames.Length > 0 ? weapon2.UpgradeNames[0]
                    : CanonicalString.Null;
                if (upgradeName != CanonicalString.Null)
                {
                    Host.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, upgradeName);
                    if (_effectName != "") Host.Owner.PostprocessEffectNames.EnsureContains(_effectName);
                }
            }
        }

        public override void Dispose()
        {
            if (Host != null && Host.Owner != null)
            {
                Host.SetDeviceType(Weapon.OwnerHandleType.SecondaryWeapon, Host.Owner.Weapon2Name);
                if (_effectName != "") Host.Owner.PostprocessEffectNames.Remove(_effectName);
            }
            base.Dispose();
        }
    }
}
