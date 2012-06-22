using System;
using System.Linq;
using NUnit.Framework;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    [TestFixture]
    public class CoronerTest
    {
        private Player _player1, _player2, _player3;
        private Gob _gob1, _gob2, _gob2Nature, _gob1DamagedBy2;
        private Arena _arena;

        [SetUp]
        public void Setup()
        {
            Spectator.CreateStatsData = spectator => new MockStats();
            Coroner.ResetPhraseSets();
            _player1 = new Player(null, "Player 1", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _player2 = new Player(null, "Player 2", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _player3 = new Player(null, "Player 3", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _arena = new Arena();
            _gob1 = new Gob { Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
            _gob1DamagedBy2 = new Gob { Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
            _gob2 = new Gob { Owner = _player2, MaxDamageLevel = 100, Arena = _arena };
            _gob2Nature = new Gob { Owner = null };
            _arena.TotalTime = TimeSpan.FromSeconds(10);
            _gob1DamagedBy2.InflictDamage(10, new DamageInfo(_gob2));
        }

        private void AssertSuicideMessages(string suicidePhrase, string deathMessage, string bystanderMessage)
        {
            Coroner.SetPhraseSets(new[] { suicidePhrase }, new[] { "dummy" }, new string[0]);
            var info = new DamageInfo(_gob2Nature).Bind(_gob1, TimeSpan.FromSeconds(10));
            var coroner = new Coroner(info);
            Assert.AreEqual(null, coroner.ScoringSpectator);
            Assert.AreEqual(Coroner.DeathTypeType.Accident, coroner.DeathType);
            Assert.AreEqual(deathMessage, coroner.MessageToCorpse);
            Assert.AreEqual(bystanderMessage, coroner.MessageToBystander);
        }

        private void AssertKillMessages(string killPhrase, string killMessage, string deathMessage, string bystanderMessage)
        {
            Coroner.SetPhraseSets(new[] { "dummy" }, new[] { killPhrase }, new string[0]);
            var info = new DamageInfo(_gob2).Bind(_gob1, TimeSpan.FromSeconds(10));
            var coroner = new Coroner(info);
            Assert.AreEqual(_player2, coroner.ScoringSpectator);
            Assert.AreEqual(Coroner.DeathTypeType.Kill, coroner.DeathType);
            Assert.AreEqual(killMessage, coroner.MessageToScoringPlayer);
            Assert.AreEqual(deathMessage, coroner.MessageToCorpse);
            Assert.AreEqual(bystanderMessage, coroner.MessageToBystander);
        }

        [Test]
        public void TestSimpleKill()
        {
            AssertKillMessages("{0} nailed {1}", "You nailed Player 1", "Player 2 nailed you", "Player 2 nailed Player 1");
        }

        [Test]
        public void TestComplexKill()
        {
            AssertKillMessages("{0} put {1} on {1:genetivePronoun} knees", "You put Player 1 on his knees", "Player 2 put you on your knees", "Player 2 put Player 1 on his knees");
            AssertKillMessages("{0} stepped on {1:genetive} foot", "You stepped on Player 1's foot", "Player 2 stepped on your foot", "Player 2 stepped on Player 1's foot");
        }

        [Test]
        public void TestSimpleSuicide()
        {
            AssertSuicideMessages("{0} screwed up", "You screwed up", "Player 1 screwed up");
        }

        [Test]
        public void TestComplexSuicide()
        {
            AssertSuicideMessages("{0} nailed {0:reflexivePronoun}", "You nailed yourself", "Player 1 nailed himself");
            AssertSuicideMessages("{0} ended up as {0:genetivePronoun} own nemesis", "You ended up as your own nemesis", "Player 1 ended up as his own nemesis");
        }

        [Test]
        public void TestLastDamager()
        {
            Func<Player, Gob> getGob = owner => new Gob { Owner = owner, MaxDamageLevel = 100, Arena = _arena };
            Func<Player, DamageInfo> getInfo = causeOwner => new DamageInfo(getGob(causeOwner));
            Func<Player, DamageInfo, DamageInfo, Coroner> getCoronerFromInfo = (targetPlayer, damageInfo1, damageInfo2) =>
            {
                var target = getGob(targetPlayer);
                _arena.TotalTime = TimeSpan.FromSeconds(10);
                target.InflictDamage(10, damageInfo1);
                var info = damageInfo2.Bind(target, TimeSpan.FromSeconds(11));
                return new Coroner(info);
            };
            Func<Player, Player, Player, Coroner> getCoronerFromPlayer = (targetPlayer, sourcePlayer1, sourcePlayer2) =>
                getCoronerFromInfo(targetPlayer, getInfo(sourcePlayer1), getInfo(sourcePlayer2));
            Action<Coroner, Player, Coroner.DeathTypeType> assertCoroner = (coroner, expectedScoringPlayer, expectedDeathType) =>
            {
                Assert.AreEqual(expectedDeathType, coroner.DeathType);
                Assert.AreEqual(expectedScoringPlayer, coroner.ScoringSpectator);
            };
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, DamageInfo.Unspecified), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, getInfo(null)), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromInfo(_player1, getInfo(null), DamageInfo.Unspecified), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, getInfo(_player1)), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromInfo(_player1, getInfo(_player1), DamageInfo.Unspecified), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, getInfo(_player2)), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromInfo(_player1, getInfo(_player2), DamageInfo.Unspecified), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, null, null), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, null, _player1), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, null, _player2), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player1, null), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, _player1, _player1), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, _player1, _player2), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player2, null), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player2, _player1), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player2, _player2), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player2, _player3), _player3, Coroner.DeathTypeType.Kill);
        }

        [Test]
        public void TestLastDamagerTooLate()
        {
            var info = DamageInfo.Unspecified.Bind(_gob1DamagedBy2, TimeSpan.FromSeconds(30));
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.Unspecified, info.SourceType);
            var coroner = new Coroner(info);
            Assert.AreEqual(Coroner.DeathTypeType.Accident, coroner.DeathType);
            Assert.AreEqual(null, coroner.ScoringSpectator);
        }

        [Test]
        public void TestBystandersKill()
        {
            var info = new DamageInfo(_gob2).Bind(_gob1, TimeSpan.FromSeconds(10));
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystandingPlayers(new[] { _player1, _player2, _player3 }).ToArray();
            Assert.AreEqual(new[] { _player3 }, bystanders);
        }

        [Test]
        public void TestBystandersUnspecifiedDeath()
        {
            var info = DamageInfo.Unspecified.Bind(_gob1, TimeSpan.FromSeconds(10));
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystandingPlayers(new[] { _player1, _player2, _player3 }).ToArray();
            Assert.AreEqual(new[] { _player2, _player3 }, bystanders);
        }

        [Test]
        public void TestBystandersSuicide()
        {
            var info = new DamageInfo(_gob1).Bind(_gob1, TimeSpan.FromSeconds(10));
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystandingPlayers(new[] { _player1, _player2, _player3 }).ToArray();
            Assert.AreEqual(new[] { _player2, _player3 }, bystanders);
        }

        [Test]
        public void TestBystandersKillLastDamager()
        {
            var info = DamageInfo.Unspecified.Bind(_gob1DamagedBy2, TimeSpan.FromSeconds(11));
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystandingPlayers(new[] { _player1, _player2, _player3 }).ToArray();
            Assert.AreEqual(new[] { _player3 }, bystanders);
        }
    }
}
