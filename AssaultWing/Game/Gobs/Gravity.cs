using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A gravitational force, pulling other gobs.
    /// </summary>
    /// Gravity is tought of simply as a constant acceleration vector that is the same
    /// for everyone regardless of their location and mass.
    public class Gravity : Gob, IGravity
    {
        /// <summary>
        /// The gravitational acceleration vector of the gravitational pull, in m/s^2.
        /// </summary>
        [Helpers.RuntimeState]
        Vector2 force;

        /// <summary>
        /// Creates an uninitialised gravitational force.
        /// </summary>
        /// This constructor is only for serialisation.
        public Gravity() : base() 
        {
            force = new Vector2(0, -10);
            base.collisionAreas = new CollisionArea[] {
                new CollisionArea("Force", new Helpers.Circle(Vector2.Zero, 500f), this),
                new CollisionArea("Force", new Helpers.Polygon(new Vector2[] {
                    new Vector2(0,0),
                    new Vector2(500,0),
                    new Vector2(500,500), 
                    new Vector2(0,500),
                }), this),
                new CollisionArea("Force", new Helpers.Everything(), this),
            };
        }

        /// <summary>
        /// Creates a gravitational force.
        /// </summary>
        /// <param name="typeName">The type of the gravitational force.</param>
        public Gravity(string typeName)
            : base(typeName)
        {
            base.physicsApplyMode = PhysicsApplyMode.None;
        }

        /// <summary>
        /// Draws the gravitational force.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public override void Draw(Matrix view, Matrix projection)
        {
            // You must feel the force. You can't see it.
        }

        #region IGravity Members

        /// <summary>
        /// Returns the acceleration that the gravitational force applies to a mass
        /// located at the given position.
        /// </summary>
        /// <param name="pos">The position of the mass that is pulled by the force.</param>
        /// <returns>The acceleration created by the force.</returns>
        public Vector2 GetGravity(Vector2 pos)
        {
            return force;
        }

        #endregion

        #region ICollidable Members

        /// <summary>
        /// Performs collision operations with a gob whose general collision area
        /// has collided with one of our receptor areas.
        /// </summary>
        /// <param name="gob">The gob we collided with.</param>
        /// <param name="receptorName">The name of our colliding receptor area.</param>
        public override void Collide(ICollidable gob, string receptorName)
        {
            // We have only one receptor, "Gravity".
            //if (!receptorName.Equals("Gravity")) return;
            if (!(gob is ISolid && gob is Gob)) return;
            if (gob is IThick) return;
            Gob gobgob = (Gob)gob;
            if ((gobgob.PhysicsApplyMode & PhysicsApplyMode.Gravity) != 0)
                physics.ApplyForce(gobgob, force * gobgob.Mass);
        }

        #endregion // ICollidable Members
    }
}
