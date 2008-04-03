// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
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

    /// <summary>
    /// The whole two-dimensional space.
    /// </summary>
    public struct Everything : IGeomPrimitive
    {
        #region IGeomPrimitive Members

        /// <summary>
        /// A rectangle that contains the geometric primitive.
        /// </summary>
        /// The Z-coordinates are irrelevant.
        public BoundingBox BoundingBox
        {
            get
            {
                return new BoundingBox(new Vector3(Single.MinValue),
                                       new Vector3(Single.MaxValue));
            }
        }

        /// <summary>
        /// Transforms the geometric primitive by a transformation matrix.
        /// </summary>
        /// <param name="transformation">The transformation matrix.</param>
        /// <returns>The transformed geometric primitive.</returns>
        public IGeomPrimitive Transform(Matrix transformation)
        {
            // It's not a _copy_ but it shouldn't matter because this
            // object has no state.
            return this;
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
            return 0;
        }

        #endregion
    }

    /// <summary>
    /// A point in two-dimensional space.
    /// </summary>
    public struct Point : IGeomPrimitive
    {
        Vector2 location;

        /// <summary>
        /// Gets and sets the location of the point.
        /// </summary>
        public Vector2 Location { get { return location; } set { location = value; } }

        /// <summary>
        /// Creates an arbitrary point.
        /// </summary>
        /// <param name="location">The point's location.</param>
        public Point(Vector2 location)
        {
            this.location = location;
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

        #region IGeomPrimitive Members

        /// <summary>
        /// A rectangle that contains the geometric primitive.
        /// </summary>
        /// The Z-coordinates are irrelevant.
        public BoundingBox BoundingBox
        {
            get
            {
                return new BoundingBox(new Vector3(location, 0),
                                       new Vector3(location, 0));
            }
        }

        /// <summary>
        /// Transforms the geometric primitive by a transformation matrix.
        /// </summary>
        /// <param name="transformation">The transformation matrix.</param>
        /// <returns>The transformed geometric primitive.</returns>
        public IGeomPrimitive Transform(Matrix transformation)
        {
            return new Point(Vector2.Transform(location, transformation));
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
            return Vector2.Distance(this.location, point);
        }

        #endregion IGeomPrimitive Members

    }

    /// <summary>
    /// A circle in two-dimensional space.
    /// </summary>
    public struct Circle : IGeomPrimitive
    {
        Vector2 center;
        float radius;

        /// <summary>
        /// Gets and sets the center of the circle.
        /// </summary>
        public Vector2 Center { get { return center; } set { center = value; } }

        /// <summary>
        /// Gets and sets the radius of the circle.
        /// </summary>
        public float Radius { get { return radius; } set { radius = MathHelper.Max(value, 0f); } }

        /// <summary>
        /// Creates an arbitrary circle.
        /// </summary>
        /// <param name="center">The circle's center.</param>
        /// <param name="radius">The circle's radius.</param>
        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        #region IGeomPrimitive Members

        /// <summary>
        /// A rectangle that contains the geometric primitive.
        /// </summary>
        /// The Z-coordinates are irrelevant.
        public BoundingBox BoundingBox
        {
            get
            {
                return new BoundingBox(new Vector3(center.X - radius, center.Y - radius, 0),
                                       new Vector3(center.X + radius, center.Y + radius, 0));
            }
        }

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

    /// <summary>
    /// A polygon in two-dimensional space.
    /// </summary>
    /// A polygon is always simple, i.e., its edge doesn't intersect itself.
    [LimitedSerialization]
    public struct Polygon : IGeomPrimitive, IEquatable<Polygon>, IConsistencyCheckable
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
            /// The Z-coordinate is not used.
            public BoundingBox boundingBox;

            /// <summary>
            /// Creates a face strip for a polygon.
            /// </summary>
            /// <param name="startIndex">Index of the first vertex in the strip.</param>
            /// <param name="endIndex">Index of the first vertex not in the strip, (exclusive end).</param>
            /// <param name="boundingBox">Bounding box for the face strip.</param>
            public FaceStrip(int startIndex, int endIndex, BoundingBox boundingBox)
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
        /// The Z-coordinate is irrelevant.
        BoundingBox boundingBox;

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

            boundingBox = new BoundingBox();
            faceStrips = null;
            UpdateBoundingBox();
            UpdateFaceStrips();

#if DEBUG
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
            boundingBox = new BoundingBox(new Vector3(min, 0), new Vector3(max, 0));
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
                    new BoundingBox(new Vector3(min, 0), new Vector3(max, 0))));
                startIndex = endIndex;
            }
            this.faceStrips = faceStripList.ToArray();
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
    }

    /// <summary>
    /// Contains helper methods for geometric problems.
    /// </summary>
    public class Geometry
    {
        #region Type definitions

        /// <summary>
        /// Kinds of intersection volumes that two lines can have.
        /// </summary>
        public enum LineIntersectionType
        {
            /// <summary>
            /// The lines don't intersect.
            /// </summary>
            None,
            /// <summary>
            /// The lines intersect at one point.
            /// </summary>
            Point,
            /// <summary>
            /// The lines intersect at a line interval (infinitely many points).
            /// </summary>
            Segment,
        }

        /// <summary>
        /// Type of stand of a point relative to the directed edge of a geometric object.
        /// </summary>
        public enum StandType
        {
            /// <summary>
            /// The point stands on the left hand side of the directed edge.
            /// </summary>
            Left,

            /// <summary>
            /// The point stands on the right hand side of the directed edge.
            /// </summary>
            Right,

            /// <summary>
            /// The point stands on the edge.
            /// </summary>
            Edge,
        }

        #endregion Type definitions

        #region Intersection methods

        /// <summary>
        /// Returns true iff the two geometric primitives intersect.
        /// </summary>
        /// <param name="prim1">One primitive.</param>
        /// <param name="prim2">The other primitive</param>
        /// <returns>True iff the two geometric primitives intersect.</returns>
        public static bool Intersect(IGeomPrimitive prim1, IGeomPrimitive prim2)
        {
            if (prim1 is Everything || prim2 is Everything)
                return true;
            if (prim1 is Point)
            {
                Point point1 = (Point)prim1;
                if (prim2 is Point) return point1.Location.Equals(((Point)prim2).Location);
                if (prim2 is Circle) return Intersect(point1, (Circle)prim2);
                if (prim2 is Polygon) return Intersect(point1, (Polygon)prim2);
            }
            if (prim1 is Circle)
            {
                Circle circle1 = (Circle)prim1;
                if (prim2 is Point) return Intersect((Point)prim2, circle1);
                if (prim2 is Circle) return Intersect(circle1, (Circle)prim2);
                if (prim2 is Polygon) return Intersect(circle1, (Polygon)prim2);
            }
            if (prim1 is Polygon)
            {
                Polygon polygon1 = (Polygon)prim1;
                if (prim2 is Point) return Intersect((Point)prim2, polygon1);
                if (prim2 is Circle) return Intersect((Circle)prim2, polygon1);
                if (prim2 is Polygon) return Intersect(polygon1, (Polygon)prim2);
            }
            Log.Write("Unknown geometric primitives in Geometry.Intersect(): " +
                      prim1.GetType().Name + " " + prim2.GetType().Name);
            return false;
        }

        /// <summary>
        /// Returns true iff the point is in the circle.
        /// </summary>
        /// The circle is thought of as a solid disc that contains also its edge.
        /// <param name="point">The point.</param>
        /// <param name="circle">The circle.</param>
        /// <returns>True iff the point is in the circle.</returns>
        public static bool Intersect(Point point, Circle circle)
        {
            return Vector2.DistanceSquared(point.Location, circle.Center) <= circle.Radius * circle.Radius;
        }

        /// <summary>
        /// Returns true iff the two circles intersect.
        /// </summary>
        /// The circles are thought of as solid discs that contain also their edges.
        /// <param name="circle1">One circle.</param>
        /// <param name="circle2">The other circle.</param>
        /// <returns>True iff the circles intersect.</returns>
        public static bool Intersect(Circle circle1, Circle circle2)
        {
            // The circles intersect iff their centers are at most the sum of
            // their radii apart. We can just as well compare the squares of the distances.
            float radiiSum = circle1.Radius + circle2.Radius;
            return Vector2.DistanceSquared(circle1.Center, circle2.Center) <= radiiSum * radiiSum;
        }

        /// <summary>
        /// Returns true iff the two line segments ab and cd intersect.
        /// </summary>
        /// <param name="a">One end of the first line segment.</param>
        /// <param name="b">The other end of the first line segment.</param>
        /// <param name="c">One end of the second line segment.</param>
        /// <param name="d">The other end of the second line segment.</param>
        /// <param name="intersection">Where to store the point of intersection.</param>
        /// <returns>True iff the line segments intersect.</returns>
        /// The point of intersection will be stored to 'intersection' only if the segments
        /// intersect at a single point. If the segments don't intersect, 'intersect' is unmodified.
        /// If the segments intersect at a whole line segment, 'intersection' will be set to null.
        public static bool Intersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d, ref Vector2? intersection)
        {
            // This algorithm is adapted from C code by Franklin Antonio 
            // at http://www.graphicsgems.org
            float x1lo, x1hi, y1lo, y1hi;
            float Ax = b.X - a.X;
            float Bx = c.X - d.X;

            // X bound box test
            if (Ax < 0)
            {
                x1lo = b.X;
                x1hi = a.X;
            }
            else
            {
                x1hi = b.X;
                x1lo = a.X;
            }
            if (Bx > 0)
            {
                if (x1hi < d.X || c.X < x1lo) return false;
            }
            else
            {
                if (x1hi < c.X || d.X < x1lo) return false;
            }

            float Ay = b.Y - a.Y;
            float By = c.Y - d.Y;

            // Y bound box test
            if (Ay < 0)
            {
                y1lo = b.Y;
                y1hi = a.Y;
            }
            else
            {
                y1hi = b.Y;
                y1lo = a.Y;
            }
            if (By > 0)
            {
                if (y1hi < d.Y || c.Y < y1lo) return false;
            }
            else
            {
                if (y1hi < c.Y || d.Y < y1lo) return false;
            }

            float Cx = a.X - c.X;
            float Cy = a.Y - c.Y;
            float val1 = By * Cx - Bx * Cy;
            float val3 = Ay * Bx - Ax * By;
            if (val3 > 0)
            {
                if (val1 < 0 || val1 > val3) return false;
            }
            else
            {
                if (val1 > 0 || val1 < val3) return false;
            }
            float val2 = Ax * Cy - Ay * Cx;
            if (val3 > 0)
            {
                if (val2 < 0 || val2 > val3) return false;
            }
            else
            {
                if (val2 > 0 || val2 < val3) return false;
            }

            if (val3 == 0)
            {
                // both segments on the same line
                intersection = null;
                return true;
            }

            Vector2 inters = new Vector2();
            float num = val1 * Ax;	               // numerator
            inters.X = a.X + num / val3;           // intersection x

            num = val1 * Ay;
            inters.Y = a.Y + num / val3;           // intersection y
            intersection = new Vector2?(inters);
            return true; // intersecting segments
        }

        /// <summary>
        /// Returns the kind of intersection of the two line segments ab and cd.
        /// </summary>
        /// <param name="a">One end of the first line segment.</param>
        /// <param name="b">The other end of the first line segment.</param>
        /// <param name="c">One end of the second line segment.</param>
        /// <param name="d">The other end of the second line segment.</param>
        /// <returns>How the two line segments intersect, if at all.</returns>
        public static LineIntersectionType Intersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            // This algorithm is adapted from C code by Franklin Antonio 
            // at http://www.graphicsgems.org
            float x1lo, x1hi, y1lo, y1hi;
            float Ax = b.X - a.X;
            float Bx = c.X - d.X;

            // X bound box test
            if (Ax < 0)
            {
                x1lo = b.X;
                x1hi = a.X;
            }
            else
            {
                x1hi = b.X;
                x1lo = a.X;
            }
            if (Bx > 0)
            {
                if (x1hi < d.X || c.X < x1lo) return LineIntersectionType.None;
            }
            else
            {
                if (x1hi < c.X || d.X < x1lo) return LineIntersectionType.None;
            }

            float Ay = b.Y - a.Y;
            float By = c.Y - d.Y;

            // Y bound box test
            if (Ay < 0)
            {
                y1lo = b.Y;
                y1hi = a.Y;
            }
            else
            {
                y1hi = b.Y;
                y1lo = a.Y;
            }
            if (By > 0)
            {
                if (y1hi < d.Y || c.Y < y1lo) return LineIntersectionType.None;
            }
            else
            {
                if (y1hi < c.Y || d.Y < y1lo) return LineIntersectionType.None;
            }

            float Cx = a.X - c.X;
            float Cy = a.Y - c.Y;
            float val1 = By * Cx - Bx * Cy;
            float val3 = Ay * Bx - Ax * By;
            if (val3 > 0)
            {
                if (val1 < 0 || val1 > val3) return LineIntersectionType.None;
            }
            else
            {
                if (val1 > 0 || val1 < val3) return LineIntersectionType.None;
            }
            float val2 = Ax * Cy - Ay * Cx;
            if (val3 > 0)
            {
                if (val2 < 0 || val2 > val3) return LineIntersectionType.None;
            }
            else
            {
                if (val2 > 0 || val2 < val3) return LineIntersectionType.None;
            }

            if (val3 == 0) return LineIntersectionType.Segment;
            return LineIntersectionType.Point;
        }

        /// <summary>
        /// Returns true iff the point is in the area defined by the polygon.
        /// </summary>
        /// Edges are considered to belong to the polygon.
        /// <param name="point">The point to check.</param>
        /// <param name="polygon">The polygon to check.</param>
        /// <returns>True iff the point is in the polygon.</returns>
        public static bool Intersect(Point point, Polygon polygon)
        {
            // Adapted from C code by Eric Haines, from http://www.graphicsgems.org/.

            // This version is usually somewhat faster than the original published in
            // Graphics Gems IV; by turning the division for testing the X axis crossing
            // into a tricky multiplication test this part of the test became faster,
            // which had the additional effect of making the test for "both to left or
            // both to right" a bit slower for triangles than simply computing the
            // intersection each time.  The main increase is in triangle testing speed,
            // which was about 15% faster; all other polygon complexities were pretty much
            // the same as before.  On machines where division is very expensive (not the
            // case on the HP 9000 series on which I tested) this test should be much
            // faster overall than the old code.  Your mileage may (in fact, will) vary,
            // depending on the machine and the test data, but in general I believe this
            // code is both shorter and faster.  This test was inspired by unpublished
            // Graphics Gems submitted by Joseph Samosky and Mark Haigh-Hutchinson.
            // Related work by Samosky is in:
            //
            // Samosky, Joseph, "SectionView: A system for interactively specifying and
            // visualizing sections through three-dimensional medical image data",
            // M.S. Thesis, Department of Electrical Engineering and Computer Science,
            // Massachusetts Institute of Technology, 1993.

            // Shoot a test ray along +X axis.  The strategy is to compare vertex Y values
            // to the testing point's Y and quickly discard edges which are entirely to one
            // side of the test ray. 
            Vector2 vPrev = polygon.Vertices[polygon.Vertices.Length - 1];

            // Get test bit for above/below X axis.
            bool yflag0 = vPrev.Y >= point.Location.Y;

            bool inside_flag = false;
            foreach (Vector2 v in polygon.Vertices)
            {
                bool yflag1 = v.Y >= point.Location.Y;

                // Check if endpoints straddle (are on opposite sides) of X axis
                // (i.e. the Y's differ); if so, +X ray could intersect this edge.
                // The old test also checked whether the endpoints are both to the
                // right or to the left of the test point.  However, given the faster
                // intersection point computation used below, this test was found to
                // be a break-even proposition for most polygons and a loser for
                // triangles (where 50% or more of the edges which survive this test
                // will cross quadrants and so have to have the X intersection computed
                // anyway).  I credit Joseph Samosky with inspiring me to try dropping
                // the "both left or both right" part of my code.
                if (yflag0 != yflag1)
                {
                    /* Check intersection of pgon segment with +X ray.
                     * Note if >= point's X; if so, the ray hits it.
                     * The division operation is avoided for the ">=" test by checking
                     * the sign of the first vertex wrto the test point; idea inspired
                     * by Joseph Samosky's and Mark Haigh-Hutchinson's different
                     * polygon inclusion tests.
                     */
                    if (((v.Y - point.Location.Y) * (vPrev.X - v.X) >=
                        (v.X - point.Location.X) * (vPrev.Y - v.Y)) == yflag1)
                    {
                        inside_flag = !inside_flag;
                    }
                }

                /* Move to the next pair of vertices, retaining info as possible. */
                yflag0 = yflag1;
                vPrev = v;
            }
            return inside_flag;
        }

        /// <summary>
        /// Returns true iff the circle intersects the polygon.
        /// </summary>
        /// The circle and the polygon are considered to contain their respective edges.
        /// <param name="circle">The circle to check.</param>
        /// <param name="polygon">The polygon to check.</param>
        /// <returns>True iff the circle intersects the polygon.</returns>
        public static bool Intersect(Circle circle, Polygon polygon)
        {
#if GEOMETRY_APPROXIMATE
            // This approximation is unsuitable for ship vs. wall collisions;
            // players get stuck very easily.
            Point right = new Point(circle.Center + new Vector2(circle.Radius, 0));
            Point top = new Point(circle.Center + new Vector2(0, circle.Radius));
            Point left = new Point(circle.Center + new Vector2(-circle.Radius, 0));
            Point bottom = new Point(circle.Center + new Vector2(0, -circle.Radius));
            return Intersect(right, polygon) || Intersect(top, polygon)
                || Intersect(left, polygon) || Intersect(bottom, polygon);
#else
            return DistanceSquared(new Point(circle.Center), polygon) < circle.Radius * circle.Radius;
#endif
        }

        /// <summary>
        /// Returns true iff the two polygons intersect.
        /// </summary>
        /// The polygons are considered to contain their respective edges.
        /// <param name="polygon1">One polygon.</param>
        /// <param name="polygon2">The other polygon.</param>
        /// <returns>True iff the two polygons intersect.</returns>
        public static bool Intersect(Polygon polygon1, Polygon polygon2)
        {
            // TODO: Implement polygon-polygon intersection.
            return false;
        }

        #endregion Intersection methods

        #region Location and distance query methods

        /// <summary>
        /// Returns the distance between 'point' and the line segment ab.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="a">One end of the line segment.</param>
        /// <param name="b">The other end of the line segment.</param>
        /// <returns>The distance between 'point' and the line segment ab.</returns>
        public static float Distance(Point point, Vector2 a, Vector2 b)
        {
            return (float)Math.Sqrt(DistanceSquared(point, a, b));
        }

        /// <summary>
        /// Returns the squared distance between 'point' and the line segment ab.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="a">One end of the line segment.</param>
        /// <param name="b">The other end of the line segment.</param>
        /// <returns>The squared distance between 'point' and the line segment ab.</returns>
        public static float DistanceSquared(Point point, Vector2 a, Vector2 b)
        {
            // There's three cases to consider, depending on the point's projection
            // on the line that a and b define.
            Vector2 ab = b - a;
            Vector2 ap = point.Location - a;
            Vector2 bp = point.Location - b;
            float dot1 = Vector2.Dot(ab, ap);
            float dot2 = Vector2.Dot(ab, bp);

            // Case 1: Point's projection is outside the line segment, closer to a than b.
            if (dot1 <= 0)
                return ap.LengthSquared();

            // Case 2: Point's projection is outside the line segment, closer to b than a.
            if (dot2 >= 0)
                return bp.LengthSquared();

            // Case 3: Point's projection is on the line segment.
            return ap.LengthSquared() - dot1 * dot1 / ab.LengthSquared();
        }

        /// <summary>
        /// Returns a point on the line segment that is maximally close to the given point.
        /// </summary>
        /// There may not be a unique closest point. In such a case the exact return
        /// value is undefined but will still meet the definition of the return value.
        /// If the given point is on the line segment, the same point is returned.
        /// <param name="a">One end of the line segment.</param>
        /// <param name="b">The other end of the line segment.</param>
        /// <param name="point">The point.</param>
        /// <returns>A point on the line segment that is maximally close to the given point.</returns>
        public static Point GetClosestPoint(Vector2 a, Vector2 b, Point point)
        {
            float distance;
            return GetClosestPoint(a, b, point, out distance);
        }

        /// <summary>
        /// Returns a point on the line segment that is maximally close to the given point.
        /// Also computes the distance from the given point to the returned point.
        /// </summary>
        /// There may not be a unique closest point. In such a case the exact return
        /// value is undefined but will still meet the definition of the return value.
        /// If the given point is on the line segment, the same point is returned.
        /// <param name="a">One end of the line segment.</param>
        /// <param name="b">The other end of the line segment.</param>
        /// <param name="point">The point.</param>
        /// <param name="distance">Where to store the distance between the given point and the returned point.</param>
        /// <returns>A point on the line segment that is maximally close to the given point.</returns>
        public static Point GetClosestPoint(Vector2 a, Vector2 b, Point point, out float distance)
        {
            // There's three cases to consider, depending on the point's projection
            // on the line that a and b define.
            Vector2 ab = b - a;
            Vector2 ap = point.Location - a;
            Vector2 bp = point.Location - b;
            float dot1 = Vector2.Dot(ab, ap);
            float dot2 = Vector2.Dot(ab, bp);
            Point closestPoint;

            // Case 1: Point's projection is outside the line segment, closer to a than b.
            if (dot1 <= 0)
                closestPoint = new Point(a);

            // Case 2: Point's projection is outside the line segment, closer to b than a.
            else if (dot2 >= 0)
                closestPoint = new Point(b);

            // Case 3: Point's projection is on the line segment.
            else
            {
                Vector2 apProjectedToAb = ab * Vector2.Dot(ap, ab) / ab.LengthSquared();
                closestPoint = new Point(a + apProjectedToAb);
            }

            distance = (point.Location - closestPoint.Location).Length();
            return closestPoint;
        }

        /// <summary>
        /// Returns the distance between a point and a rectangle.
        /// </summary>
        /// The distance is the least distance between the point and any
        /// point that lies in the rectangle. In particular, if the point itself
        /// lies inside the rectangle, zero will be returned.
        /// <param name="point">The point.</param>
        /// <param name="rectangle">The rectangle. Z-coordinates are not used.</param>
        /// <returns>The distance between the point and the rectangle.</returns>
        public static float Distance(Point point, BoundingBox rectangle)
        {
            bool left = point.Location.X < rectangle.Min.X;
            bool right = rectangle.Max.X < point.Location.X;
            bool under = point.Location.Y < rectangle.Min.Y;
            bool over = rectangle.Max.Y < point.Location.Y;

            // Is the shortest distance measured from a face?
            if (!left && !right)
            {
                if (under)
                    return rectangle.Min.Y - point.Location.Y;
                if (over)
                    return point.Location.Y - rectangle.Max.Y;
                return 0;
            }
            if (!under && !over)
            {
                if (left)
                    return rectangle.Min.X - point.Location.X;
                if (right)
                    return point.Location.X - rectangle.Max.X;
                return 0;
            }

            // Shortest distance is measured from a corner.
            if (left)
            {
                if (under)
                    return (float)Math.Sqrt(
                        (rectangle.Min.X - point.Location.X) * (rectangle.Min.X - point.Location.X) +
                        (rectangle.Min.Y - point.Location.Y) * (rectangle.Min.Y - point.Location.Y));
                else // over
                    return (float)Math.Sqrt(
                        (rectangle.Min.X - point.Location.X) * (rectangle.Min.X - point.Location.X) +
                        (point.Location.Y - rectangle.Max.Y) * (point.Location.Y - rectangle.Max.Y));
            }
            else // right
            {
                if (under)
                    return (float)Math.Sqrt(
                        (point.Location.X - rectangle.Max.X) * (point.Location.X - rectangle.Max.X) +
                        (rectangle.Min.Y - point.Location.Y) * (rectangle.Min.Y - point.Location.Y));
                else // over
                    return (float)Math.Sqrt(
                        (point.Location.X - rectangle.Max.X) * (point.Location.X - rectangle.Max.X) +
                        (point.Location.Y - rectangle.Max.Y) * (point.Location.Y - rectangle.Max.Y));
            }
        }

        /// <summary>
        /// Returns the squared distance between a point and a rectangle.
        /// </summary>
        /// The distance is the least distance between the point and any
        /// point that lies in the rectangle. In particular, if the point itself
        /// lies inside the rectangle, zero will be returned.
        /// <param name="point">The point.</param>
        /// <param name="rectangle">The rectangle. Z-coordinates are not used.</param>
        /// <returns>The squared distance between the point and the rectangle.</returns>
        public static float DistanceSquared(Point point, BoundingBox rectangle)
        {
            bool left = point.Location.X < rectangle.Min.X;
            bool right = rectangle.Max.X < point.Location.X;
            bool under = point.Location.Y < rectangle.Min.Y;
            bool over = rectangle.Max.Y < point.Location.Y;

            // Is the shortest distance measured from a face?
            if (!left && !right)
            {
                if (under)
                    return (rectangle.Min.Y - point.Location.Y) * (rectangle.Min.Y - point.Location.Y);
                if (over)
                    return (point.Location.Y - rectangle.Max.Y) * (point.Location.Y - rectangle.Max.Y);
                return 0;
            }
            if (!under && !over)
            {
                if (left)
                    return (rectangle.Min.X - point.Location.X) * (rectangle.Min.X - point.Location.X);
                if (right)
                    return (point.Location.X - rectangle.Max.X) * (point.Location.X - rectangle.Max.X);
                return 0;
            }

            // Shortest distance is measured from a corner.
            if (left)
            {
                if (under)
                    return 
                        (rectangle.Min.X - point.Location.X) * (rectangle.Min.X - point.Location.X) +
                        (rectangle.Min.Y - point.Location.Y) * (rectangle.Min.Y - point.Location.Y);
                else // over
                    return 
                        (rectangle.Min.X - point.Location.X) * (rectangle.Min.X - point.Location.X) +
                        (point.Location.Y - rectangle.Max.Y) * (point.Location.Y - rectangle.Max.Y);
            }
            else // right
            {
                if (under)
                    return 
                        (point.Location.X - rectangle.Max.X) * (point.Location.X - rectangle.Max.X) +
                        (rectangle.Min.Y - point.Location.Y) * (rectangle.Min.Y - point.Location.Y);
                else // over
                    return 
                        (point.Location.X - rectangle.Max.X) * (point.Location.X - rectangle.Max.X) +
                        (point.Location.Y - rectangle.Max.Y) * (point.Location.Y - rectangle.Max.Y);
            }
        }

        /// <summary>
        /// Returns the distance between the given point and polygon.
        /// </summary>
        /// The returned distance is the least distance between the point and any
        /// point that lies in the polygon. In particular, if the point itself
        /// lies inside the polygon, zero will be returned.
        /// <param name="point">The point.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns>The distance between the given point and polygon.</returns>
        public static float Distance(Point point, Polygon polygon)
        {
            return (float)Math.Sqrt(DistanceSquared(point, polygon));
        }

        /// <summary>
        /// Returns the squared distance between the given point and polygon.
        /// </summary>
        /// The distance is the least distance between the point and any
        /// point that lies in the polygon. In particular, if the point itself
        /// lies inside the polygon, zero will be returned.
        /// <param name="point">The point.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns>The squared distance between the given point and polygon.</returns>
        public static float DistanceSquared(Point point, Polygon polygon)
        {
            if (Intersect(point, polygon))
                return 0;
            Vector2[] vertices = polygon.Vertices;
            if (polygon.FaceStrips != null)
            {
                float bestDistanceSquared = Single.MaxValue;
                float bestStripDistanceSquared = Single.MaxValue;
                int bestStripI = -1;

                // The distance (from the point) to each face strip is at most
                // the least distance to the farthest corner of a face of the
                // bounding box of the face strip, i.e. the second-shortest distance
                // to a corner of the strip's bounding box. This is so because the
                // bounding box is tight and thus there is at least one vertex
                // of the face strip lying on each face of the bounding box.
                // If the point is in the bounding box, the strip must be checked.
                // From bounding boxes not containing the point one must check
                // the closest strip and all strips whose closest corner is closer
                // than the second-closest corner of the closest strip.
                // For this, the strips will be ordered in 'stripIs' by distance 
                // to their bounding box. Strips that contain the query point
                // we mark with distance zero, thus always making them to be checked.
                // 'stripIs' and 'stripDistances' have the same indexing.
                int[] stripIs = new int[polygon.FaceStrips.Length];
                for (int i = 0; i < stripIs.Length; ++i) stripIs[i] = i;
                float[] stripDistancesSquared = new float[polygon.FaceStrips.Length];
                for (int stripI = 0; stripI < polygon.FaceStrips.Length; ++stripI)
                {
                    Polygon.FaceStrip strip = polygon.FaceStrips[stripI];
                    if (strip.boundingBox.Contains(new Vector3(point.Location, 0)) == ContainmentType.Contains)
                    {
                        stripDistancesSquared[stripI] = 0;
                    }
                    else
                    {
                        // Seek out the closest face strip of those that don't contain the query point.
                        float[] cornerDistsSquared = new float[4] { 
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.boundingBox.Min.X, strip.boundingBox.Min.Y)),
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.boundingBox.Min.X, strip.boundingBox.Max.Y)),
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.boundingBox.Max.X, strip.boundingBox.Min.Y)),
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.boundingBox.Max.X, strip.boundingBox.Max.Y))
                        };
                        Array.Sort(cornerDistsSquared);
                        stripDistancesSquared[stripI] = DistanceSquared(point, strip.boundingBox);
                        float stripDistanceSquared = cornerDistsSquared[1];
                        if (stripDistanceSquared < bestStripDistanceSquared)
                        {
                            bestStripDistanceSquared = stripDistanceSquared;
                            bestStripI = stripI;
                        }
                    }
                }

                Array.Sort(stripDistancesSquared, stripIs);
                for (int stripI = 0; stripI < stripDistancesSquared.Length && stripDistancesSquared[stripI] <= bestStripDistanceSquared; ++stripI)
                {
                    Polygon.FaceStrip strip = polygon.FaceStrips[stripIs[stripI]];
                    int oldI = strip.startIndex;
                    for (int vertI = strip.startIndex + 1; vertI <= strip.endIndex; ++vertI)
                    {
                        int realI = vertI % vertices.Length;
                        bestDistanceSquared = MathHelper.Min(bestDistanceSquared,
                            DistanceSquared(point, vertices[oldI], vertices[realI]));
                        oldI = realI;
                    }
                }

                return bestDistanceSquared;
            }
            else
            {
                float bestDistanceSquared = Single.MaxValue;
                Vector2 oldV = polygon.Vertices[vertices.Length - 1];
                foreach (Vector2 v in vertices)
                {
                    bestDistanceSquared = MathHelper.Min(bestDistanceSquared, DistanceSquared(point, oldV, v));
                    oldV = v;
                }
                return bestDistanceSquared;
            }
        }

        /// <summary>
        /// Returns a point in the polygon that is maximally close to the given point.
        /// </summary>
        /// There may not be a unique closest point. In such a case the exact return
        /// value is undefined but will still meet the definition of the return value.
        /// If the given point is inside the polygon, the same point is returned.
        /// <param name="polygon">The polygon.</param>
        /// <param name="point">The point.</param>
        /// <returns>A point in the polygon that is maximally close to the given point.</returns>
        public static Point GetClosestPoint(Polygon polygon, Point point)
        {
            if (Intersect(point, polygon))
                return point;
            float bestDistance = Single.MaxValue;
            Point bestPoint = point;
            int oldI = polygon.Vertices.Length - 1;
            for (int i = 0; i < polygon.Vertices.Length; oldI = i++)
            {
                float distance;
                Point closestPoint = GetClosestPoint(polygon.Vertices[oldI], polygon.Vertices[i], point, out distance);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = closestPoint;
                }
            }
            return bestPoint;
        }

        /// <summary>
        /// Returns a unit normal vector from the given polygon pointing towards the given point.
        /// </summary>
        /// The returned vector will be normalised, it will be parallel to a shortest
        /// line segment that connects the polygon and the point, and it will
        /// point from the polygon towards the point. If the point lies inside
        /// the polygon, the zero vector will be returned.
        /// Note that normal is not unique in all cases. In ambiguous cases the exact
        /// result is undefined but will obey the specified return conditions.
        /// <param name="polygon">The polygon.</param>
        /// <param name="point">The point for the normal to point to.</param>
        /// <returns>A unit normal pointing to the given location.</returns>
        public static Vector2 GetNormal(Polygon polygon, Point point)
        {
            if (Intersect(point, polygon))
                return Vector2.Zero;
            Point closestPoint = GetClosestPoint(polygon, point);
            return Vector2.Normalize(point.Location - closestPoint.Location);
        }

        /// <summary>
        /// Returns where point p stands relative to the 
        /// directed line defined by the vector from a to b.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <param name="a">The tail of the vector.</param>
        /// <param name="b">The head of the vector.</param>
        /// <returns>The stand of p relative to the
        /// directed line defined by the vector from a to b.</returns>
        public static StandType Stand(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 dir = b - a;
            Vector2 leftNormal = new Vector2(-dir.Y, dir.X);
            float dot = Vector2.Dot(leftNormal, p - a);
            return dot < 0 ? StandType.Right
                : dot > 0 ? StandType.Left
                : StandType.Edge;
        }

        #endregion Location and distance query methods

        #region Other methods

        /// <summary>
        /// Translates cartesian coordinates into normalised barycentric 
        /// coordinates relative to a triangle.
        /// </summary>
        /// The triangle is given as the three vectors, v1, v2 and v3, and the
        /// coordinates to translate as the vector p. The resulting barycentric
        /// coordinates are (A,B,C), of which B and C are stored to amount2 and
        /// amount3, and A is 1-amount2-amount3.
        /// If the triangle is reduced, the return value is undefined.
        /// <param name="v1">First vertex of the triangle.</param>
        /// <param name="v2">Second vertex of the triangle.</param>
        /// <param name="v3">Third vertex of the triangle.</param>
        /// <param name="p">Coordinates to translate.</param>
        /// <param name="amount2">Resulting barycentric coordinate B.</param>
        /// <param name="amount3">Resulting barycentric coordinate C.</param>
        public static void CartesianToBarycentric(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 p,
            out float amount2, out float amount3)
        {
            float denom = (v2.X - v1.X) * (v3.Y - v1.Y) + (v1.X - v3.X) * (v2.Y - v1.Y);
            if (denom == 0)
            {
                // Triangle's faces are all parallel.
                amount2 = 0;
                amount3 = 0;
            }
            else
            {
                amount2 = ((v1.X - p.X) * (v1.Y - v3.Y) + (v3.X - v1.X) * (v1.Y - p.Y)) / denom;
                amount3 = ((v2.X - v1.X) * (p.Y - v1.Y) + (p.X - v1.X) * (v1.Y - v2.Y)) / denom;
            }
        }

        #endregion Other methods

        #region Unit tests
