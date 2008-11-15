using System;
using System.Collections.Generic;
using System.Text;
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
        /// Lifetime of the bounce bullet, in game time seconds.
        /// Death is inevitable when lifetime has passed.
        /// </summary>
        [TypeParameter]
        float lifetime;

        /// <summary>
        /// Time of certain death of the bounce bullet, in game time.
        /// </summary>
        [RuntimeState]
        TimeSpan deathTime;

        /// <summary>
        /// Creates an uninitialised bounce bullet.
        /// </summary>
        /// This constructor is only for serialisation.
        public BounceBullet()
            : base()
        {
            lifetime = 5;
            deathTime = new TimeSpan(0, 1, 2);
        }

        /// <summary>
        /// Creates a bounce bullet.
        /// </summary>
        /// <param name="typeName">The type of the bounce bullet.</param>
        public BounceBullet(string typeName)
            : base(typeName)
        {
            deathTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(lifetime);
        }

        /// <summary>
        /// Updates the gob according to physical laws.
        /// </summary>
        /// Overriden Update methods should explicitly call this method in order to have 
        /// physical laws apply to the gob and the gob's exhaust engines updated.
        public override void Update()
        {
            if (AssaultWing.Instance.GameTime.TotalGameTime >= deathTime)
                Die(new DeathCause());
            base.Update();
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
