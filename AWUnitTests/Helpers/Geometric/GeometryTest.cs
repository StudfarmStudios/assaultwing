using System;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace AW2.Helpers.Geometric
{
    [TestFixture]
    public class GeometryTest
    {
        [Test]
        public void TestGeneralIntersect()
        {
            var p1 = new Vector2(10f, 10f);
            var p2 = new Vector2(30f, 90f);
            var c1 = new Circle(new Vector2(20f, 10f), 20f);
            var q1 = new Vector2(0f, 0f);
            var q2 = new Vector2(30f, 10f);
            var q3 = new Vector2(20f, 90f);
            var q4 = new Vector2(10f, 90f);
            var q5 = new Vector2(-10f, 20f);
            var poly1 = new Polygon(new[] { q1, q2, q3, q4, q5 });

            // Point-circle
            Assert.IsTrue(Geometry.Intersect(p1, (IGeomPrimitive)c1));
            Assert.IsFalse(Geometry.Intersect(p2, (IGeomPrimitive)c1));

            // Point-polygon
            Assert.IsTrue(Geometry.Intersect(p1, (IGeomPrimitive)poly1));
            Assert.IsFalse(Geometry.Intersect(p2, (IGeomPrimitive)poly1));
        }

        [Test]
        public void TestIntersectLinePoint()
        {
            var p1 = new Vector2(10f, 10f);
            var p2 = new Vector2(40f, 50f);
            var p3 = new Vector2(20f, 60f);
            var p4 = new Vector2(50f, -10f);
            var p5 = new Vector2(30f, 20f);
            var p6 = p1 + (p1 - p2);
            var p7 = p2 + (p2 - p1);
            var p8 = p2 + new Vector2(1000f, -1000f);
            var p9 = p1 + new Vector2(1000f, -1000f);
            var p10 = new Vector2(-10f, 0f);
            var p11 = new Vector2(10f, 0f);
            var p12 = new Vector2(0f, -10f);
            var p13 = new Vector2(0f, 10f);
            var cross = new Vector2?();

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

        [Test]
        public void TestIntersectPointPolygon()
        {
            var p1 = new Vector2(-10f, -10f);
            var p2 = new Vector2(10f, 20f);
            var p3 = new Vector2(50f, 20f);
            var p4 = new Vector2(25f, 20f);
            var p5 = new Vector2(10f, 200f);

            var q1 = new Vector2(10f, 10f);
            var q2 = new Vector2(100f, 10f);
            var q3 = new Vector2(100f, 100f);
            var q4 = new Vector2(10f, 100f);
            var q5 = new Vector2(25f, 70f);
            var q6 = new Vector2(50f, 10f);
            var q7 = new Vector2(75f, 70f);

            var poly1 = new Polygon(new[] { q1, q2, q3, q4 });
            var poly2 = new Polygon(new[] { q1, q5, q6, q7, q2, q3, q4 });

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

        [Test]
        public void TestIntersectPointTriangle()
        {
            var p1 = new Vector2(0f, 0f);
            var p2 = new Vector2(19.9999f, 49.9999f);
            var p3 = new Vector2(10f, 0f);
            var p4 = new Vector2(0f, -20f);
            var v1 = new Vector2(0f, -10f);
            var v2 = new Vector2(20f, 50f);
            var v3 = new Vector2(-50f, 70f);
            var t1 = new Triangle(v1, v2, v3);
            Assert.IsTrue(Geometry.Intersect(p1, t1));
            Assert.IsTrue(Geometry.Intersect(p2, t1));
            Assert.IsFalse(Geometry.Intersect(p3, t1));
            Assert.IsFalse(Geometry.Intersect(p4, t1));
        }

        private void AssertAreInDelta(Vector3 expected, Vector3 actual, float delta)
        {
            var difference = Vector3.Distance(expected, actual);
            var message = string.Format("Expected: {0}\nBut was:  {1}\nWith delta: {2}", expected, actual, delta);
            Assert.That(difference < delta, message);
        }

        private void AssertAreInDelta(Vector2 expected, Vector2 actual, float delta)
        {
            var difference = Vector2.Distance(expected, actual);
            var message = string.Format("Expected: {0}\nBut was:  {1}\nWith delta: {2}", expected, actual, delta);
            Assert.That(difference < delta, message);
        }

        [Test]
        public void TestCropLineSegment()
        {
            var min = new Vector2(-10, -10);
            var max = new Vector2(10, 10);
            var a = new Vector2(0, 0);
            var b = new Vector2(-5, 9);
            var c = new Vector2(20, 6);
            var a_c = new Vector2(10, 3);
            var d = new Vector2(6, 20);
            var a_d = new Vector2(3, 10);
            var e = new Vector2(-20, 6);
            var a_e = new Vector2(-10, 3);
            var f = new Vector2(-6, -20);
            var a_f = new Vector2(-3, -10);
            Assert.AreEqual(b, Geometry.CropLineSegment(a, b, min, max));
            Assert.AreEqual(a, Geometry.CropLineSegment(a, a, min, max));
            Assert.AreEqual(max, Geometry.CropLineSegment(a, max, min, max));
            Assert.AreEqual(min, Geometry.CropLineSegment(a, min, min, max));
            Assert.Throws<ArgumentException>(() => Geometry.CropLineSegment(min, max, a, a));
            Assert.Throws<ArgumentException>(() => Geometry.CropLineSegment(a, a, a, max));
            Assert.Throws<ArgumentException>(() => Geometry.CropLineSegment(a, b, max, min));
            Assert.AreEqual(a_c, Geometry.CropLineSegment(a, c, min, max));
            Assert.AreEqual(a_d, Geometry.CropLineSegment(a, d, min, max));
            Assert.AreEqual(a_e, Geometry.CropLineSegment(a, e, min, max));
            Assert.AreEqual(a_f, Geometry.CropLineSegment(a, f, min, max));
        }

        [Test]
        public void TestGeneralDistance()
        {
            var prims = new IGeomPrimitive[]
            {
                new Circle(new Vector2(30, 40), 50),
                new Rectangle(60, 70, 80, 90),
                new Triangle(new Vector2(10, 60), new Vector2(-90, -10), new Vector2(-30, 20)),
                new Polygon(new[] { new Vector2(15, 25), new Vector2(35, 45), new Vector2(55, 65) }),
            };
            foreach (var prim2 in prims)
                Assert.LessOrEqual(0, Geometry.Distance(new Vector2(10, 20), prim2));
        }

        [Test]
        public void TestDistancePointCircle()
        {
            var p1 = new Vector2(-9, 10);
            var p2 = new Vector2(10, 30);
            var p3 = new Vector2(11, 30);
            var p4 = new Vector2(-990, -2990);
            var c1 = new Circle(new Vector2(10, 10), 20);
            var delta = 0.0001f; // amount of acceptable error
            Assert.AreEqual(0, Geometry.Distance(p1, c1), delta); // interior
            Assert.AreEqual(0, Geometry.Distance(p2, c1), delta); // edge
            Assert.AreEqual(Math.Sqrt(1 * 1 + 20 * 20) - 20, Geometry.Distance(p3, c1), delta); // exterior near
            Assert.AreEqual(Math.Sqrt(1000 * 1000 + 3000 * 3000) - 20, Geometry.Distance(p4, c1), delta); // exterior far
        }

        [Test]
        public void TestDistancePointRectangle()
        {
            var p1 = new Vector2(0, 0);
            var p2 = new Vector2(-20, 20);
            var p3 = new Vector2(30, -5);
            var p4 = new Vector2(40, 30);
            var p5 = new Vector2(0, -5001);
            var r1 = new Rectangle(-20, -10, 30, 20);
            var delta = 0.0001f; // amount of acceptable error
            Assert.AreEqual(0, Geometry.Distance(p1, r1), delta); // interior
            Assert.AreEqual(0, Geometry.Distance(p2, r1), delta); // vertex
            Assert.AreEqual(0, Geometry.Distance(p3, r1), delta); // edge
            Assert.AreEqual(10 * Math.Sqrt(2), Geometry.Distance(p4, r1), delta); // closest to vertex
            Assert.AreEqual(4991, Geometry.Distance(p5, r1), delta); // closest to edge
        }

        [Test]
        public void TestDistancePointTriangle()
        {
            var q1 = new Vector2(30, 30);
            var q2 = new Vector2(-20, 20);
            var q3 = new Vector2(10, -30);
            var p1 = new Vector2(10, 10);
            var p2 = new Vector2(30, 30);
            var p3 = new Vector2(5, 25);
            var p4 = new Vector2(50, 50);
            var p5 = new Vector2(5 - 10, 25 + 50);
            var t1 = new Triangle(q1, q2, q3);
            var delta = 0.0001f; // amount of acceptable error

            // Point in triangle
            Assert.AreEqual(0, Geometry.Distance(p1, t1), delta); // interior
            Assert.AreEqual(0, Geometry.Distance(p2, t1), delta); // vertex
            Assert.AreEqual(0, Geometry.Distance(p3, t1), delta); // edge

            // Point outside triangle
            Assert.AreEqual(20 * Math.Sqrt(2), Geometry.Distance(p4, t1), delta); // closest to vertex
            Assert.AreEqual(Math.Sqrt(10 * 10 + 50 * 50), Geometry.Distance(p5, t1), delta); // closest to edge
        }

        [Test]
        public void TestGetClosestPointPointSegment()
        {
            var q1 = new Vector2(10, 10);
            var q2 = new Vector2(20, 30);
            var q3 = new Vector2(50, 30);
            var q4 = new Vector2(20, -30);
            var p1 = new Vector2(10, 10);
            var p2 = new Vector2(20, 30);
            var p3 = new Vector2(15, 20);
            var p4 = new Vector2(20, 10);
            var p5 = new Vector2(10, 30);
            var p6 = new Vector2(10, 40);
            var p7 = new Vector2(0, 0);
            var p8 = new Vector2(40, -10);
            var p9 = new Vector2(20, 1000);
            var p10 = new Vector2(1000, 30);
            var r1 = new Vector2(12, 14);
            var r2 = new Vector2(18, 26);
            var r3 = new Vector2(40, 30);
            var r4 = new Vector2(20, 0);

            var delta = 0.0001f; // acceptable error margin

            // Points on line segment
            AssertAreInDelta(p1, Geometry.GetClosestPoint(q1, q2, p1), delta); // endpoint
            AssertAreInDelta(p2, Geometry.GetClosestPoint(q1, q2, p2), delta); // endpoint
            AssertAreInDelta(p3, Geometry.GetClosestPoint(q1, q2, p3), delta); // middle point

            // Point out of line segment, projects inside line segment
            AssertAreInDelta(r1, Geometry.GetClosestPoint(q1, q2, p4), delta);
            AssertAreInDelta(r2, Geometry.GetClosestPoint(q1, q2, p5), delta);

            // Point out of line segment, projects out of line segment
            AssertAreInDelta(p2, Geometry.GetClosestPoint(q1, q2, p6), delta);
            AssertAreInDelta(p1, Geometry.GetClosestPoint(q1, q2, p7), delta);

            // Point out of line segment, projects on an endpoint
            AssertAreInDelta(p2, Geometry.GetClosestPoint(q2, q3, p4), delta);
            AssertAreInDelta(p2, Geometry.GetClosestPoint(q2, q3, p9), delta);
            AssertAreInDelta(p2, Geometry.GetClosestPoint(q2, q4, p5), delta);
            AssertAreInDelta(p2, Geometry.GetClosestPoint(q2, q4, p10), delta);

            // Vertical line segment, point out of line segment, projects to line segment
            AssertAreInDelta(r3, Geometry.GetClosestPoint(q2, q3, p8), delta);

            // Horizontal line segment, point out of line segment, projects to line segment
            AssertAreInDelta(r4, Geometry.GetClosestPoint(q2, q4, p7), delta);
        }

        [Test]
        public void TestDistancePointPolygon()
        {
            var q1 = new Vector2(10f, 10f);
            var q2 = new Vector2(100f, 10f);
            var q3 = new Vector2(100f, 100f);
            var q4 = new Vector2(10f, 100f);
            var q5 = new Vector2(35f, 35f);
            var q6 = new Vector2(60f, 10f);

            var poly1 = new Polygon(new[] { q1, q2, q3, q4 });
            var poly2 = new Polygon(new[] { q1, q5, q6, q2, q3, q4 });

            var p1 = new Vector2(50, 50);
            var p2 = new Vector2(100, 100);
            var p3 = new Vector2(10, 20);
            var p4 = new Vector2(0, 0);
            var p5 = new Vector2(0, 120);
            var p6 = new Vector2(130, 100);
            var p7 = new Vector2(120, -20);
            var p8 = new Vector2(70, 0);
            var p9 = new Vector2(35, -15);
            var p10 = new Vector2(35, 30);

            var delta = 0.0001f; // acceptable error margin

            // Point in polygon
            Assert.AreEqual(Geometry.Distance(p1, poly1), 0, delta); // inside
            Assert.AreEqual(Geometry.Distance(p2, poly1), 0, delta); // on vertex
            Assert.AreEqual(Geometry.Distance(p3, poly1), 0, delta); // on edge

            // Point out of polygon, convex polygon
            Assert.AreEqual(Geometry.Distance(p4, poly1), 10 * Math.Sqrt(2), delta); // closest point is a vertex
            Assert.AreEqual(Geometry.Distance(p5, poly1), (new Vector2(-10, 20)).Length(), delta); // ditto
            Assert.AreEqual(Geometry.Distance(p6, poly1), 30, delta); // ditto
            Assert.AreEqual(Geometry.Distance(p7, poly1), (new Vector2(20, -30)).Length(), delta); // ditto
            Assert.AreEqual(Geometry.Distance(p8, poly1), 10, delta); // closest point is on edge

            // Point out of polygon, concave polygon
            Assert.AreEqual(Geometry.Distance(p9, poly2), 25 * Math.Sqrt(2), delta); // ambiguous normal from two vertices
            Assert.AreEqual(Geometry.Distance(p10, poly2), 5.0 / 2.0 * Math.Sqrt(2), delta); // ambiguous normal from two edges
        }

        [Test]
        public void TestBarycentric()
        {
            var v1 = new Vector2(10f, 10f);
            var v2 = new Vector2(50f, 10f);
            var v3 = new Vector2(10f, 50f);
            var v4 = new Vector2(70f, 10f);
            var p1 = new Vector2(20f, 20f);
            float amount2, amount3;

            // Coordinates at triangle vertices.
            Geometry.CartesianToBarycentric(v1, v2, v3, v1, out amount2, out amount3);
            Assert.AreEqual(amount2, 0f);
            Assert.AreEqual(amount3, 0f);
            Geometry.CartesianToBarycentric(v1, v2, v3, v2, out amount2, out amount3);
            Assert.AreEqual(amount2, 1f);
            Assert.AreEqual(amount3, 0f);
            Geometry.CartesianToBarycentric(v1, v2, v3, v3, out amount2, out amount3);
            Assert.AreEqual(amount2, 0);
            Assert.AreEqual(amount3, 1f);

            // Coordinates inside the triangle.
            Geometry.CartesianToBarycentric(v1, v2, v3, p1, out amount2, out amount3);
            Assert.Greater(amount2, 0f);
            Assert.Less(amount2, 1f);
            Assert.Greater(amount3, 0f);
            Assert.Less(amount3, 1f);

            // Reduced triangle. There's no asserts as return values are undefined,
            // but the code shouldn't crash.
            Geometry.CartesianToBarycentric(v1, v2, v4, v1, out amount2, out amount3);
            Geometry.CartesianToBarycentric(v1, v2, v4, v2, out amount2, out amount3);
            Geometry.CartesianToBarycentric(v1, v2, v4, v3, out amount2, out amount3);
            Geometry.CartesianToBarycentric(v1, v2, v4, p1, out amount2, out amount3);

        }
    }
}
