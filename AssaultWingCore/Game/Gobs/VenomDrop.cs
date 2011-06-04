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

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck, Arena.CollisionSideEffectType sideEffectTypes)
        {
            var collidedWithPhysical = (theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0;
            if ((sideEffectTypes & AW2.Game.Arena.CollisionSideEffectType.Reversible) != 0)
            {
                if (collidedWithPhysical) theirArea.Owner.InflictDamage(_impactDamage, new DamageInfo(this));
            }
            if ((sideEffectTypes & AW2.Game.Arena.CollisionSideEffectType.Irreversible) != 0)
            {
                if (collidedWithPhysical) DoDamageOverTime(theirArea.Owner);
                Die();
            }
        }

        private void DoDamageOverTime(Gob target)
        {
            var player = target.Owner;
            if (!(target is Ship) || player == null) return; // TODO: damage over time for all gobs
            BonusAction.Create<DamageBuffBonusAction>(_damageEffectTypeName, player, gob => gob.Cause = this);
        }
    }
}
