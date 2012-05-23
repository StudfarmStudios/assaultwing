using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace FarseerPhysics.Collision
{
    [TestFixture]
    public class AABBTest
    {
        [Test]
        public void TestThinningAndFattening()
        {
            var aabb = new AABB(new Vector2(-10, 5), new Vector2(0, 35));
            Assert.AreEqual(aabb, aabb.Thinned.Fattened);
            Assert.AreEqual(aabb, aabb.Fattened.Thinned);

            AssertFattened(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
            AssertThinned(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
            AssertFattened(new Vector2(0, 0), new Vector2(40, 40), new Vector2(10, 10), new Vector2(30, 30));
            AssertThinned(new Vector2(-40, -40), new Vector2(-20, -20), new Vector2(-50, -50), new Vector2(-10, -10));
        }

        private void AssertFattened(Vector2 expectedMin, Vector2 expectedMax, Vector2 originalMin, Vector2 originalMax)
        {
            var expected = new AABB(expectedMin, expectedMax);
            var actual = new AABB(originalMin, originalMax).Fattened;
            Assert.AreEqual(expected, actual);
        }

        private void AssertThinned(Vector2 expectedMin, Vector2 expectedMax, Vector2 originalMin, Vector2 originalMax)
        {
            var expected = new AABB(expectedMin, expectedMax);
            var actual = new AABB(originalMin, originalMax).Thinned;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestContainmentDistance()
        {
            AssertContainmentDistance(2, new Vector2(10, 20), new Vector2(30, 40), new Vector2(12, 29), new Vector2(21, 31));
            AssertContainmentDistance(3, new Vector2(10, 20), new Vector2(30, 40), new Vector2(19, 29), new Vector2(27, 31));
            AssertContainmentDistance(4, new Vector2(10, 20), new Vector2(30, 40), new Vector2(19, 24), new Vector2(21, 31));
            AssertContainmentDistance(5, new Vector2(10, 20), new Vector2(30, 40), new Vector2(19, 29), new Vector2(21, 35));
        }

        private void AssertContainmentDistance(float expected, Vector2 fixedMin, Vector2 fixedMax, Vector2 testMin, Vector2 testMax)
        {
            var aabb = new AABB(fixedMin, fixedMax);
            var test = new AABB(testMin, testMax);
            Assert.AreEqual(expected, aabb.ContainmentDistance(ref test));
        }
    }
}
