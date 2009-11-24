#if !DEBUG
#define TRUSTED_VISIBILITY_BREACH // makes code faster at the cost of naughty class design
#endif
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Net;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// An axis-aligned rectangle in two-dimensional space.
    /// </summary>
    public class Rectangle : IGeomPrimitive
    {
#if TRUSTED_VISIBILITY_BREACH
        [SerializedName("min")]
        public Vector2 Min;
        [SerializedName("max")]
        public Vector2 Max;
#else
        Vector2 min, max;
#endif

#if !TRUSTED_VISIBILITY_BREACH
        /// <summary>
        /// The bottom left corner of the rectangle.
        /// </summary>
        public Vector2 Min { get { return min; } }

        /// <summary>
        /// The top right corner of the rectangle.
        /// </summary>
        public Vector2 Max { get { return max; } }
#endif

        /// <summary>
        /// The width and height of the rectangle.
        /// </summary>
        public Vector2 Dimensions
        {
            get
            {
#if TRUSTED_VISIBILITY_BREACH
                return Max - Min;
#else
                return max - min;
#endif
            }
        }

        /// <summary>
        /// Creates a zero-sized rectangle at the origin.
        /// </summary>
        public Rectangle()
        {
        }

        /// <summary>
        /// Creates a rectangle.
        /// </summary>
        /// <param name="min">The bottom left corner of the rectangle.</param>
        /// <param name="max">The top right corner of the rectangle.</param>
        public Rectangle(Vector2 min, Vector2 max)
        {
            if (min.X > max.X || min.Y > max.Y)
                throw new ArgumentException("Min is not less than or equal to max");
#if TRUSTED_VISIBILITY_BREACH
            Min = min;
            Max = max;
#else
            this.min = min;
            this.max = max;
#endif
        }

        /// <summary>
        /// Creates a rectangle.
        /// </summary>
        /// <param name="minX">The left coordinate of the rectangle.</param>
        /// <param name="minY">The bottom coordinate of the rectangle.</param>
        /// <param name="maxX">The right coordinate of the rectangle.</param>
        /// <param name="maxY">The top coordinate of the rectangle.</param>
        public Rectangle(float minX, float minY, float maxX, float maxY)
        {
            if (minX > maxX || minY > maxY)
                throw new ArgumentException("Min is not less than or equal to max");
#if TRUSTED_VISIBILITY_BREACH
            Min = new Vector2(minX, minY);
            Max = new Vector2(maxX, maxY);
#else
            min = new Vector2(minX, minY);
            max = new Vector2(maxX, maxY);
#endif
        }

        /// <summary>
        /// Returns a string representation of the rectangle.
        /// </summary>
        public override string ToString()
        {
            return "{" + Min + " - " + Max + "}";
        }

        #region IGeomPrimitive Members

        /// <summary>
        /// A rectangle that contains the geometric primitive.
        /// </summary>
        public Rectangle BoundingBox { get { return this; } }

        /// <summary>
        /// Transforms the geometric primitive by a transformation matrix.
        /// The result is a polygon.
        /// </summary>
        /// <param name="transformation">The transformation matrix.</param>
        /// <returns>The transformed geometric primitive.</returns>
        /// If the transformation rotates around the Z axis, the result
        /// is undefined.
        public IGeomPrimitive Transform(Matrix transformation)
        {
#if TRUSTED_VISIBILITY_BREACH
            var min = Min;
            var max = Max;
#endif
            var p1 = Vector2.Transform(min, transformation);
            var p2 = Vector2.Transform(new Vector2(min.X, max.Y), transformation);
            var p3 = Vector2.Transform(max, transformation);
            var p4 = Vector2.Transform(new Vector2(max.X, min.Y), transformation);
            return new Polygon(new Vector2[] { p1, p2, p3, p4 });
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
            return Geometry.Distance(new Point(point), this);
        }

        #endregion

        #region INetworkSerializable Members

        /// <summary>
        /// Serialises the object to a binary writer.
        /// </summary>
        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
#if TRUSTED_VISIBILITY_BREACH
                var min = Min;
                var max = Max;
#endif
                writer.Write((float)min.X);
                writer.Write((float)min.Y);
                writer.Write((float)max.X);
                writer.Write((float)max.Y);
            }
        }

        /// <summary>
        /// Deserialises the object from a binary writer.
        /// </summary>
        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, TimeSpan messageAge)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
#if TRUSTED_VISIBILITY_BREACH
                Vector2 min, max;
#endif
                min = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
                max = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
#if TRUSTED_VISIBILITY_BREACH
                Min = min;
                Max = max;
#endif
            }
        }

        #endregion
    }
}
