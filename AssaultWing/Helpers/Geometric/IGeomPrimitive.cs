using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// Interface for a geometric primitive.
    /// </summary>
    public interface IGeomPrimitive
    {
        /// <summary>
        /// A rectangle that contains the geometric primitive.
        /// </summary>
        /// The Z-coordinates are irrelevant.
        BoundingBox BoundingBox { get; }

        /// <summary>
        /// Transforms the geometric primitive by a transformation matrix.
        /// </summary>
        /// <param name="transformation">The transformation matrix.</param>
        /// <returns>The transformed geometric primitive.</returns>
        IGeomPrimitive Transform(Matrix transformation);

        /// <summary>
        /// Returns the shortest distance between the geometric primitive
        /// and a point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>The shortest distance between the geometric primitive
        /// and the point.</returns>
        float DistanceTo(Vector2 point);
    }
}
