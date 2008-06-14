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
    public class Bullet : Gob
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

        #region ICollidable Members
        // Some members are implemented in class Gob.

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            base.Activate();

            // If we have a physical collision area, i.e. we want to bounce
            // off walls, but we are born inside a wall, we die immediately
            // lest we face the treacherous consistency manoeuver of flying
            // through it, void of even the most minute intention to perish.
            if (!physics.IsFreePosition(this, this.Pos))
                Die();
        }

        /// <summary>
        /// Performs collision operations for the case when one of this gob's collision areas
        /// is overlapping one of another gob's collision areas.
        /// </summary>
        /// <param name="myArea">The collision area of this gob.</param>
        /// <param name="theirArea">The collision area of the other gob.</param>
        /// <param name="backtrackFailed">If <b>true</b> then <b>theirArea.Type</b> matches 
        /// <b>myArea.CannotOverlap</b> and backtracking couldn't resolve this overlap. It is
        /// then up to this gob and the other gob to resolve the overlap.</param>
        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool backtrackFailed)
        {
            if (TypeName == "bouncebomb") // HACK until implemented class BounceBullet : Bullet
            {
                if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
                {
                    theirArea.Owner.InflictDamage(impactDamage);
                    Die();
                }
            }
            else
            {
                if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
                    theirArea.Owner.InflictDamage(impactDamage);
                if ((theirArea.Type & CollisionAreaType.PhysicalWall) != 0)
                    ((Wall)theirArea.Owner).MakeHole(Pos/* HACK: Where is Bullet.ImpactArea ? */);
                Die();
            }
        }

        #endregion
    }
}
