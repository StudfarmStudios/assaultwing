using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;

namespace AW2.Game.Weapons
{
    [TestFixture]
    public class LightningGunTest
    {
        private class MockGob : Gob
        {
            public string Name { get; set; }
            public override string ToString()
            {
                return string.Format("{0} at {1}", Name, Pos);
            }
        }

        private Ship _shooter;
        private Gob _gob1, _gob2, _gob3, _gob4, _gob5, _gob6, _gob7;
        private Gob[] _potentialTargets;
        private LightningGun _gun;

        [SetUp]
        public void Setup()
        {
            _shooter = new Gobs.Ship { Pos = new Vector2(0, 0), Rotation = 0 };
            _gob1 = new MockGob { Name = "Gob1", Pos = new Vector2(100, 0) };
            _gob2 = new MockGob { Name = "Gob2", Pos = new Vector2(200, 0) };
            _gob3 = new MockGob { Name = "Gob3", Pos = new Vector2(300, 0) };
            _gob4 = new MockGob { Name = "Gob4", Pos = new Vector2(0, 100) };
            _gob5 = new MockGob { Name = "Gob5", Pos = new Vector2(0, 200) };
            _gob6 = new MockGob { Name = "Gob6", Pos = new Vector2(-100, 0) };
            _gob7 = new MockGob { Name = "Gob7", Pos = new Vector2(0, -100) };
            _potentialTargets = new[] { _gob1, _gob2, _gob3, _gob4, _gob5, _gob6 };
            _gun = new LightningGun();
            _gun.AttachTo(_shooter, GobUtils.ShipDevice.OwnerHandleType.PrimaryWeapon);
        }

        [Test]
        public void TestFindTargets()
        {
            _shooter.Rotation = 0;
            Assert.AreEqual(new[] { _gob1, _gob2, _gob3 }, _gun.FindTargets(_potentialTargets));

            _shooter.Rotation = MathHelper.PiOver2;
            Assert.AreEqual(new[] { _gob4, _gob5 }, _gun.FindTargets(_potentialTargets).ToArray());
        }
    }
}
