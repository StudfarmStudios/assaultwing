using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A bullet that bounces off walls and hits only damageable gobs.
    /// Bounce bullet dies by timer, if not sooner.
    /// </summary>
    [LimitedSerialization]
    class BounceBullet : Bullet
    {
        /// <summary>
        /// Only for serialisation.
        /// </summary>
        public BounceBullet()
            : base()
        {
        }

        public BounceBullet(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
            {
                theirArea.Owner.InflictDamage(_impactDamage, new DamageInfo(this));
                Die();
            }
            else if (stuck)
                Die();
        }
    }
}
