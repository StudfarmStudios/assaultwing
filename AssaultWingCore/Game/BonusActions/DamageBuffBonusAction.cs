using System;
using AW2.Core;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.BonusActions
{
    public class DamageBuffBonusAction : Gobs.BonusAction
    {
        [TypeParameter]
        private string _buffName; // not CanonicalString because this doesn't contain any "well known string" such as a content texture name

        [TypeParameter]
        private CanonicalString _bonusIconName;

        [TypeParameter]
        private float _damagePerSecond;

        public override string BonusText { get { return _buffName; } }
        public override CanonicalString BonusIconName { get { return _bonusIconName; } }
        public Gob Cause { get; set; }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public DamageBuffBonusAction()
        {
            _buffName = "dummy damage buff";
            _bonusIconName = (CanonicalString)"dummytexture";
            _damagePerSecond = 500;
        }

        public DamageBuffBonusAction(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Update()
        {
            base.Update();
            float damage = Game.PhysicsEngine.ApplyChange(_damagePerSecond, Game.GameTime.ElapsedGameTime);
            if (Owner.Ship != null)
            {
                if (damage > 0)
                    Owner.Ship.InflictDamage(damage, new DamageInfo(Cause));
                else
                    Owner.Ship.RepairDamage(-damage);
            }
        }
    }
}
