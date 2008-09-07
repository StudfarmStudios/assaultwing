#if !DEBUG
#define TRUSTED_VISIBILITY_BREACH // makes code faster at the cost of naughty class design
#endif
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// A triangle in two-dimensional space. The triangle is formed by three
    /// corner points that are ordered in clockwise order.
    /// A triangle can also be degenerate, i.e. all the corner points
    /// lie on the same line.
    /// </summary>
    public class Triangle : IGeomPrimitive
    {
#if TRUSTED_VISIBILITY_BREACH
        [SerializedName("p1")]
        public Vector2 P1;
        [SerializedName("p2")]
        public Vector2 P2;
        [SerializedName("p3")]
        public Vector2 P3;
#else
        Vector2 p1, p2, p3;
#endif
        Vector2 n12, n13, n23;

        /// <summary>
        /// A rectangle containing the triangle.
        /// </summary>
        Rectangle boundingBox;

#if !TRUSTED_VISIBILITY_BREACH
        /// <summary>
        /// The first corner point.
        /// </summary>
        public Vector2 P1 { get { return p1; } }

        /// <summary>
        /// The second corner point.
        /// </summary>
        public Vector2 P2 { get { return p2; } }

        /// <summary>
        /// The third corner point.
        /// </summary>
        public Vector2 P3 { get { return p3; } }
#endif

        /// <summary>
        /// The unit normal pointing away from the triangle at the edge
        /// defined by P1 and P2.
        /// </summary>
        public Vector2 Normal12 { get { return n12; } }

        /// <summary>
        /// The unit normal pointing away from the triangle at the edge
        /// defined by P1 and P3.
        /// </summary>
        public Vector2 Normal13 { get { return n13; } }

        /// <summary>
        /// The unit normal pointing away from the triangle at the edge
        /// defined by P2 and P3.
        /// </summary>
        public Vector2 Normal23 { get { return n23; } }

        /// <summary>
        /// Creates a triangle. The corner points will be reordered
        /// to clockwise order.
        /// </summary>
        /// <param name="p1">The first corner point.</param>
        /// <param name="p2">The second corner point.</param>
        /// <param name="p3">The third corner point.</param>
        public Triangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            boundingBox = new Geometric.Rectangle(
                Math.Min(p1.X, Math.Min(p2.X, p3.X)),
                Math.Min(p1.Y, Math.Min(p2.Y, p3.Y)),
                Math.Max(p1.X, Math.Max(p2.X, p3.X)),
                Math.Max(p1.Y, Math.Max(p2.Y, p3.Y)));
            
            // Assign p1 as given, but possibly swap p2 and p3 to enforce
            // clockwise order of corner points.
            Vector2 e12LeftNormal = new Vector2(p1.Y - p2.Y, p2.X - p1.X);
            Vector2 e23 = p3 - p2;
            float dot = Vector2.Dot(e12LeftNormal, e23);
            if (dot > 0)
            {
#if TRUSTED_VISIBILITY_BREACH
                P1 = p1;
                P2 = p3;
                P3 = p2;
#else
                this.p1 = p1;
                this.p2 = p3;
                this.p3 = p2;
#endif
                n13 = -e12LeftNormal;
                n12 = new Vector2(p1.Y - p3.Y, p3.X - p1.X);
                n23 = new Vector2(p3.Y - p2.Y, p2.X - p3.X);
            }
            else
            {
#if TRUSTED_VISIBILITY_BREACH
                P1 = p1;
                P2 = p2;
                P3 = p3;
#else
                this.p1 = p1;
                this.p2 = p2;
                this.p3 = p3;
#endif
                n12 = e12LeftNormal;
                n13 = new Vector2(p3.Y - p1.Y, p1.X - p3.X);
                n23 = new Vector2(p2.Y - p3.Y, p3.X - p2.X);
            }
            n12.Normalize();
            n13.Normalize();
            n23.Normalize();
        }

        /// <summary>
        /// Returns a string representation of the triangle.
        /// </summary>
        public override string ToString()
        {
            return "{" + P1 + ", " + P2 + ", " + P3 + "}";
        }

        #region IGeomPrimitive Members

        /// <summary>
        /// A rectangle that contains the geometric primitive.
        /// </summary>
        public Rectangle BoundingBox { get { return boundingBox; } }

        /// <summary>
        /// Transforms the geometric primitive by a transformation matrix.
        /// </summary>
        /// <param name="transformation">The transformation matrix.</param>
        /// <returns>The transformed geometric primitive.</returns>
        public IGeomPrimitive Transform(Matrix transformation)
        {
#if TRUSTED_VISIBILITY_BREACH
            return new Triangle(Vector2.Transform(P1, transformation),
                Vector2.Transform(P2, transformation),
                Vector2.Transform(P3, transformation));
#else
            return new Triangle(Vector2.Transform(p1, transformation),
                Vector2.Transform(p2, transformation),
                Vector2.Transform(p3, transformation));
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
            return Geometry.Distance(new Point(point), this);
        }

        #endregion
    }
}
