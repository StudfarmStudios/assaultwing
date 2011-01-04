#if DEBUG
using NUnit.Framework;
using AW2.Helpers;
#endif
using System;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Information about a damaging event of a <see cref="Gob"/>.
    /// </summary>
    public class DamageInfo
    {
        public static DamageInfo Unspecified { get; private set; }

        public Gob Cause { get; private set; }

        static DamageInfo()
        {
            Unspecified = new DamageInfo();
        }

        public DamageInfo(Gob cause)
        {
            Cause = cause;
        }

        private DamageInfo() { }

        public BoundDamageInfo Bind(Gob target, TimeSpan time)
        {
            if (target == null) throw new ArgumentNullException("target");
            return new BoundDamageInfo(this, target, time);
        }
    }

    public class BoundDamageInfo : DamageInfo
    {
        public enum SourceTypeType { Unspecified, OwnPlayer, EnemyPlayer };

        public Gob Target { get; private set; }
        public TimeSpan Time { get; private set; }
        public SourceTypeType SourceType
        {
            get
            {
                if (Cause == null || Cause.Owner == null) return SourceTypeType.Unspecified;
                return Cause.Owner == Target.Owner
                    ? SourceTypeType.OwnPlayer
                    : SourceTypeType.EnemyPlayer;
            }
        }
        public Player PlayerCause { get { return Cause.Owner; } }

        public BoundDamageInfo(DamageInfo info, Gob target, TimeSpan time)
            : base(info.Cause)
        {
            if (target == null) throw new ArgumentNullException("target");
            Target = target;
            Time = time;
        }
    }

#if DEBUG
    [TestFixture]
    public class DamageInfoTests
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
            _info1 = new DamageInfo(_gob1).Bind(_gob1, TimeSpan.FromSeconds(10));
            _info2 = new DamageInfo(_gob2).Bind(_gob1, TimeSpan.FromSeconds(10));
            _info3 = new DamageInfo(_gobNature).Bind(_gob1, TimeSpan.FromSeconds(10));
            _arena.TotalTime = TimeSpan.FromSeconds(10);
            _gob1DamagedBy2.InflictDamage(10, new DamageInfo(_gob2));
            _info1DamagedBy2 = DamageInfo.Unspecified.Bind(_gob1DamagedBy2, TimeSpan.FromSeconds(11));
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
#endif
}
