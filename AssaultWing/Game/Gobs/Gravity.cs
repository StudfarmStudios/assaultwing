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
        /// Bounding volume of the 3D visuals of the gob, in world coordinates.
        /// </summary>
        public override BoundingSphere DrawBounds { get { return new BoundingSphere(); } }

        /// <summary>
        /// Creates an uninitialised gravitational force.
        /// </summary>
        /// This constructor is only for serialisation.
        public Gravity() : base() 
        {
            force = new Vector2(0, -10);
            base.collisionAreas = new CollisionArea[0];
        }

        /// <summary>
        /// Creates a gravitational force.
        /// </summary>
        /// <param name="typeName">The type of the gravitational force.</param>
        public Gravity(AW2.Helpers.CanonicalString typeName)
            : base(typeName)
        {
            movable = false;
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
            AssaultWing.Instance.PhysicsEngine.ApplyForce(theirArea.Owner, force * theirArea.Owner.Mass);
        }

        #region Methods related to serialisation

        /// <summary>
        /// Serialises the gob for to a binary writer.
        /// </summary>
        /// <param name="writer">The writer where to write the serialised data.</param>
        /// <param name="mode">Which parts of the gob to serialise.</param>
        public override void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((float)force.X);
                writer.Write((float)force.Y);
            }
        }

        /// <summary>
        /// Deserialises the gob from a binary writer.
        /// </summary>
        /// <param name="reader">The reader where to read the serialised data.</param>
        /// <param name="mode">Which parts of the gob to deserialise.</param>
        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode)
        {
            base.Deserialize(reader, mode);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                Vector2 newForce = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
                force = newForce;
            }
        }

        #endregion Methods related to serialisation
    }
}