#if DEBUG
        /// <summary>
        /// Tests the Geometry class.
        /// </summary>
        [TestFixture]
        public class GeometryTest
        {
            /// <summary>
            /// Sets up the testing.
            /// </summary>
            [SetUp]
            public void SetUp()
            {
            }

            /// <summary>
            /// Tests the general intersection method.
            /// </summary>
            [Test]
            public void TestGeneralIntersect()
            {
                Point p1 = new Point(new Vector2(10f, 10f));
                Point p2 = new Point(new Vector2(30f, 90f));
                Circle c1 = new Circle(new Vector2(20f, 10f), 20f);
                Circle c2 = new Circle(new Vector2(45f, 20f), 10f);
                Circle c3 = new Circle(new Vector2(90f, 90f), 20f);
                Vector2 q1 = new Vector2(0f, 0f);
                Vector2 q2 = new Vector2(30f, 10f);
                Vector2 q3 = new Vector2(20f, 90f);
                Vector2 q4 = new Vector2(10f, 90f);
                Vector2 q5 = new Vector2(-10f, 20f);
                Vector2 q6 = new Vector2(20f, 20f);
                Vector2 q7 = new Vector2(30f, 20f);
                Vector2 q8 = new Vector2(25f, 30f);
                Vector2 q9 = new Vector2(20f, 120f);
                Vector2 q10 = new Vector2(30f, 120f);
                Vector2 q11 = new Vector2(25f, 130f);
                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4, q5 });
                Polygon poly2 = new Polygon(new Vector2[] { q6, q7, q8 });
                Polygon poly3 = new Polygon(new Vector2[] { q9, q10, q11 });
                Everything e1 = new Everything();

                // Everything vs. anything
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)e1, (IGeomPrimitive)e1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)e1, (IGeomPrimitive)p1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)p2, (IGeomPrimitive)e1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)e1, (IGeomPrimitive)c1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)c2, (IGeomPrimitive)e1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)e1, (IGeomPrimitive)poly1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)poly2, (IGeomPrimitive)e1));

                // Point-point
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)p1, (IGeomPrimitive)p1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)p1, (IGeomPrimitive)new Point(new Vector2(10f, 10f))));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)p1, (IGeomPrimitive)p2));

                // Point-circle
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)p1, (IGeomPrimitive)c1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)c1, (IGeomPrimitive)p1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)p2, (IGeomPrimitive)c1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)c1, (IGeomPrimitive)p2));

                // Point-polygon
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)p1, (IGeomPrimitive)poly1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)poly1, (IGeomPrimitive)p1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)p2, (IGeomPrimitive)poly1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)poly1, (IGeomPrimitive)p2));

                // Circle-circle
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)c1, (IGeomPrimitive)c2));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)c2, (IGeomPrimitive)c1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)c1, (IGeomPrimitive)c3));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)c3, (IGeomPrimitive)c1));

                // Circle-polygon
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)c1, (IGeomPrimitive)poly1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)poly1, (IGeomPrimitive)c1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)c2, (IGeomPrimitive)poly2));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)poly2, (IGeomPrimitive)c2));

                // Polygon-polygon
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)poly1, (IGeomPrimitive)poly2));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)poly2, (IGeomPrimitive)poly1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)poly1, (IGeomPrimitive)poly3));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)poly3, (IGeomPrimitive)poly1));
            }

            /// <summary>
            /// Tests circle-circle intersections.
            /// </summary>
            [Test]
            public void TestIntersectCircleCircle()
            {
                Vector2 p1 = new Vector2(10f, 10f);
                Vector2 p2 = new Vector2(10f, 30f);
                Vector2 p3 = new Vector2(50f, 50f);

                Circle c1_0 = new Circle(p1, 0);
                Circle c2_0 = new Circle(p2, 0);
                Circle c1_10 = new Circle(p1, 10f);
                Circle c2_10 = new Circle(p2, 10f);
                Circle c2_20 = new Circle(p2, 20f);
                Circle c2_50 = new Circle(p2, 50f);
                Circle c2_10000 = new Circle(p2, 10000f);
                Circle c3_10 = new Circle(p3, 10f);

                // Dot vs. dot
                Assert.IsTrue(Geometry.Intersect(c1_0, c1_0));  // same dot
                Assert.IsFalse(Geometry.Intersect(c1_0, c2_0)); // different dot

                // Dot vs. circle
                Assert.IsFalse(Geometry.Intersect(c1_0, c3_10));   // dot outside circle
                Assert.IsTrue(Geometry.Intersect(c1_0, c2_20));    // dot on circle edge
                Assert.IsTrue(Geometry.Intersect(c1_0, c2_10000)); // dot inside circle

                // Circle vs. circle
                Assert.IsFalse(Geometry.Intersect(c1_10, c3_10));  // disjoint circles
                Assert.IsTrue(Geometry.Intersect(c1_10, c2_10));   // only edges intersect
                Assert.IsTrue(Geometry.Intersect(c2_50, c3_10));   // circle interiors intersect only partly
                Assert.IsTrue(Geometry.Intersect(c2_10, c2_50));   // one circle is strictly inside the other
                Assert.IsTrue(Geometry.Intersect(c2_20, c2_20));   // the circles are the same
            }

            /// <summary>
            /// Tests intersection of line segments with returned intersection point.
            /// </summary>
            [Test]
            public void TestIntersectLinePoint()
            {
                Vector2 p1 = new Vector2(10f, 10f);
                Vector2 p2 = new Vector2(40f, 50f);
                Vector2 p3 = new Vector2(20f, 60f);
                Vector2 p4 = new Vector2(50f, -10f);
                Vector2 p5 = new Vector2(30f, 20f);
                Vector2 p6 = p1 + (p1 - p2);
                Vector2 p7 = p2 + (p2 - p1);
                Vector2 p8 = p2 + new Vector2(1000f, -1000f);
                Vector2 p9 = p1 + new Vector2(1000f, -1000f);
                Vector2 p10 = new Vector2(-10f, 0f);
                Vector2 p11 = new Vector2(10f, 0f);
                Vector2 p12 = new Vector2(0f, -10f);
                Vector2 p13 = new Vector2(0f, 10f);
                Vector2? cross = new Vector2?();

                // General cases
                cross = p3; // to reveal possible errors in next call
                Assert.IsTrue(Geometry.Intersect(p10, p11, p12, p13, ref cross)); // orthogonal crossing
                Assert.AreEqual(cross, Vector2.Zero);
                Assert.IsTrue(Geometry.Intersect(p1, p2, p3, p4, ref cross));     // general crossing
                Assert.IsFalse(Geometry.Intersect(p1, p4, p2, p3, ref cross));    // lines cross but segments don't
                Assert.IsFalse(Geometry.Intersect(p1, p5, p3, p4, ref cross));    // line crosses segment, segments don't

                // Special cases
                Assert.IsTrue(Geometry.Intersect(p1, p2, p2, p3, ref cross));  // Endpoints connect
                Assert.AreEqual(cross, p2);
                Assert.IsTrue(Geometry.Intersect(p1, p3, p2, p3, ref cross));  // Endpoints connect
                Assert.AreEqual(cross, p3);
                Assert.IsFalse(Geometry.Intersect(p1, p2, p8, p9, ref cross)); // parallel lines far away
                Assert.IsTrue(Geometry.Intersect(p1, p2, p6, p7, ref cross)); // one line contained in another
                Assert.IsNull(cross, "Got one intersection point instead of infinitely many");
                cross = p3; // to reveal possible errors in next call
                Assert.IsTrue(Geometry.Intersect(p1, p2, p1, p2, ref cross)); // the same line segment
                Assert.IsNull(cross, "Got one intersection point instead of infinitely many");
            }

            /// <summary>
            /// Tests intersection of line segments with returned intersection type.
            /// </summary>
            [Test]
            public void TestIntersectLineType()
            {
                Vector2 p1 = new Vector2(10f, 10f);
                Vector2 p2 = new Vector2(40f, 50f);
                Vector2 p3 = new Vector2(20f, 60f);
                Vector2 p4 = new Vector2(50f, -10f);
                Vector2 p5 = new Vector2(30f, 20f);
                Vector2 p6 = p1 + (p1 - p2);
                Vector2 p7 = p2 + (p2 - p1);
                Vector2 p8 = p2 + new Vector2(1000f, -1000f);
                Vector2 p9 = p1 + new Vector2(1000f, -1000f);
                Vector2 p10 = new Vector2(-10f, 0f);
                Vector2 p11 = new Vector2(10f, 0f);
                Vector2 p12 = new Vector2(0f, -10f);
                Vector2 p13 = new Vector2(0f, 10f);

                // General cases
                Assert.AreEqual(Geometry.Intersect(p10, p11, p12, p13), Geometry.LineIntersectionType.Point); // orthogonal crossing
                Assert.AreEqual(Geometry.Intersect(p1, p2, p3, p4), Geometry.LineIntersectionType.Point);     // general crossing
                Assert.AreEqual(Geometry.Intersect(p1, p4, p2, p3), Geometry.LineIntersectionType.None);      // lines cross but segments don't
                Assert.AreEqual(Geometry.Intersect(p1, p5, p3, p4), Geometry.LineIntersectionType.None);      // line crosses segment, segments don't

                // Special cases
                Assert.AreEqual(Geometry.Intersect(p1, p2, p2, p3), Geometry.LineIntersectionType.Point);  // Endpoints connect
                Assert.AreEqual(Geometry.Intersect(p1, p3, p2, p3), Geometry.LineIntersectionType.Point);  // Endpoints connect
                Assert.AreEqual(Geometry.Intersect(p1, p2, p8, p9), Geometry.LineIntersectionType.None); // parallel lines far away
                Assert.AreEqual(Geometry.Intersect(p1, p2, p6, p7), Geometry.LineIntersectionType.Segment); // one line contained in another
                Assert.AreEqual(Geometry.Intersect(p1, p2, p1, p2), Geometry.LineIntersectionType.Segment); // the same line
            }

            /// <summary>
            /// Tests point-polygon intersections
            /// </summary>
            [Test]
            public void TestIntersectPointPolygon()
            {
                Point p1 = new Point(new Vector2(-10f, -10f));
                Point p2 = new Point(new Vector2(10f, 20f));
                Point p3 = new Point(new Vector2(50f, 20f));
                Point p4 = new Point(new Vector2(25f, 20f));
                Point p5 = new Point(new Vector2(10f, 200f));

                Vector2 q1 = new Vector2(10f, 10f);
                Vector2 q2 = new Vector2(100f, 10f);
                Vector2 q3 = new Vector2(100f, 100f);
                Vector2 q4 = new Vector2(10f, 100f);
                Vector2 q5 = new Vector2(25f, 70f);
                Vector2 q6 = new Vector2(50f, 10f);
                Vector2 q7 = new Vector2(75f, 70f);

                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4 });
                Polygon poly2 = new Polygon(new Vector2[] { q1, q5, q6, q7, q2, q3, q4 });

                // Point and square; out, on boundary, in
                Assert.IsFalse(Geometry.Intersect(p1, poly1));
                Assert.IsTrue(Geometry.Intersect(p2, poly1));
                Assert.IsTrue(Geometry.Intersect(p3, poly1));
                Assert.IsTrue(Geometry.Intersect(p4, poly1));
                Assert.IsFalse(Geometry.Intersect(p5, poly1));

                // Point and concave polygon
                Assert.IsFalse(Geometry.Intersect(p1, poly2));
                Assert.IsTrue(Geometry.Intersect(p2, poly2));
                Assert.IsTrue(Geometry.Intersect(p3, poly2));
                Assert.IsFalse(Geometry.Intersect(p4, poly2));
                Assert.IsFalse(Geometry.Intersect(p5, poly2));

            }

            /// <summary>
            /// Tests circle-polygon intersections
            /// </summary>
            [Test]
            public void TestIntersectCirclePolygon()
            {
                float delta = 0.0002f; // slight margin in favour of intersection
                Circle c1 = new Circle(new Vector2(30f, 10f), 20f);
                Circle c2 = new Circle(new Vector2(10f, 0f), 5f);
                Circle c3 = new Circle(new Vector2(-90f, -90f), 20f);
                Circle c4 = new Circle(new Vector2(20f, 50f), 5f);
                Circle c5 = new Circle(new Vector2(20f, 70f), 10f + delta);
                Circle c6 = new Circle(new Vector2(15f, 100f), 10f + delta);
                Circle c7 = new Circle(new Vector2(15f, 90f), 0f + delta);
                Circle c8 = new Circle(new Vector2(0f, 60f), 5f);
                Circle c9 = new Circle(new Vector2(10f, 85f), 10f);
                Vector2 q1 = new Vector2(0f, 0f);
                Vector2 q2 = new Vector2(30f, 10f);
                Vector2 q3 = new Vector2(20f, 90f);
                Vector2 q4 = new Vector2(10f, 90f);
                Vector2 q5 = new Vector2(0f, 20f);
                Vector2 q6 = new Vector2(-10f, 70f);
                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4, q5, q6 });

                Assert.IsTrue(Geometry.Intersect(c1, poly1)); // circle centered at a vertex 
                Assert.IsTrue(Geometry.Intersect(c2, poly1)); // circle centered out, intersects
                Assert.IsFalse(Geometry.Intersect(c3, poly1)); // circle totally out
                Assert.IsTrue(Geometry.Intersect(c4, poly1)); // circle totally in
                Assert.IsTrue(Geometry.Intersect(c5, poly1)); // circle centered out, only edge intersects a vertex
                Assert.IsTrue(Geometry.Intersect(c6, poly1)); // circle centered out, only edge intersects edge
                Assert.IsTrue(Geometry.Intersect(c7, poly1)); // circle centered on edge, zero size
                Assert.IsFalse(Geometry.Intersect(c8, poly1)); // circle totally out, in polygon's "armpit"
                Assert.IsTrue(Geometry.Intersect(c9, poly1)); // circle centered in, intersects also complement
            }

            /// <summary>
            /// Tests polygon-polygon intersections
            /// </summary>
            [Test]
            public void TestIntersectPolygonPolygon()
            {
                Vector2 q1 = new Vector2(0f, 0f);
                Vector2 q2 = new Vector2(30f, 10f);
                Vector2 q3 = new Vector2(20f, 90f);
                Vector2 q4 = new Vector2(10f, 90f);
                Vector2 q5 = new Vector2(0f, 20f);
                Vector2 q6 = new Vector2(-10f, 70f);
                Vector2 w1 = new Vector2(-10f, -10f);
                Vector2 w2 = new Vector2(-20f, 0f);
                Vector2 w3 = new Vector2(20f, 20f);
                Vector2 w4 = new Vector2(60f, 50f);
                Vector2 w5 = new Vector2(30f, -20f);
                Vector2 w6 = new Vector2(25f, 40f);
                Vector2 w7 = new Vector2(20f, 70f);
                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4, q5, q6 });
                Polygon poly2 = new Polygon(new Vector2[] { q1, w1, w2 });
                Polygon poly3 = new Polygon(new Vector2[] { w3, w4, w5 });
                Polygon poly4 = new Polygon(new Vector2[] { w3, w6, w7 });
                Polygon poly5 = new Polygon(new Vector2[] { w4, w2, w5 });

                Assert.IsTrue(Geometry.Intersect(poly1, poly2)); // only one vertex intersects
                Assert.IsTrue(Geometry.Intersect(poly1, poly3)); // vertex in polygon
                Assert.IsFalse(Geometry.Intersect(poly2, poly3)); // no intersection
                Assert.IsTrue(Geometry.Intersect(poly1, poly4)); // polygon in polygon
                Assert.IsTrue(Geometry.Intersect(poly1, poly5)); // vertices out, edge intersects polygon
            }

            /// <summary>
            /// Tests point to line segment distance.
            /// </summary>
            [Test]
            public void TestDistancePointSegment()
            {
                Vector2 q1 = new Vector2(10, 10);
                Vector2 q2 = new Vector2(20, 30);
                Vector2 q3 = new Vector2(50, 30);
                Vector2 q4 = new Vector2(20, -30);
                Point p1 = new Point(new Vector2(10, 10));
                Point p2 = new Point(new Vector2(20, 30));
                Point p3 = new Point(new Vector2(15, 20));
                Point p4 = new Point(new Vector2(20, 10));
                Point p5 = new Point(new Vector2(10, 30));
                Point p6 = new Point(new Vector2(10, 40));
                Point p7 = new Point(new Vector2(0, 0));
                Point p8 = new Point(new Vector2(40, -10));
                Point p9 = new Point(new Vector2(20, 1000));
                Point p10 = new Point(new Vector2(1000, 30));
                float delta = 0.0001f; // amount of acceptable error

                // Points on line segment
                Assert.AreEqual(Distance(p1, q1, q2), 0f, delta); // endpoint
                Assert.AreEqual(Distance(p2, q1, q2), 0f, delta); // endpoint
                Assert.AreEqual(Distance(p3, q1, q2), 0f, delta); // middle point

                // Point out of line segment, projects inside line segment
                Assert.AreEqual(Distance(p4, q1, q2), 10 * 2 / Math.Sqrt(5), delta);
                Assert.AreEqual(Distance(p5, q1, q2), 10 * 2 / Math.Sqrt(5), delta);

                // Point out of line segment, projects out of line segment
                Assert.AreEqual(Distance(p6, q1, q2), 10 * Math.Sqrt(2), delta);
                Assert.AreEqual(Distance(p7, q1, q2), 10 * Math.Sqrt(2), delta);

                // Point out of line segment, projects on an endpoint
                Assert.AreEqual(Distance(p4, q2, q3), 20, delta);
                Assert.AreEqual(Distance(p9, q2, q3), 970, delta);
                Assert.AreEqual(Distance(p5, q2, q4), 10, delta);
                Assert.AreEqual(Distance(p10, q2, q4), 980, delta);

                // Vertical line segment, point out of line segment, projects to line segment
                Assert.AreEqual(Distance(p8, q2, q3), 40, delta);

                // Horizontal line segment, point out of line segment, projects to line segment
                Assert.AreEqual(Distance(p7, q2, q4), 20, delta);
            }

            /// <summary>
            /// Tests closest point of a line segment with respect to a point.
            /// </summary>
            [Test]
            public void TestGetClosestPointPointSegment()
            {
                Vector2 q1 = new Vector2(10, 10);
                Vector2 q2 = new Vector2(20, 30);
                Vector2 q3 = new Vector2(50, 30);
                Vector2 q4 = new Vector2(20, -30);
                Point p1 = new Point(new Vector2(10, 10));
                Point p2 = new Point(new Vector2(20, 30));
                Point p3 = new Point(new Vector2(15, 20));
                Point p4 = new Point(new Vector2(20, 10));
                Point p5 = new Point(new Vector2(10, 30));
                Point p6 = new Point(new Vector2(10, 40));
                Point p7 = new Point(new Vector2(0, 0));
                Point p8 = new Point(new Vector2(40, -10));
                Point p9 = new Point(new Vector2(20, 1000));
                Point p10 = new Point(new Vector2(1000, 30));
                Point r1 = new Point(new Vector2(12, 14));
                Point r2 = new Point(new Vector2(18, 26));
                Point r3 = new Point(new Vector2(40, 30));
                Point r4 = new Point(new Vector2(20, 0));

                // Points on line segment
                Assert.AreEqual(GetClosestPoint(q1, q2, p1), p1); // endpoint
                Assert.AreEqual(GetClosestPoint(q1, q2, p2), p2); // endpoint
                Assert.AreEqual(GetClosestPoint(q1, q2, p3), p3); // middle point

                // Point out of line segment, projects inside line segment
                Assert.AreEqual(GetClosestPoint(q1, q2, p4), r1);
                Assert.AreEqual(GetClosestPoint(q1, q2, p5), r2);

                // Point out of line segment, projects out of line segment
                Assert.AreEqual(GetClosestPoint(q1, q2, p6), p2);
                Assert.AreEqual(GetClosestPoint(q1, q2, p7), p1);

                // Point out of line segment, projects on an endpoint
                Assert.AreEqual(GetClosestPoint(q2, q3, p4), p2);
                Assert.AreEqual(GetClosestPoint(q2, q3, p9), p2);
                Assert.AreEqual(GetClosestPoint(q2, q4, p5), p2);
                Assert.AreEqual(GetClosestPoint(q2, q4, p10), p2);

                // Vertical line segment, point out of line segment, projects to line segment
                Assert.AreEqual(GetClosestPoint(q2, q3, p8), r3);

                // Horizontal line segment, point out of line segment, projects to line segment
                Assert.AreEqual(GetClosestPoint(q2, q4, p7), r4);
            }

            /// <summary>
            /// Tests point to polygon distance.
            /// </summary>
            [Test]
            public void TestDistancePointPolygon()
            {
                Vector2 q1 = new Vector2(10f, 10f);
                Vector2 q2 = new Vector2(100f, 10f);
                Vector2 q3 = new Vector2(100f, 100f);
                Vector2 q4 = new Vector2(10f, 100f);
                Vector2 q5 = new Vector2(35f, 35f);
                Vector2 q6 = new Vector2(60f, 10f);

                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4 });
                Polygon poly2 = new Polygon(new Vector2[] { q1, q5, q6, q2, q3, q4 });

                Point p1 = new Point(new Vector2(50, 50));
                Point p2 = new Point(new Vector2(100, 100));
                Point p3 = new Point(new Vector2(10, 20));
                Point p4 = new Point(new Vector2(0, 0));
                Point p5 = new Point(new Vector2(0, 120));
                Point p6 = new Point(new Vector2(130, 100));
                Point p7 = new Point(new Vector2(120, -20));
                Point p8 = new Point(new Vector2(70, 0));
                Point p9 = new Point(new Vector2(35, -15));
                Point p10 = new Point(new Vector2(35, 30));

                float delta = 0.0001f; // acceptable error margin

                // Point in polygon
                Assert.AreEqual(Distance(p1, poly1), 0, delta); // inside
                Assert.AreEqual(Distance(p2, poly1), 0, delta); // on vertex
                Assert.AreEqual(Distance(p3, poly1), 0, delta); // on edge

                // Point out of polygon, convex polygon
                Assert.AreEqual(Distance(p4, poly1), 10 * Math.Sqrt(2), delta); // closest point is a vertex
                Assert.AreEqual(Distance(p5, poly1), (new Vector2(-10, 20)).Length(), delta); // ditto
                Assert.AreEqual(Distance(p6, poly1), 30, delta); // ditto
                Assert.AreEqual(Distance(p7, poly1), (new Vector2(20, -30)).Length(), delta); // ditto
                Assert.AreEqual(Distance(p8, poly1), 10, delta); // closest point is on edge

                // Point out of polygon, concave polygon
                Assert.AreEqual(Distance(p9, poly2), 25 * Math.Sqrt(2), delta); // ambiguous normal from two vertices
                Assert.AreEqual(Distance(p10, poly2), 5.0 / 2.0 * Math.Sqrt(2), delta); // ambiguous normal from two edges
            }

            /// <summary>
            /// Tests getting a normal for a polygon.
            /// </summary>
            [Test]
            public void TestGetNormalPolygon()
            {
                Vector2 q1 = new Vector2(10f, 10f);
                Vector2 q2 = new Vector2(100f, 10f);
                Vector2 q3 = new Vector2(100f, 100f);
                Vector2 q4 = new Vector2(10f, 100f);
                Vector2 q5 = new Vector2(35f, 35f);
                Vector2 q6 = new Vector2(60f, 10f);

                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4 });
                Polygon poly2 = new Polygon(new Vector2[] { q1, q5, q6, q2, q3, q4 });

                Point p1 = new Point(new Vector2(50, 50));
                Point p2 = new Point(new Vector2(100, 100));
                Point p3 = new Point(new Vector2(10, 20));
                Point p4 = new Point(new Vector2(0, 0));
                Point p5 = new Point(new Vector2(0, 120));
                Point p6 = new Point(new Vector2(130, 100));
                Point p7 = new Point(new Vector2(120, -20));
                Point p8 = new Point(new Vector2(70, 0));
                Point p9 = new Point(new Vector2(35, -15));
                Point p10 = new Point(new Vector2(35, 30));

                // Point in polygon
                Assert.AreEqual(GetNormal(poly1, p1), Vector2.Zero); // inside
                Assert.AreEqual(GetNormal(poly1, p2), Vector2.Zero); // on vertex
                Assert.AreEqual(GetNormal(poly1, p3), Vector2.Zero); // on edge

                // Point out of polygon, convex polygon
                Assert.AreEqual(GetNormal(poly1, p4), Vector2.Normalize(-Vector2.One)); // closest point is a vertex
                Assert.AreEqual(GetNormal(poly1, p5), Vector2.Normalize(new Vector2(-10, 20))); // ditto
                Assert.AreEqual(GetNormal(poly1, p6), Vector2.UnitX); // ditto
                Assert.AreEqual(GetNormal(poly1, p7), Vector2.Normalize(new Vector2(20, -30))); // ditto
                Assert.AreEqual(GetNormal(poly1, p8), -Vector2.UnitY); // closest point is on edge

                // Point out of polygon, concave polygon
                Assert.IsTrue(GetNormal(poly2, p9).Equals(Vector2.Normalize(-Vector2.One)) ||
                              GetNormal(poly2, p9).Equals(Vector2.Normalize(new Vector2(1, -1)))); // ambiguous normal from two vertices
                Assert.IsTrue(GetNormal(poly2, p10).Equals(Vector2.Normalize(-Vector2.One)) ||
                              GetNormal(poly2, p10).Equals(Vector2.Normalize(new Vector2(1, -1)))); // ambiguous normal from two edges
            }

            /// <summary>
            /// Tests translation of cartesian coordinates into barycentric coordinates.
            /// </summary>
            [Test]
            public void TestBarycentric()
            {
                Vector2 v1 = new Vector2(10f, 10f);
                Vector2 v2 = new Vector2(50f, 10f);
                Vector2 v3 = new Vector2(10f, 50f);
                Vector2 v4 = new Vector2(70f, 10f);
                Vector2 p1 = new Vector2(20f, 20f);
                float amount2, amount3;

                // Coordinates at triangle vertices.
                CartesianToBarycentric(v1, v2, v3, v1, out amount2, out amount3);
                Assert.AreEqual(amount2, 0f);
                Assert.AreEqual(amount3, 0f);
                CartesianToBarycentric(v1, v2, v3, v2, out amount2, out amount3);
                Assert.AreEqual(amount2, 1f);
                Assert.AreEqual(amount3, 0f);
                CartesianToBarycentric(v1, v2, v3, v3, out amount2, out amount3);
                Assert.AreEqual(amount2, 0);
                Assert.AreEqual(amount3, 1f);

                // Coordinates inside the triangle.
                CartesianToBarycentric(v1, v2, v3, p1, out amount2, out amount3);
                Assert.Greater(amount2, 0f);
                Assert.Less(amount2, 1f);
                Assert.Greater(amount3, 0f);
                Assert.Less(amount3, 1f);

                // Reduced triangle. There's no asserts as return values are undefined,
                // but the code shouldn't crash.
                CartesianToBarycentric(v1, v2, v4, v1, out amount2, out amount3);
                CartesianToBarycentric(v1, v2, v4, v2, out amount2, out amount3);
                CartesianToBarycentric(v1, v2, v4, v3, out amount2, out amount3);
                CartesianToBarycentric(v1, v2, v4, p1, out amount2, out amount3);

            }

        }
#endif
        #endregion Unit tests
    }
}
