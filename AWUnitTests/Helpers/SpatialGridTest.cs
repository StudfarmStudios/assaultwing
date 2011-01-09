using System;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using AW2.Helpers.Geometric;
using Rectangle = AW2.Helpers.Geometric.Rectangle;

namespace AW2.Helpers
{
    [TestFixture(TypeArgs = new[] { typeof(int) })]
    public class SpatialGridTest
    {
        [Test]
        public void TestConvertArea()
        {
            int gridMinX, gridMinY, gridMaxX, gridMaxY;
            bool outOfBounds;
            var grid1 = new SpatialGrid<int>(10, new Vector2(-50), new Vector2(100));
            Rectangle area;

            area = new Rectangle(1, 1, 9, 9);
            grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
            Assert.AreEqual(5, gridMinX);
            Assert.AreEqual(5, gridMinY);
            Assert.AreEqual(6, gridMaxX);
            Assert.AreEqual(6, gridMaxY);
            Assert.AreEqual(false, outOfBounds);

            area = new Rectangle(0, 0, 10, 10);
            grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
            Assert.AreEqual(5, gridMinX);
            Assert.AreEqual(5, gridMinY);
            Assert.AreEqual(7, gridMaxX);
            Assert.AreEqual(7, gridMaxY);
            Assert.AreEqual(false, outOfBounds);

            area = new Rectangle(-50, -50, -50, -50);
            grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
            Assert.AreEqual(0, gridMinX);
            Assert.AreEqual(0, gridMinY);
            Assert.AreEqual(1, gridMaxX);
            Assert.AreEqual(1, gridMaxY);
            Assert.AreEqual(false, outOfBounds);

            area = new Rectangle(99.9999f, 99.9999f, 99.9999f, 99.9999f);
            grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
            Assert.AreEqual(14, gridMinX);
            Assert.AreEqual(14, gridMinY);
            Assert.AreEqual(15, gridMaxX);
            Assert.AreEqual(15, gridMaxY);
            Assert.AreEqual(false, outOfBounds);

            area = new Rectangle(15, -15, float.MaxValue, float.MaxValue);
            grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
            Assert.AreEqual(6, gridMinX);
            Assert.AreEqual(3, gridMinY);
            Assert.AreEqual(15, gridMaxX);
            Assert.AreEqual(15, gridMaxY);
            Assert.AreEqual(true, outOfBounds);

            area = new Rectangle(-float.MaxValue, -float.MaxValue, float.MaxValue, 100.1f);
            grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
            Assert.AreEqual(0, gridMinX);
            Assert.AreEqual(0, gridMinY);
            Assert.AreEqual(15, gridMaxX);
            Assert.AreEqual(15, gridMaxY);
            Assert.AreEqual(true, outOfBounds);
        }
    }
}
