using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// A circle in two-dimensional space.
    /// </summary>
    public class Circle : IGeomPrimitive
    {
        Vector2 center;
        float radius;

        /// <summary>
        /// A rectangle containing the triangle.
        /// </summary>
        /// The Z-coordinate is irrelevant.
        BoundingBox boundingBox;

        /// <summary>
        /// Gets and sets the center of the circle.
        /// </summary>
        public Vector2 Center { get { return center; } set { center = value; } }

        /// <summary>
        /// Gets and sets the radius of the circle.
        /// </summary>
        public float Radius { get { return radius; } set { radius = MathHelper.Max(value, 0f); } }

        /// <summary>
        /// Creates a zero-radius circle at the origin.
        /// </summary>
        public Circle()
        {
            center = Vector2.Zero;
            radius = 0;
            boundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
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
            boundingBox = new BoundingBox(new Vector3(center.X - radius, center.Y - radius, 0),
                                          new Vector3(center.X + radius, center.Y + radius, 0));
        }

        #region IGeomPrimitive Members

        /// <summary>
        /// A rectangle that contains the geometric primitive.
        /// </summary>
        /// The Z-coordinates are irrelevant.
        public BoundingBox BoundingBox { get { return boundingBox; } }

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
    }
}
