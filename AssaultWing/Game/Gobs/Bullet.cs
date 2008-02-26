using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A simple bullet.
    /// </summary>
    public class Bullet : Gob, IProjectile
    {
        /// <summary>
        /// Amount of damage to inflict on impact with a damageable gob.
        /// </summary>
        [TypeParameter]
        float impactDamage;

        /// <summary>
        /// The hole to make on impact to walls.
        /// </summary>
        [TypeParameter]
        Polygon impactArea;

        /// <summary>
        /// A list of alternative model names for a bullet.
        /// </summary>
        /// The actual model name for a bullet is chosen from these by random.
        [TypeParameter]
        string[] bulletModelNames;

        /// <summary>
        /// Names of all 3D models that this gob type will ever use.
        /// </summary>
        public override List<string> ModelNames
        {
            get
            {
                List<string> names = base.ModelNames;
                names.AddRange(bulletModelNames);
                return names;
            }
        }

        /// <summary>
        /// Creates an uninitialised bullet.
        /// </summary>
        /// This constructor is only for serialisation.
        public Bullet()
            : base()
        {
            this.impactDamage = 10;
            this.impactArea = new Polygon(new Vector2[] { 
                new Vector2(-5,-5),
                new Vector2(-5,5),
                new Vector2(7,0)});
            this.bulletModelNames = new string[] { "dummymodel", };
        }

        /// <summary>
        /// Creates a bullet.
        /// </summary>
        /// <param name="typeName">The type of the bullet.</param>
        public Bullet(string typeName)
            : base(typeName)
        {
            int modelNameI = RandomHelper.GetRandomInt(bulletModelNames.Length);
            base.ModelName = bulletModelNames[modelNameI];
            base.physicsApplyMode = PhysicsApplyMode.All | PhysicsApplyMode.ReceptorCollidesPhysically;
        }

        /// <summary>
        /// Updates the gob according to its natural behaviour.
        /// </summary>
        public override void Update()
        {
            base.Update();

            // Fly nose first.
            float rotationGoal = (float)Math.Acos(Move.X / Move.Length());
            if (Move.Y < 0)
                rotationGoal = MathHelper.TwoPi - rotationGoal;
            Rotation = rotationGoal;
        }

        #region ICollidable Members
        // Some members are implemented in class Gob.

        /// <summary>
        /// Performs collision operations with a gob whose general collision area
        /// has collided with one of our receptor areas.
        /// </summary>
        /// <param name="gob">The gob we collided with.</param>
        /// <param name="receptorName">The name of our colliding receptor area.</param>
        public override void Collide(ICollidable gob, string receptorName)
        {
            IDamageable damaGob = gob as IDamageable;
            if (damaGob != null)
                damaGob.InflictDamage(impactDamage);

            Die();

            // Fake safe position to make physical collisions happen.
            // We can do this only because we know we're dead already.
            HadSafePosition = true;
        }

        #endregion
        
        #region IProjectile Members

        /// <summary>
        /// The area the projectile destroys from thick gobs on impact.
        /// </summary>
        /// The area is translated according to the gob's location.
        public Polygon ImpactArea
        {
            get
            {
                return (Polygon)impactArea.Transform(WorldMatrix);
            }
        }

        #endregion
    }
}
