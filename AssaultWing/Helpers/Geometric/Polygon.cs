// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif
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

        #region Unit tests
#if DEBUG
        /// <summary>
        /// Test class for Polygon.
        /// </summary>
        [TestFixture]
        public class PolygonTest
        {
            /// <summary>
            /// Sets up the test.
            /// </summary>
            [SetUp]
            public void SetUp()
            {
            }

            /// <summary>
            /// Trying to create different polygons.
            /// </summary>
            [Test]
            public void TestPolygon()
            {
                Vector2 p1 = new Vector2(10f, 10f);
                Vector2 p2 = new Vector2(30f, 10f);
                Vector2 p3 = new Vector2(30f, 30f);
                Vector2 p4 = new Vector2(10f, 30f);
                Vector2 p5 = new Vector2(40f, 10f);
                Vector2 p6 = new Vector2(20f, 10f);
                Vector2 p7 = new Vector2(20f, -10f);

                // Good polygons
                AssertPolygonCreation(new Vector2[] { p1, p2, p3, p4 }, true);  // square
                AssertPolygonCreation(new Vector2[] { p1, p2, p3 }, true);  // triangle
                AssertPolygonCreation(new Vector2[] { p1, p3, p2 }, true);  // triangle, opposite winding

                // Reduced polygons
                AssertPolygonCreation(new Vector2[] { p1, p2 }, false);  // line segment
                AssertPolygonCreation(new Vector2[] { p1 }, false);  // dot
                AssertPolygonCreation(new Vector2[] { p1, p1 }, false);  // reduced line segment
                AssertPolygonCreation(new Vector2[] { p1, p2, p3, p3, p4 }, false);  // reduced line segment
                AssertPolygonCreation(new Vector2[] { p1, p2, p3, p4, p1 }, false);  // loop back to start
                AssertPolygonCreation(new Vector2[] { p1, p2, p5 }, false);  // flat triangle

                // Intersecting edge
                AssertPolygonCreation(new Vector2[] { p1, p3, p2, p4 }, false); // edge intersects at a point
                AssertPolygonCreation(new Vector2[] { p1, p2, p3, p5, p6, p7 }, false); // parallel segments intersect not only at an endpoint
            }

            /// <summary>
            /// Tries to create a polygon and asserts that its creation succeeds as specified.
            /// </summary>
            /// <param name="vertices">The vertices of the polygon.</param>
            /// <param name="shouldSucceed">Should the creation succeed.</param>
            public void AssertPolygonCreation(Vector2[] vertices, bool shouldSucceed)
            {
                try
                {
                    Polygon poly1 = new Polygon(vertices);
                    Assert.IsTrue(shouldSucceed, "Exception for illegal polygon not thrown");
                }
                catch (Exception e)
                {
                    // Let NUnit exceptions fall through.
                    if (e is AssertionException)
                        throw e;
                    Assert.IsFalse(shouldSucceed, "Legal polygon was denied creation");
                }
            }

            /// <summary>
            /// Tests polygon equality.
            /// </summary>
            [Test]
            public void TestEquality()
            {
                Polygon p1 = new Polygon(new Vector2[] {
                    new Vector2(10f, 10f),
                    new Vector2(50f, 10f),
                    new Vector2(50f, 50f),
                    new Vector2(10f, 50f),
                });
                Polygon p2 = new Polygon(new Vector2[] {
                    new Vector2(10f, 10f),
                    new Vector2(50f, 10f),
                    new Vector2(50f, 50f),
                    new Vector2(10f, 50f),
                });
                Polygon p3 = new Polygon(new Vector2[] {
                    new Vector2(50f, 10f),
                    new Vector2(50f, 50f),
                    new Vector2(10f, 50f),
                    new Vector2(10f, 10f),
                });
                Polygon p4 = new Polygon(new Vector2[] {
                    new Vector2(50f, 10f),
                    new Vector2(10f, 10f),
                    new Vector2(10f, 50f),
                    new Vector2(50f, 50f),
                });
                Polygon p5 = new Polygon(new Vector2[] {
                    new Vector2(50f, 10f),
                    new Vector2(10f, 10f),
                    new Vector2(10f, 50f),
                });
                Polygon p6 = new Polygon(new Vector2[] {
                    new Vector2(10f, 10f),
                    new Vector2(50f, 10f),
                    new Vector2(50f, 50f),
                    new Vector2(10f, 50f),
                    new Vector2(30f, 30f),
                });
                Polygon p7 = new Polygon(new Vector2[] {
                    new Vector2(10f, 10f),
                    new Vector2(50f, 10f),
                    new Vector2(50f, 50f),
                    new Vector2(30f, 30f),
                    new Vector2(10f, 50f),
                });
                Assert.IsTrue(p1.Equals(p2));
                Assert.IsTrue(p2.Equals(p1));
                Assert.IsTrue(p1.Equals(p3));
                Assert.IsTrue(p3.Equals(p1));
                Assert.IsTrue(p1.Equals(p4));
                Assert.IsTrue(p4.Equals(p1));
                Assert.IsFalse(p4.Equals(p5));
                Assert.IsFalse(p5.Equals(p4));
                Assert.IsFalse(p7.Equals(p6));
                Assert.IsFalse(p6.Equals(p7));
            }
        }
#endif
        #endregion // Unit tests

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

        #region INetworkSerializable Members

        /// <summary>
        /// Serialises the object to a binary writer.
        /// </summary>
        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)vertices.Length);
                foreach (var vertex in vertices)
                {
                    writer.Write((float)vertex.X);
                    writer.Write((float)vertex.Y);
                }
            }
        }

        /// <summary>
        /// Deserialises the object from a binary writer.
        /// </summary>
        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                int vertexCount = reader.ReadInt32();
                vertices = new Vector2[vertexCount];
                for (int i = 0; i < vertexCount; ++i)
                    vertices[i] = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
                UpdateBoundingBox();
                UpdateFaceStrips();
            }
        }

        #endregion
    }
}
