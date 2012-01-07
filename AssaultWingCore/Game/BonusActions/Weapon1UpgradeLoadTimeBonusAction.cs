using System;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.BonusActions
{
    public class Weapon1UpgradeLoadTimeBonusAction : Gobs.BonusAction
    {
        [TypeParameter]
        private float _loadTimeMultiplier;

        public override string BonusText { get { return (Host.Owner != null ? Host.Owner.Weapon1Name + "\n" : "") + "speedloader"; } }
        public override CanonicalString BonusIconName { get { return (CanonicalString)"b_icon_rapid_fire_1"; } }
        public new Ship Host { get { return base.Host as Ship; } }

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
            if (Host != null && Host.Owner != null) Host.Weapon1.LoadTimeMultiplier *= _loadTimeMultiplier;
        }

        public override void Dispose()
        {
            if (Host != null && Host.Owner != null) Host.Weapon1.LoadTimeMultiplier = 1;
            base.Dispose();
        }
    }
}
