using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Net;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// A circle in two-dimensional space.
    /// </summary>
    public class Circle : IGeomPrimitive, IConsistencyCheckable
    {
        Vector2 center;
        float radius;

        /// <summary>
        /// A rectangle containing the triangle.
        /// </summary>
        Rectangle boundingBox;

        /// <summary>
        /// Gets and sets the center of the circle.
        /// </summary>
        public Vector2 Center { get { return center; } }

        /// <summary>
        /// Gets and sets the radius of the circle.
        /// </summary>
        public float Radius { get { return radius; } }

        /// <summary>
        /// Creates a zero-radius circle at the origin.
        /// </summary>
        public Circle()
        {
            center = Vector2.Zero;
            radius = 0;
            UpdateBoundingBox();
        }

        /// <summary>
        /// Creates an arbitrary circle.
        /// </summary>
        /// <param name="center">The circle's center.</param>
        /// <param name="radius">The circle's radius.</param>
        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
            UpdateBoundingBox();
        }

        void UpdateBoundingBox()
        {
            boundingBox = new Rectangle(center.X - radius, center.Y - radius,
                                        center.X + radius, center.Y + radius);
        }

        #region IGeomPrimitive Members

        /// <summary>
        /// A rectangle that contains the geometric primitive.
        /// </summary>
        /// The Z-coordinates are irrelevant.
        public Rectangle BoundingBox { get { return boundingBox; } }

        /// <summary>
        /// Transforms the geometric primitive by a transformation matrix.
        /// </summary>
        /// <param name="transformation">The transformation matrix.</param>
        /// <returns>The transformed geometric primitive.</returns>
        /// If the transformation scales X and Y axes differently, the result
        /// is undefined.
        public IGeomPrimitive Transform(Matrix transformation)
        {
            Vector2 newCenter = Vector2.Transform(center, transformation);
            Vector2 newRadiusVector = Vector2.TransformNormal(radius * Vector2.UnitX, transformation);
            return new Circle(newCenter, newRadiusVector.Length());
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
            float distance = Vector2.Distance(this.center, point) - this.radius;
            return Math.Max(distance, 0);
        }

        #endregion IGeomPrimitive Members

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        public void MakeConsistent(Type limitationAttribute)
        {
            UpdateBoundingBox();
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
                writer.Write((float)center.X);
                writer.Write((float)center.Y);
                writer.Write((float)radius);
            }
        }

        /// <summary>
        /// Deserialises the object from a binary writer.
        /// </summary>
        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                center = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
                radius = reader.ReadSingle();
            }
        }

        #endregion
    }
}
