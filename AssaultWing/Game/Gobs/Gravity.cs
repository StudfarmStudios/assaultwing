using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using AW2.Helpers.Geometric;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A gravitational force, pulling other gobs.
    /// </summary>
    /// Gravity is tought of simply as a constant acceleration vector that is the same
    /// for everyone regardless of their location and mass.
    public class Gravity : Gob
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
                new CollisionArea("Force", new Circle(Vector2.Zero, 500f), this, 
                CollisionAreaType.Receptor, CollisionAreaType.PhysicalMovable, CollisionAreaType.None),
                new CollisionArea("Force", new Polygon(new Vector2[] {
                    new Vector2(0,0),
                    new Vector2(500,0),
                    new Vector2(500,500), 
                    new Vector2(0,500),
                }), this, 
                CollisionAreaType.Receptor, CollisionAreaType.PhysicalMovable, CollisionAreaType.None),
                new CollisionArea("Force", new Everything(), this, 
                CollisionAreaType.Receptor, CollisionAreaType.PhysicalMovable, CollisionAreaType.None),
            };
        }

        /// <summary>
        /// Creates a gravitational force.
        /// </summary>
        /// <param name="typeName">The type of the gravitational force.</param>
        public Gravity(string typeName)
            : base(typeName)
        {
            movable = false;
        }

        /// <summary>
        /// Draws the gob's 3D graphics.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public override void Draw(Matrix view, Matrix projection)
        {
            // You must feel the force. You can't see it.
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
            // We assume that we have only one collision area which collides with movables.
            physics.ApplyForce(theirArea.Owner, force * theirArea.Owner.Mass);
        }
    }
}
