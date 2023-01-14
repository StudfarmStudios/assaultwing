using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace AW2.Game.Arenas
{
    [TestFixture]
    public class NavigatorTest
    {
        private static readonly Vector2 DIMENSIONS = new Vector2(4, 4);
        private Navigator _navigator;

        [SetUp]
        public void Setup()
        {
            _navigator = new Navigator(DIMENSIONS, 1, x => true);
        }

        [Test]
        public void TestEmptyArena()
        {
            AssertPath(Vector2.Zero, Vector2.Zero, 0.5f, 0.5f);
            AssertPath(Vector2.Zero, new Vector2(0.1f, 0.1f), 0.5f, 0.5f);
            AssertPath(new Vector2(0.9f, 0.9f), new Vector2(1.1f, 1.1f), 0.5f, 0.5f, 1.5f, 1.5f);
            AssertPath(Vector2.Zero, DIMENSIONS - Vector2.One, 0.5f, 0.5f, 1.5f, 1.5f, 2.5f, 2.5f, 3.5f, 3.5f);
        }

        [Test]
        public void TestGranularity()
        {
            _navigator = new Navigator(DIMENSIONS, 2, x => true);
            AssertPath(Vector2.Zero, DIMENSIONS - Vector2.One, 1, 1, 3, 3);
        }

        [Test]
        public void TestDimensions()
        {
            _navigator = new Navigator(new Vector2(3, 5), 1, x => true);
            AssertPath(Vector2.Zero, new Vector2(2, 4), 0.5f, 0.5f, 1.5f, 1.5f, 1.5f, 2.5f, 1.5f, 3.5f, 2.5f, 4.5f);
        }

        private void AssertPath(Vector2 from, Vector2 to, params float[] expectedXY)
        {
            Assert.NotNull(expectedXY);
            Assert.That(expectedXY.Length % 2 == 0);
            var expected = new List<Vector2>();
            for (int i = 0; i < expectedXY.Length; i += 2) expected.Add(new Vector2(expectedXY[i], expectedXY[i + 1]));
            var path = _navigator.GetPath(from, to).ToArray();
            Assert.AreEqual(expected.ToArray(), path);
        }
    }
}
