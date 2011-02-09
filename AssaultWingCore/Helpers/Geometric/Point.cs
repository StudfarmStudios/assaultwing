#if !DEBUG
#define TRUSTED_VISIBILITY_BREACH // makes code faster at the cost of naughty class design
#endif
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Helpers.Serialization;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// A point in two-dimensional space.
    /// </summary>
    public class Point : IGeomPrimitive
    {
#if TRUSTED_VISIBILITY_BREACH
        [SerializedName("location")]
        public Vector2 Location;
#else
        Vector2 location;

        /// <summary>
        /// Gets and sets the location of the point.
        /// </summary>
        public Vector2 Location { get { return location; } set { location = value; } }
#endif

        /// <summary>
        /// Creates a point at the origin.
        /// </summary>
        public Point()
        {
#if TRUSTED_VISIBILITY_BREACH
            Location = Vector2.Zero;
#else
            location = Vector2.Zero;
#endif
        }

        /// <summary>
        /// Creates an arbitrary point.
        /// </summary>
        /// <param name="location">The point's location.</param>
        public Point(Vector2 location)
        {
#if TRUSTED_VISIBILITY_BREACH
            Location = location;
#else
            this.location = location;
#endif
        }

        /// <summary>
        /// Returns true iff this and the given point are equal in the given error margin.
        /// </summary>
        /// <param name="point">The other point.</param>
        /// <param name="delta">The error margin.</param>
        /// <returns>True iff this and the given point are equal in the given error margin.</returns>
        public bool Equals(Point point, float delta)
        {
            return MathHelper.Distance(this.Location.X, point.Location.X) < delta
                && MathHelper.Distance(this.Location.Y, point.Location.Y) < delta;
        }

        /// <summary>
        /// Returns a string representation of the point.
        /// </summary>
        public override string ToString()
        {
            return Location.ToString();
        }

        #region IGeomPrimitive Members

        /// <summary>
        /// A rectangle that contains the geometric primitive.
        /// </summary>
        public Rectangle BoundingBox
        {
#if TRUSTED_VISIBILITY_BREACH
            get { return new Rectangle(Location, Location); } 
#else
            get { return new Rectangle(location, location); }
#endif
        }

        /// <summary>
        /// Transforms the geometric primitive by a transformation matrix.
        /// </summary>
        /// <param name="transformation">The transformation matrix.</param>
        /// <returns>The transformed geometric primitive.</returns>
        public IGeomPrimitive Transform(Matrix transformation)
        {
#if TRUSTED_VISIBILITY_BREACH
            return new Point(Vector2.Transform(Location, transformation));
#else
            return new Point(Vector2.Transform(location, transformation));
#endif
        }

        /// <summary>
        /// Returns the shortest distance between the geometric primitive
        /// and a point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>The shortest distance between the geometric primitive
        /// and the point.</returns>
        public float DistanceTo(Vector2 point)
        {
#if TRUSTED_VISIBILITY_BREACH
            return Vector2.Distance(Location, point);
#else
            return Vector2.Distance(location, point);
#endif
        }

        #endregion IGeomPrimitive Members

        #region INetworkSerializable Members

        /// <summary>
        /// Serialises the object to a binary writer.
        /// </summary>
        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
#if TRUSTED_VISIBILITY_BREACH
                var location = Location;
#endif
                writer.Write((float)location.X);
                writer.Write((float)location.Y);
            }
        }

        /// <summary>
        /// Deserialises the object from a binary writer.
        /// </summary>
        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
#if TRUSTED_VISIBILITY_BREACH
                var
#endif
                location = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
#if TRUSTED_VISIBILITY_BREACH
                Location = location;
#endif
            }
        }

        #endregion
    }
}
