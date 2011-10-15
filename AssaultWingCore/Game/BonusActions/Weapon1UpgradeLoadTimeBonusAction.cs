using System;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.BonusActions
{
    public class Weapon1UpgradeLoadTimeBonusAction : Gobs.BonusAction
    {
        [TypeParameter]
        private float _loadTimeMultiplier;

        public override string BonusText { get { return Owner.Weapon1Name + "\nspeedloader"; } }
        public override CanonicalString BonusIconName { get { return (CanonicalString)"b_icon_rapid_fire_1"; } }
        public new Player Owner { get { return (Player)base.Owner; } }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public Weapon1UpgradeLoadTimeBonusAction()
        {
            _loadTimeMultiplier = 0.3f;
        }

        public Weapon1UpgradeLoadTimeBonusAction(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            base.Activate();
            if (Owner.Ship != null) Owner.Ship.Weapon1.LoadTimeMultiplier *= _loadTimeMultiplier;
        }

        public override void Dispose()
        {
            if (Owner.Ship != null)
                Owner.Ship.Weapon1.LoadTimeMultiplier = 1;
            base.Dispose();
        }
    }
}
