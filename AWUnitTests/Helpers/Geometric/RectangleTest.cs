using System;
using NUnit.Framework;
using Microsoft.Xna.Framework;

namespace AW2.Helpers.Geometric
{
    [TestFixture]
    public class RectangleTest
    {
        [Test]
        public void TestContains()
        {
            var rectZero = new Rectangle(Vector2.Zero, Vector2.Zero);
            var rect1_2 = new Rectangle(new Vector2(1, 1), new Vector2(2, 2));
            var rectNeg10_20 = new Rectangle(new Vector2(-10, -10), new Vector2(20, 20));
            Assert.IsTrue(rectZero.Contains(rectZero));
            Assert.IsFalse(rectZero.Contains(rect1_2));
            Assert.IsFalse(rect1_2.Contains(rectZero));
            Assert.IsTrue(rectNeg10_20.Contains(rectZero));
            Assert.IsTrue(rectNeg10_20.Contains(rect1_2));
            Assert.IsFalse(rectZero.Contains(rectNeg10_20));
            Assert.IsFalse(rect1_2.Contains(rectNeg10_20));

            var fixedRect = new Rectangle(new Vector2(1, 51), new Vector2(3, 53));
            for (int maxX = 0; maxX < 3; maxX++)
                for (int minX = 0; minX <= maxX; minX++)
                    for (int maxY = 0; maxY < 3; maxY++)
                        for (int minY = 0; minY <= maxY; minY++)
                        {
                            var testRect = new Rectangle(new Vector2(2 * minX, 50 + 2 * minY), new Vector2(2 * maxX, 50 + 2 * maxY));
                            Assert.AreEqual(minX == 1 && maxX == 1 && minY == 1 && maxY == 1, fixedRect.Contains(testRect), "testRect = {0}", testRect);
                        }
        }
    }
}
