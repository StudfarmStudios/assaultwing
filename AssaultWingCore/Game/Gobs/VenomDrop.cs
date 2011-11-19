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

        public override Arena.CollisionSideEffectType Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck, Arena.CollisionSideEffectType sideEffectTypes)
        {
            var result = Arena.CollisionSideEffectType.None;
            var collidedWithPhysical = (theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0;
            if ((sideEffectTypes & AW2.Game.Arena.CollisionSideEffectType.Reversible) != 0)
            {
                if (collidedWithPhysical)
                {
                    theirArea.Owner.InflictDamage(_impactDamage, new DamageInfo(this));
                    result |= Arena.CollisionSideEffectType.Reversible;
                }
            }
            if ((sideEffectTypes & AW2.Game.Arena.CollisionSideEffectType.Irreversible) != 0)
            {
                if (collidedWithPhysical) DoDamageOverTime(theirArea.Owner);
                Die();
                result |= Arena.CollisionSideEffectType.Irreversible;
            }
            return result;
        }

        private void DoDamageOverTime(Gob target)
        {
            var action = BonusAction.Create<DamageBuffBonusAction>(_damageEffectTypeName, target, gob => { });
            if (action != null) action.Owner = Owner;
        }
    }
}
