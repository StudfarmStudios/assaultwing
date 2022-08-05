using System;
using NUnit.Framework;
using AW2.Game.Players;
using AW2.TestHelpers;

namespace AW2.Game.GobUtils
{
    [TestFixture]
    public class DamageInfoTest
    {
        private Arena _arena;
        private Team _avengers;
        private Player _player1, _player2, _player3;
        private Gob _gob1, _gob2, _gob3, _gobNature, _gob1DamagedBy2;
        private BoundDamageInfo _info1Hit1, _info2Hit1, _info3Hit2, _infoNatureHit1, _info1DamagedBy2;

        [SetUp]
        public void Setup()
        {
            _arena = new Arena();
            _avengers = new Team("Avengers", null);
            _player1 = PlayerHelper.Make(1);
            _player2 = PlayerHelper.Make(2);
            _player3 = PlayerHelper.Make(3);
            _player2.AssignTeam(_avengers);
            _player3.AssignTeam(_avengers);
            _gob1 = new Gob { ID = 10, Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
            _gob1DamagedBy2 = new Gob { ID = 11, Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
            _gob2 = new Gob { ID = 2, Owner = _player2, MaxDamageLevel = 100, Arena = _arena };
            _gob3 = new Gob { ID = 3, Owner = _player3, MaxDamageLevel = 100, Arena = _arena };
            _gobNature = new Gob { ID = 4, Owner = null, MaxDamageLevel = 100, Arena = _arena };
            _info1Hit1 = new DamageInfo(_gob1).Bind(_gob1);
            _info2Hit1 = new DamageInfo(_gob2).Bind(_gob1);
            _info3Hit2 = new DamageInfo(_gob3).Bind(_gob2);
            _infoNatureHit1 = new DamageInfo(_gobNature).Bind(_gob1);
            _arena.TotalTime = TimeSpan.FromSeconds(10);
            _gob1DamagedBy2.InflictDamage(10, new DamageInfo(_gob2));
            _info1DamagedBy2 = DamageInfo.Unspecified.Bind(_gob1DamagedBy2);
        }

        [Test]
        public void TestCause()
        {
            Assert.AreEqual(_gob1, _info1Hit1.Cause);
            Assert.AreEqual(_gob2, _info2Hit1.Cause);
            Assert.AreEqual(_gob3, _info3Hit2.Cause);
            Assert.AreEqual(_gobNature, _infoNatureHit1.Cause);
            Assert.AreEqual(null, _info1DamagedBy2.Cause);
        }

        [Test]
        public void TestSourceType()
        {
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.Self, _info1Hit1.SourceType);
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.EnemyPlayer, _info2Hit1.SourceType);
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.OwnTeamPlayer, _info3Hit2.SourceType);
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.Unspecified, _infoNatureHit1.SourceType);
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.Unspecified, _info1DamagedBy2.SourceType);
        }
    }
}
