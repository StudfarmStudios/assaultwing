using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Helpers.Serialization;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// A polygon in two-dimensional space.
    /// </summary>
    /// A polygon is always simple, i.e., its edge doesn't intersect itself.
    [LimitedSerialization]
    public class Polygon : IGeomPrimitive, IEquatable<Polygon>, IConsistencyCheckable
    {
        /// <summary>
        /// A strip of faces of a polygon.
        /// </summary>
        public struct FaceStrip
        {
            /// <summary>
            /// Index of the first vertex in the strip.
            /// </summary>
            public int startIndex;

            /// <summary>
            /// Index of the last vertex in the strip, (inclusive end).
            /// If the strip ends the polygon, then <b>endIndex</b> equals
            /// vertex count + 1 which denotes index 0.
            /// </summary>
            public int endIndex;

            /// <summary>
            /// Tight, axis-aligned bounding box for the face strip.
            /// </summary>
            public Rectangle boundingBox;

            /// <summary>
            /// Creates a face strip for a polygon.
            /// </summary>
            /// <param name="startIndex">Index of the first vertex in the strip.</param>
            /// <param name="endIndex">Index of the first vertex not in the strip, (exclusive end).</param>
            /// <param name="boundingBox">Bounding box for the face strip.</param>
            public FaceStrip(int startIndex, int endIndex, Rectangle boundingBox)
            {
                this.startIndex = startIndex;
                this.endIndex = endIndex;
                this.boundingBox = boundingBox;
            }
        }

        /// <summary>
        /// Maximum number of faces in one face strip.
        /// </summary>
        private static readonly int faceStripSize = 20;

        /// <summary>
        /// The vertices of the polygon. Each vertex is listed only once and in order.
        /// </summary>
        [TypeParameter, RuntimeState]
        Vector2[] vertices;

        /// <summary>
        /// A rectangle containing all the vertices.
        /// </summary>
        Rectangle boundingBox;

        /// <summary>
        /// The polygon's faces separated into strips, for optimisation purposes.
        /// May be <b>null</b>.
        /// </summary>
        FaceStrip[] faceStrips;

        /// <summary>
        /// Returns the vertices of the polygon.
        /// </summary>
        /// In order to preserve the simplicity of the polygon, the 
        /// vertices should not be modified. Rather, create a new polygon.
        public Vector2[] Vertices { get { return vertices; } }

        /// <summary>
        /// Grouping of the polygon's faces into small strips.
        /// May be null.
        /// </summary>
        /// Face strips can be used for optimisation purposes.
        public FaceStrip[] FaceStrips { get { return faceStrips; } }

        /// <summary>
        /// Creates an uninitialised polygon.
        /// </summary>
        public Polygon()
        {
            vertices = null;
        }

        /// <summary>
        /// Creates a simple polygon.
        /// </summary>
        /// <param name="vertices">The vertices of the polygon in order.</param>
        public Polygon(Vector2[] vertices)
        {
            // Sanity check.
            if (vertices.Length < 3)
                throw new Exception("At least 3 vertices needed for a polygon");

            // Make a shallow copy so that outside code cannot alter our data 
            // without us knowing about it. Vector2 is a struct so a deep copy
            // is not needed.
            this.vertices = (Vector2[])vertices.Clone();

            boundingBox = new Rectangle();
            faceStrips = null;
            UpdateBoundingBox();
            UpdateFaceStrips();

#if false// HACK: polygon simplicity check skipped 
//#if DEBUG
            // Make sure the polygon is simple.
            // This is O(n^2) -- slow code and thus not wanted in release builds.
            // This could be done in O(n log n) by a scanline algorithm, or
            // apparently even in O(n) by some sophisticated triangulation algorithm.
            for (int i = 0; i < vertices.Length; ++i)
            {
                for (int j = i + 1; j < vertices.Length; ++j)
                {
                    Geometry.LineIntersectionType intersect =
                        Geometry.Intersect(vertices[i], vertices[(i + 1) % vertices.Length],
                                       vertices[j], vertices[(j + 1) % vertices.Length]);
                    if ((i + 1) % vertices.Length == j || (j + 1) % vertices.Length == i)
                    {
                        if (intersect != Geometry.LineIntersectionType.Point)
                            throw new Exception("Not a simple polygon");
                    }
                    else
                    {
                        if (intersect != Geometry.LineIntersectionType.None)
                            throw new Exception("Not a simple polygon");
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Returns true iff the polygon's vertices are in a clockwise sequence.
        /// </summary>
        /// <returns>True iff the polygon's vertices are in a clockwise sequence.</returns>
        public bool Clockwise()
        {
            for (int i = 0; i + 2 < vertices.Length; ++i)
                switch (Geometry.Stand(vertices[i + 2], vertices[i], vertices[i + 1]))
                {
                    case Geometry.StandType.Left: return false;
                    case Geometry.StandType.Right: return true;
                    case Geometry.StandType.Edge: continue;
                }
            // We should never get here.
            throw new Exception("Polygon winding undetermined (" + vertices.Length.ToString()
                + " vertices)");
        }

        /// <summary>
        /// Returns a string that represents this polygon.
        /// </summary>
        /// <returns>A string that represents this polygon.</returns>
        public override string ToString()
        {
            string[] vertNames = Array.ConvertAll<Vector2, string>(vertices, delegate(Vector2 v)
            {
                return v.ToString();
            });
            string value = "{" + String.Join(", ", vertNames) + "}";
            return value;
        }

        /// <summary>
        /// Updates <b>boundingBox</b>.
        /// </summary>
        private void UpdateBoundingBox()
        {
            Vector2 min = new Vector2(Single.MaxValue);
            Vector2 max = new Vector2(Single.MinValue);
            foreach (Vector2 v in vertices)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
            boundingBox = new Rectangle(min, max);
        }

        /// <summary>
        /// Updates <b>faceStrips</b>.
        /// </summary>
        private void UpdateFaceStrips()
        {
            faceStrips = null;

            // Small polygons won't benefit from extra structures.
            if (vertices.Length < faceStripSize * 2)
                return;

            // Divide faces to maximal strips with no brilliant logic.
            // This is a place for a clever algorithm. A good split into face strips
            // is one where the total area of bounding boxes is small.
            List<FaceStrip> faceStripList = new List<FaceStrip>();
            int startIndex = 0;
            while (startIndex < vertices.Length)
            {
                int endIndex = Math.Min(startIndex + faceStripSize, vertices.Length);
                Vector2 min = vertices[startIndex];
                Vector2 max = vertices[startIndex];
                for (int i = startIndex + 1; i <= endIndex; ++i)
                {
                    int realI = i % vertices.Length;
                    min = Vector2.Min(min, vertices[realI]);
                    max = Vector2.Max(max, vertices[realI]);
                }
                faceStripList.Add(new FaceStrip(startIndex, endIndex,
                    new Rectangle(min, max)));
                startIndex = endIndex;
            }
            this.faceStrips = faceStripList.ToArray();
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
        /// If the transformation scales X and Y axes differently, the result
        /// is undefined.
        public IGeomPrimitive Transform(Matrix transformation)
        {
            Polygon poly = new Polygon(vertices); // vertices are cloned
            Vector2.Transform(poly.vertices, ref transformation, poly.vertices);
            poly.UpdateBoundingBox();
            poly.UpdateFaceStrips();
            return poly;
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

        #endregion IGeomPrimitive Members

        #region IEquatable<Polygon> Members

        /// <summary>
        /// Indicates whether this object defines the same polygon as another object.
        /// Equality is taken in the geometric sense. In particular, the order 
        /// of vertices is unimportant.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(Polygon other)
        {
            if (this.Vertices.Length != other.Vertices.Length)
                return false;

            // Find our first vertex in the other polygon.
            int firstOtherI = -1;
            for (int otherI = 0; otherI < other.Vertices.Length; ++otherI)
            {
                if (other.Vertices[otherI].Equals(this.Vertices[0]))
                {
                    firstOtherI = otherI;
                    break;
                }
            }
            if (firstOtherI == -1)
                return false;

            // Figure out winding in the other polygon.
            int otherInc = 0;
            if (other.Vertices[(firstOtherI + 1) % other.Vertices.Length].Equals(this.Vertices[1]))
                otherInc = 1;
            else if (other.Vertices[(firstOtherI - 1 + other.Vertices.Length) % other.Vertices.Length].Equals(this.Vertices[1]))
                otherInc = -1;
            else
                return false;

            // Compare the remaining vertices.
            for (int i = 1; i < Vertices.Length; ++i)
                if (!other.Vertices[(firstOtherI + i * otherInc + other.Vertices.Length) % other.Vertices.Length].Equals(this.Vertices[i]))
                    return false;
            return true;
        }

        #endregion

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public void MakeConsistent(Type limitationAttribute)
        {
            UpdateBoundingBox();
            UpdateFaceStrips();
        }

        #endregion

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            checked
            {
                if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
                {
                    writer.Write((ushort)vertices.Length);
                    foreach (var vertex in vertices) writer.Write((Vector2)vertex);
                }
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
                int vertexCount = reader.ReadUInt16();
                vertices = new Vector2[vertexCount];
                for (int i = 0; i < vertexCount; i++) vertices[i] = reader.ReadVector2();
                UpdateBoundingBox();
                UpdateFaceStrips();
            }
        }
    }
}
