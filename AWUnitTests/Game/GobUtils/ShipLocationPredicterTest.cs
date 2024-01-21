using System;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using AW2.UI;

namespace AW2.Game.GobUtils
{
    [TestFixture]
    public class ShipLocationPredicterTest
    {
        private ShipLocationPredicter _predicter;
        private ShipLocationPredicter.ShipData _shipData;
        private ShipLocationEntry _entry1, _entry2, _entry3;
        private ControlState[] _turningLeft;

        private void SetLatestShipLocationEntry(ShipLocationEntry entry)
        {
            _predicter.StoreOldShipLocation(_shipData.ShipLocationEntry);
            _shipData.ShipLocationEntry = entry;
        }

        private ShipLocationEntry GetShipLocationEntry(TimeSpan gameTime)
        {
            return _predicter.GetShipLocation(gameTime);
        }

        private void SetShipLocation(ShipLocationEntry entry)
        {
            _shipData.ShipLocationEntry.Pos = entry.Pos;
            _shipData.ShipLocationEntry.Move = entry.Move;
            _shipData.ShipLocationEntry.Rotation = entry.Rotation;
        }

        [SetUp]
        public void Setup()
        {
            _predicter = new ShipLocationPredicter(() => _shipData, SetShipLocation);
            var on = new ControlState(1, true);
            var off = new ControlState(0, false);
            _turningLeft = new[] { off, on, off, off, off, off, off };
            _shipData = new ShipLocationPredicter.ShipData
            {
                ShipLocationEntry = new ShipLocationEntry
                {
                    Rotation = 0,
                    Pos = Vector2.Zero,
                    Move = Vector2.Zero,
                    GameTime = TimeSpan.Zero,
                    ControlStates = _turningLeft,
                },
                TargetElapsedTime = TimeSpan.FromSeconds(1f / 20),
                ThrustForce = 120000,
                TurnSpeed = 1,
                Mass = 350,
            };
            _entry1 = new ShipLocationEntry
            {
                GameTime = TimeSpan.FromSeconds(1),
                Pos = new Vector2(100, 100),
                Move = new Vector2(1000, 0),
                Rotation = 1,
                ControlStates = _turningLeft,
            };
            _entry2 = new ShipLocationEntry
            {
                GameTime = TimeSpan.FromSeconds(1.1),
                Pos = new Vector2(200, 100),
                Move = new Vector2(1000, 0),
                Rotation = 1.1f,
                ControlStates = _turningLeft,
            };
            _entry3 = new ShipLocationEntry
            {
                GameTime = TimeSpan.FromSeconds(1.2),
                Pos = new Vector2(300, 100),
                Move = new Vector2(1000, 0),
                Rotation = 1.2f,
                ControlStates = _turningLeft,
            };
        }

        [Test]
        public void TestPrune()
        {
            // Contains entry at 0 seconds
            Assert.AreEqual(1, _predicter.ShipLocationCount);
            SetLatestShipLocationEntry(_entry1);
            // Contains entries at 0 and 1 seconds
            Assert.AreEqual(2, _predicter.ShipLocationCount);
            SetLatestShipLocationEntry(_entry2);
            // Contains entries at 1 and 1.1 seconds (0 was pruned)
            Assert.AreEqual(2, _predicter.ShipLocationCount);
            SetLatestShipLocationEntry(_entry3);
            // Contains entries at 1 and 1.1 and 1.2 seconds
            Assert.AreEqual(3, _predicter.ShipLocationCount);
        }

        [Test]
        public void TestExact()
        {
            SetLatestShipLocationEntry(_entry1);
            var entry = GetShipLocationEntry(TimeSpan.FromSeconds(1));
            Assert.AreEqual(_entry1, entry);
        }

        [Test]
        public void TestInterpolate()
        {
            SetLatestShipLocationEntry(_entry1);
            SetLatestShipLocationEntry(_entry3);
            var entry = GetShipLocationEntry(TimeSpan.FromSeconds(1.1));
            Assert.AreEqual(_entry2, entry);
        }

        [Test]
        public void TestExtrapolate()
        {
            SetLatestShipLocationEntry(_entry1);
            SetLatestShipLocationEntry(_entry2);
            var entry = GetShipLocationEntry(TimeSpan.FromSeconds(1.2));
            Assert.AreEqual(_entry3, entry);
        }

        [Test]
        public void TestUpdateFromServer()
        {
            var serverEntry1 = new ShipLocationEntry
            {
                GameTime = TimeSpan.FromSeconds(1),
                Pos = new Vector2(100, 100),
                Move = new Vector2(1000, 0),
                Rotation = 2,
                ControlStates = null,
            };
            var resultEntry2 = new ShipLocationEntry
            {
                GameTime = TimeSpan.FromSeconds(1.1),
                Pos = new Vector2(200, 100),
                Move = new Vector2(1000, 0),
                Rotation = 2.1f,
                ControlStates = _turningLeft,
            };
            SetLatestShipLocationEntry(_entry1);
            SetLatestShipLocationEntry(_entry2);
            _predicter.UpdateOldShipLocation(serverEntry1);
            var entry = GetShipLocationEntry(TimeSpan.FromSeconds(1.1));
            Assert.AreEqual(resultEntry2, entry);
        }
    }
}
