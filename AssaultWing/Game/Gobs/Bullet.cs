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
        /// Names of all 3D models that this gob type will ever use.
        /// </summary>
        public override IEnumerable<CanonicalString> ModelNames {
            get { return base.ModelNames.Union(bulletModelNames); }
        }

        /// <summary>
        /// Creates an uninitialised bullet.
        /// </summary>
        /// This constructor is only for serialisation.
        public Bullet()
            : base()
        {
            impactDamage = 10;
            impactHoleRadius = 10;
            bulletModelNames = new CanonicalString[] { (CanonicalString)"dummymodel" };
        }

        /// <summary>
        /// Creates a bullet.
        /// </summary>
        /// <param name="typeName">The type of the bullet.</param>
        public Bullet(string typeName)
            : base(typeName)
        {
            if (bulletModelNames.Length > 0)
            {
                int modelNameI = RandomHelper.GetRandomInt(bulletModelNames.Length);
                base.ModelName = bulletModelNames[modelNameI];
            }
        }

        /// <summary>
        /// Updates the gob according to its natural behaviour.
        /// </summary>
        public override void Update()
        {
            base.Update();

            // Fly nose first, but only if we're moving fast enough.
            if (move.LengthSquared() > 1 * 1)
            {
                float rotationGoal = (float)Math.Acos(Move.X / Move.Length());
                if (Move.Y < 0)
                    rotationGoal = MathHelper.TwoPi - rotationGoal;
                Rotation = rotationGoal;
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

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public override void MakeConsistent(Type limitationAttribute)
        {
            base.MakeConsistent(limitationAttribute);
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                // Make sure there's no null references.
                if (bulletModelNames == null)
                    bulletModelNames = new CanonicalString[0];
            }
        }

        #endregion IConsistencyCheckable Members
    }
}
