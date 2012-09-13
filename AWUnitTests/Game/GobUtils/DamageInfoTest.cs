using System;
using NUnit.Framework;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    [TestFixture]
    public class DamageInfoTest
    {
        private Arena _arena;
        private Player _player1, _player2;
        private Gob _gob1, _gob2, _gobNature, _gob1DamagedBy2;
        private BoundDamageInfo _info1, _info2, _info3, _info1DamagedBy2;

        [SetUp]
        public void Setup()
        {
            _arena = new Arena();
            _player1 = new Player(null, "Player 1", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _player2 = new Player(null, "Player 2", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _gob1 = new Gob { ID = 10, Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
            _gob1DamagedBy2 = new Gob { ID = 11, Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
            _gob2 = new Gob { ID = 2, Owner = _player2, MaxDamageLevel = 100, Arena = _arena };
            _gobNature = new Gob { ID = 3, Owner = null, MaxDamageLevel = 100, Arena = _arena };
            _info1 = new DamageInfo(_gob1).Bind(_gob1);
            _info2 = new DamageInfo(_gob2).Bind(_gob1);
            _info3 = new DamageInfo(_gobNature).Bind(_gob1);
            _arena.TotalTime = TimeSpan.FromSeconds(10);
            _gob1DamagedBy2.InflictDamage(10, new DamageInfo(_gob2));
            _info1DamagedBy2 = DamageInfo.Unspecified.Bind(_gob1DamagedBy2);
        }

        [Test]
        public void TestCause()
        {
            Assert.AreEqual(_gob1, _info1.Cause);
            Assert.AreEqual(_gob2, _info2.Cause);
            Assert.AreEqual(_gobNature, _info3.Cause);
            Assert.AreEqual(null, _info1DamagedBy2.Cause);
        }

        [Test]
        public void TestSourceType()
        {
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.OwnPlayer, _info1.SourceType);
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.EnemyPlayer, _info2.SourceType);
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.Unspecified, _info3.SourceType);
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.Unspecified, _info1DamagedBy2.SourceType);
        }
    }
}
