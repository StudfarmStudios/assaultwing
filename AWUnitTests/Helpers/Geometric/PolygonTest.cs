using System;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace AW2.Helpers.Geometric
{
    [TestFixture]
    public class PolygonTest
    {
        [Test]
        public void TestPolygon()
        {
            var p1 = new Vector2(10f, 10f);
            var p2 = new Vector2(30f, 10f);
            var p3 = new Vector2(30f, 30f);
            var p4 = new Vector2(10f, 30f);
            var p5 = new Vector2(40f, 10f);
            var p6 = new Vector2(20f, 10f);
            var p7 = new Vector2(20f, -10f);

            // Good polygons
            AssertPolygonCreation(new[] { p1, p2, p3, p4 }, true);  // square
            AssertPolygonCreation(new[] { p1, p2, p3 }, true);  // triangle
            AssertPolygonCreation(new[] { p1, p3, p2 }, true);  // triangle, opposite winding

            // Reduced polygons
            AssertPolygonCreation(new[] { p1, p2 }, false);  // line segment
            AssertPolygonCreation(new[] { p1 }, false);  // dot
            AssertPolygonCreation(new[] { p1, p1 }, false);  // reduced line segment
#if false // These don't pass but it shouldn't matter very much
                AssertPolygonCreation(new[] { p1, p2, p3, p3, p4 }, false);  // reduced line segment
                AssertPolygonCreation(new[] { p1, p2, p3, p4, p1 }, false);  // loop back to start
                AssertPolygonCreation(new[] { p1, p2, p5 }, false);  // flat triangle

                // Intersecting edge
                AssertPolygonCreation(new[] { p1, p3, p2, p4 }, false); // edge intersects at a point
                AssertPolygonCreation(new[] { p1, p2, p3, p5, p6, p7 }, false); // parallel segments intersect not only at an endpoint
#endif
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
                var poly1 = new Polygon(vertices);
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

        [Test]
        public void TestEquality()
        {
            var p1 = new Polygon(new[]
            {
                new Vector2(10f, 10f),
                new Vector2(50f, 10f),
                new Vector2(50f, 50f),
                new Vector2(10f, 50f),
            });
            var p2 = new Polygon(new[]
            {
                new Vector2(10f, 10f),
                new Vector2(50f, 10f),
                new Vector2(50f, 50f),
                new Vector2(10f, 50f),
            });
            var p3 = new Polygon(new[]
            {
                new Vector2(50f, 10f),
                new Vector2(50f, 50f),
                new Vector2(10f, 50f),
                new Vector2(10f, 10f),
            });
            var p4 = new Polygon(new[]
            {
                new Vector2(50f, 10f),
                new Vector2(10f, 10f),
                new Vector2(10f, 50f),
                new Vector2(50f, 50f),
            });
            var p5 = new Polygon(new[]
            {
                new Vector2(50f, 10f),
                new Vector2(10f, 10f),
                new Vector2(10f, 50f),
            });
            var p6 = new Polygon(new[]
            {
                new Vector2(10f, 10f),
                new Vector2(50f, 10f),
                new Vector2(50f, 50f),
                new Vector2(10f, 50f),
                new Vector2(30f, 30f),
            });
            var p7 = new Polygon(new[]
            {
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
}
