using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A simple bullet.
    /// </summary>
    public class Bullet : Gob
    {
        /// <summary>
        /// Amount of damage to inflict on impact with a damageable gob.
        /// </summary>
        [TypeParameter]
        protected float impactDamage;

        /// <summary>
        /// The radius of the hole to make on impact to walls.
        /// </summary>
        [TypeParameter]
        float impactHoleRadius;

        /// <summary>
        /// A list of alternative model names for a bullet.
        /// </summary>
        /// The actual model name for a bullet is chosen from these by random.
        [TypeParameter, ShallowCopy]
        CanonicalString[] bulletModelNames;

        /// <summary>
        /// Lifetime of the bounce bullet, in game time seconds.
        /// Death is inevitable when lifetime has passed.
        /// </summary>
        [TypeParameter]
        float lifetime;

        /// <summary>
        /// If true, the bullet rotates by <see cref="rotationSpeed"/>.
        /// </summary>
        [TypeParameter]
        bool isRotating;

        /// <summary>
        /// Rotation speed in radians per second. Has an effect only when <see cref="isRotating"/> is true.
        /// </summary>
        [TypeParameter]
        float rotationSpeed;

        /// <summary>
        /// Time of certain death of the bullet, in game time.
        /// </summary>
        protected TimeSpan DeathTime { get; set; }

        /// <summary>
        /// Names of all 3D models that this gob type will ever use.
        /// </summary>
        public override IEnumerable<CanonicalString> ModelNames
        {
            get { return base.ModelNames.Union(bulletModelNames); }
        }

        /// This constructor is only for serialisation.
        public Bullet()
            : base()
        {
            impactDamage = 10;
            impactHoleRadius = 10;
            bulletModelNames = new CanonicalString[] { (CanonicalString)"dummymodel" };
            lifetime = 60;
            isRotating = false;
            rotationSpeed = 5;
            DeathTime = new TimeSpan(0, 1, 2);
        }

        public Bullet(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            DeathTime = Arena.TotalTime + TimeSpan.FromSeconds(lifetime);
            if (bulletModelNames.Length > 0)
            {
                int modelNameI = RandomHelper.GetRandomInt(bulletModelNames.Length);
                base.ModelName = bulletModelNames[modelNameI];
            }
            base.Activate();
        }

        public override void Update()
        {
            if (Arena.TotalTime >= DeathTime)
                Die(new DeathCause());
            
            base.Update();
            if (isRotating)
            {
                Rotation += rotationSpeed * (float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            }
            else
            {
                // Fly nose first, but only if we're moving fast enough.
                if (move.LengthSquared() > 1 * 1) {
                    float rotationGoal = (float)Math.Acos( Move.X / Move.Length() );
                    if (Move.Y < 0)
                        rotationGoal = MathHelper.TwoPi - rotationGoal;
                    Rotation = rotationGoal;
                }
            }
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
                theirArea.Owner.InflictDamage(impactDamage, new DeathCause(theirArea.Owner, DeathCauseType.Damage, this));
            Arena.MakeHole(Pos, impactHoleRadius);
            Die(new DeathCause());
        }
    }
}
