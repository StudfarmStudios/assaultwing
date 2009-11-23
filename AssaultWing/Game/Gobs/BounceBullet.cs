using AW2.Helpers;

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
        /// Creates an uninitialised bounce bullet.
        /// </summary>
        /// This constructor is only for serialisation.
        public BounceBullet()
            : base()
        {
        }

        /// <summary>
        /// Creates a bounce bullet.
        /// </summary>
        /// <param name="typeName">The type of the bounce bullet.</param>
        public BounceBullet(CanonicalString typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Performs collision operations for the case when one of this gob's collision areas
        /// is overlapping one of another gob's collision areas.
        /// </summary>
        /// <param name="myArea">The collision area of this gob.</param>
        /// <param name="theirArea">The collision area of the other gob.</param>
        /// <param name="stuck">If <b>true</b> then the gob is stuck, i.e.
        /// <b>theirArea.Type</b> matches <b>myArea.CannotOverlap</b> and it's not possible
        /// to backtrack out of the overlap. It is then up to this gob and the other gob 
        /// to resolve the overlap.</param>
        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
            {
                theirArea.Owner.InflictDamage(impactDamage, new DeathCause(theirArea.Owner, DeathCauseType.Damage, this));
                Die(new DeathCause());
            }
            else if (stuck)
                Die(new DeathCause());
        }
    }
}
