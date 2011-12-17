using System;
using System.Linq;
using AW2.Game.BonusActions;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A drop of venom. Has a limited lifetime and causes damage over time on collision.
    /// </summary>
    public class VenomDrop : Bullet
    {
        [TypeParameter]
        private CanonicalString _damageEffectTypeName;

        /// This constructor is only for serialisation.
        public VenomDrop()
        {
            _damageEffectTypeName = (CanonicalString)"dummygob";
        }

        public VenomDrop(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override bool CollideIrreversible(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
            {
                theirArea.Owner.InflictDamage(_impactDamage, new DamageInfo(this));
                DoDamageOverTime(theirArea.Owner);
                Game.Stats.SendHit(this, theirArea.Owner);
            }
            Die();
            return true;
        }

        private void DoDamageOverTime(Gob target)
        {
            var action = BonusAction.Create<DamageBuffBonusAction>(_damageEffectTypeName, target, gob => { });
            if (action != null) action.Owner = Owner;
        }
    }
}
