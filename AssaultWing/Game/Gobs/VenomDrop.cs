using System;
using AW2.Game.BonusActions;
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
        private CanonicalString _damageOverTimeBonusIconName;

        [TypeParameter]
        private float _clingDamagePerSecond;

        [TypeParameter]
        private float _clingTime;

        /// This constructor is only for serialisation.
        public VenomDrop()
        {
            _damageOverTimeBonusIconName = (CanonicalString)"dummytexture";
            _clingDamagePerSecond = 10;
            _clingTime = 5;
        }

        /// <param name="typeName">The type of the venom drop.</param>
        public VenomDrop(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
            {
                theirArea.Owner.InflictDamage(_impactDamage, new DeathCause(theirArea.Owner, this));
                DoDamageOverTime(theirArea.Owner);
            }
            Die();
        }

        private void DoDamageOverTime(Gob target)
        {
            var player = target.Owner;
            if (!(target is Ship) || player == null) return; // TODO: damage over time for all gobs
            var dot = new DamageBuffBonusAction(TypeName, _damageOverTimeBonusIconName, _clingDamagePerSecond);
            dot.Player = player;
            dot.SetDuration(_clingTime);
            if (dot.DoAction()) player.BonusActions.AddOrReplace(dot);
        }
    }
}
