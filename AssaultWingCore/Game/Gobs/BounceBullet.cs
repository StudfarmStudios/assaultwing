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
    public class BounceBullet : Bullet
    {
        private int _deathBySlownessCounter;

        /// <summary>
        /// Only for serialisation.
        /// </summary>
        public BounceBullet()
        {
        }

        public BounceBullet(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Update()
        {
            base.Update();
            if (Move.LengthSquared() < 1 * 1)
                _deathBySlownessCounter++;
            else
                _deathBySlownessCounter = 0;
            if (_deathBySlownessCounter > 3) Die();
        }

        public override bool CollideIrreversible(CollisionArea myArea, CollisionArea theirArea)
        {
            var collidedWithPhysical = (theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0;
            return !collidedWithPhysical ? false : base.CollideIrreversible(myArea, theirArea);
        }
    }
}
