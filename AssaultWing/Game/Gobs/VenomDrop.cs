using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A drop of venom. Has a limited lifetime and clings to target on collision.
    /// </summary>
    class VenomDrop : Bullet
    {
        /// <summary>
        /// How much damage to do in a second when clung to a target.
        /// </summary>
        [TypeParameter]
        float clingDamagePerSecond;

        /// <summary>
        /// How many seconds the drop clings before it dies.
        /// </summary>
        [TypeParameter]
        float clingTime;

        Gob clungTo;

        /// This constructor is only for serialisation.
        public VenomDrop()
            : base()
        {
            clingDamagePerSecond = 10;
            clingTime = 5;
        }

        /// <param name="typeName">The type of the venom drop.</param>
        public VenomDrop(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Update()
        {
            base.Update();
            if (clungTo != null)
            {
                if (clungTo.Dead) Die(new DeathCause());
                float seconds = (float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds;
                float damage = clingDamagePerSecond * seconds;
                clungTo.InflictDamage(damage, new DeathCause(clungTo, DeathCauseType.Damage));
            }
        }

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
            {
                theirArea.Owner.InflictDamage(impactDamage, new DeathCause(theirArea.Owner, DeathCauseType.Damage, this));
                clungTo = theirArea.Owner;
                IsVisible = false;
                movable = false;
                RemoveCollisionAreas(area => true);
                DeathTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(clingTime);
            }
            else
                Die(new DeathCause());
        }
    }
}
