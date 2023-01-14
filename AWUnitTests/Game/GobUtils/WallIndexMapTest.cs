using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using Rectangle = AW2.Helpers.Geometric.Rectangle;

namespace AW2.Game.GobUtils
{
    [TestFixture]
    public class WallIndexMapTest
    {
        private List<int> _removedTriangles;

        [SetUp]
        public void Setup()
        {
            _removedTriangles = new List<int>();
        }

        [Test]
        public void TestGetVerySmallTriangles()
        {
            var map = CreateMap(
                0, 0, 0, 0, 0, 0,
                0, 0, 0, 10, 10, 0,
                0, 10, 10, 0, 0, 10);
            Assert.AreEqual(new[] { 0, 2 }, map.GetVerySmallTriangles().ToArray());
        }

        [Test]
        public void TestRemove()
        {
            var map = CreateMap(0, 0, 0, 2, 2, 0);
            // Note: map covers three pixels.
            AssertRemove(map);
        }

        [Test]
        public void TestRemoveWithOffset()
        {
            var map = CreateMap(10, 10, 10, 12, 12, 10);
            // Note: map covers three pixels.
            AssertRemove(map);
        }

        [Test]
        public void TestSerialize()
        {
            var map = CreateMap(0, 0, 0, 2, 2, 0);
            var buffer = new MemoryStream();
            var writer = new BinaryWriter(buffer);
            map.Serialize(writer);
            buffer.Seek(0, SeekOrigin.Begin);
            var boundingBox = new Rectangle(0, 0, 2, 2); // FIXME !!! this is stupid; WallIndexMap should figure out the bounding box by itself
            var map2 = new WallIndexMap(x => { }, boundingBox, new BinaryReader(buffer));
            Assert.AreEqual(map.WallToIndexMapTransform, map2.WallToIndexMapTransform);
            Assert.AreEqual(Tuple.Create(map.Width, map.Height), Tuple.Create(map2.Width, map2.Height));
        }

        private WallIndexMap CreateMap(params float[] coords)
        {
            var vertices = new Vector2[coords.Length / 2];
            for (int i = 0; i < coords.Length / 2; i++) vertices[i] = new Vector2(coords[i * 2], coords[i * 2 + 1]);
            var bounds = Rectangle.FromVector2(vertices);
            var indices = Enumerable.Range(0, vertices.Count()).Select(i => (short)i).ToArray();
            return new WallIndexMap(_removedTriangles.Add, bounds, vertices, indices);
        }

        private void AssertRemove(WallIndexMap map)
        {
            Assert.IsFalse(map.Remove(-1, -1, 1));
            Assert.IsFalse(map.Remove(2, 0, 1));
            Assert.IsFalse(map.Remove(0, 2, 1));
            Assert.IsTrue(map.Remove(0, 0, 1));
            Assert.IsEmpty(_removedTriangles);
            Assert.IsTrue(map.Remove(0, 0, 1));
            Assert.IsEmpty(_removedTriangles);
            Assert.IsTrue(map.Remove(0, 0, 1));
            Assert.AreEqual(new[] { 0 }, _removedTriangles);
        }
    }
}
