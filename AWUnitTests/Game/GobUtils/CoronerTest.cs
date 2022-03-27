using System;
using System.Linq;
using NUnit.Framework;
using AW2.Game.Players;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    [TestFixture]
    public class CoronerTest
    {
        private Team _avengers;
        private Player _player1, _player2, _player3, _player4;
        private Gob _gob1, _gob2, _gob2Nature, _gob1DamagedBy2, _gob4;
        private Arena _arena;

        private Player[] AllPlayers { get { return new[] { _player1, _player2, _player3, _player4 }; } }

        [SetUp]
        public void Setup()
        {
            Coroner.ResetPhraseSets();
            _avengers = new Team("Avengers", null);
            _player1 = new Player(null, "Player 1", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _player2 = new Player(null, "Player 2", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _player3 = new Player(null, "Player 3", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _player4 = new Player(null, "Player 4", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _player1.AssignTeam(_avengers);
            _player4.AssignTeam(_avengers);
            _arena = new Arena();
            _gob1 = new Gob { Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
            _gob1DamagedBy2 = new Gob { Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
            _gob2 = new Gob { Owner = _player2, MaxDamageLevel = 100, Arena = _arena };
            _gob2Nature = new Gob { Owner = null };
            _gob4 = new Gob { Owner = _player4, MaxDamageLevel = 100, Arena = _arena };
            _arena.TotalTime = TimeSpan.FromSeconds(10);
            _gob1DamagedBy2.InflictDamage(10, new DamageInfo(_gob2));
        }

        private void AssertSuicideMessages(string suicidePhrase, string deathMessage, string bystanderMessage)
        {
            Coroner.SetPhraseSets(new[] { suicidePhrase }, new[] { "dummy" }, new[] { "dummy" }, new string[0]);
            var info = new DamageInfo(_gob2Nature).Bind(_gob1);
            var coroner = new Coroner(info);
            Assert.AreEqual(null, coroner.KillerSpectator);
            Assert.AreEqual(Coroner.DeathTypeType.Accident, coroner.DeathType);
            Assert.AreEqual(deathMessage, coroner.MessageToCorpse);
            Assert.AreEqual(bystanderMessage, coroner.MessageToBystander);
        }

        private void AssertTeamKillMessages(string teamKillPhrase, string killMessage, string deathMessage, string bystanderMessage)
        {
            Coroner.SetPhraseSets(new[] { "dummy" }, new[] { "dummy" }, new[] { teamKillPhrase }, new string[0]);
            var info = new DamageInfo(_gob4).Bind(_gob1);
            var coroner = new Coroner(info);
            Assert.AreEqual(_player4, coroner.KillerSpectator);
            Assert.AreEqual(Coroner.DeathTypeType.TeamKill, coroner.DeathType);
            Assert.AreEqual(killMessage, coroner.MessageToKiller);
            Assert.AreEqual(deathMessage, coroner.MessageToCorpse);
            Assert.AreEqual(bystanderMessage, coroner.MessageToBystander);
        }

        private void AssertKillMessages(string killPhrase, string killMessage, string deathMessage, string bystanderMessage)
        {
            Coroner.SetPhraseSets(new[] { "dummy" }, new[] { killPhrase }, new[] { "dummy" }, new string[0]);
            var info = new DamageInfo(_gob2).Bind(_gob1);
            var coroner = new Coroner(info);
            Assert.AreEqual(_player2, coroner.KillerSpectator);
            Assert.AreEqual(Coroner.DeathTypeType.Kill, coroner.DeathType);
            Assert.AreEqual(killMessage, coroner.MessageToKiller);
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
        public void TestSimpleTeamKill()
        {
            AssertKillMessages("{0} mistook {1} for an enemy", "You mistook Player 1 for an enemy", "Player 2 mistook you for an enemy", "Player 2 mistook Player 1 for an enemy");
        }

        [Test]
        public void TestComplexTeamKill()
        {
            AssertKillMessages("{0} pretended to be {1:genetive} enemy", "You pretended to be Player 1's enemy", "Player 2 pretended to be your enemy", "Player 2 pretended to be Player 1's enemy");
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
                _arena.TotalTime = TimeSpan.FromSeconds(11);
                var info = damageInfo2.Bind(target);
                return new Coroner(info);
            };
            Func<Player, Player, Player, Coroner> getCoronerFromPlayer = (targetPlayer, sourcePlayer1, sourcePlayer2) =>
                getCoronerFromInfo(targetPlayer, getInfo(sourcePlayer1), getInfo(sourcePlayer2));
            Action<Coroner, Player, Coroner.DeathTypeType> assertCoroner = (coroner, expectedKiller, expectedDeathType) =>
            {
                Assert.AreEqual(expectedDeathType, coroner.DeathType);
                Assert.AreEqual(expectedKiller, coroner.KillerSpectator);
            };
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, DamageInfo.Unspecified), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, getInfo(null)), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromInfo(_player1, getInfo(null), DamageInfo.Unspecified), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, getInfo(_player1)), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromInfo(_player1, getInfo(_player1), DamageInfo.Unspecified), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, getInfo(_player2)), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromInfo(_player1, getInfo(_player2), DamageInfo.Unspecified), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, getInfo(_player4)), _player4, Coroner.DeathTypeType.TeamKill);
            assertCoroner(getCoronerFromInfo(_player1, getInfo(_player4), DamageInfo.Unspecified), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, null, null), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, null, _player1), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, null, _player2), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, null, _player4), _player4, Coroner.DeathTypeType.TeamKill);
            assertCoroner(getCoronerFromPlayer(_player1, _player1, null), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, _player1, _player1), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, _player1, _player2), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player1, _player4), _player4, Coroner.DeathTypeType.TeamKill);
            assertCoroner(getCoronerFromPlayer(_player1, _player2, null), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player2, _player1), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player2, _player2), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player2, _player3), _player3, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player2, _player4), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player4, null), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, _player4, _player1), null, Coroner.DeathTypeType.Accident);
            assertCoroner(getCoronerFromPlayer(_player1, _player4, _player2), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player4, _player4), _player4, Coroner.DeathTypeType.TeamKill);
        }

        [Test]
        public void TestLastDamagerTooLate()
        {
            _arena.TotalTime = TimeSpan.FromSeconds(30);
            var info = DamageInfo.Unspecified.Bind(_gob1DamagedBy2);
            Assert.AreEqual(BoundDamageInfo.SourceTypeType.Unspecified, info.SourceType);
            var coroner = new Coroner(info);
            Assert.AreEqual(Coroner.DeathTypeType.Accident, coroner.DeathType);
            Assert.AreEqual(null, coroner.KillerSpectator);
        }

        [Test]
        public void TestBystandersKill()
        {
            var info = new DamageInfo(_gob2).Bind(_gob1);
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystandingPlayers(AllPlayers).ToArray();
            Assert.AreEqual(new[] { _player3, _player4 }, bystanders);
        }

        [Test]
        public void TestBystandersUnspecifiedDeath()
        {
            var info = DamageInfo.Unspecified.Bind(_gob1);
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystandingPlayers(AllPlayers).ToArray();
            Assert.AreEqual(new[] { _player2, _player3, _player4 }, bystanders);
        }

        [Test]
        public void TestBystandersSuicide()
        {
            var info = new DamageInfo(_gob1).Bind(_gob1);
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystandingPlayers(AllPlayers).ToArray();
            Assert.AreEqual(new[] { _player2, _player3, _player4 }, bystanders);
        }

        [Test]
        public void TestBystandersTeamKill()
        {
            var info = new DamageInfo(_gob4).Bind(_gob1);
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystandingPlayers(AllPlayers).ToArray();
            Assert.AreEqual(new[] { _player2, _player3 }, bystanders);
        }

        [Test]
        public void TestBystandersKillLastDamager()
        {
            _arena.TotalTime = TimeSpan.FromSeconds(11);
            var info = DamageInfo.Unspecified.Bind(_gob1DamagedBy2);
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystandingPlayers(new[] { _player1, _player2, _player3 }).ToArray();
            Assert.AreEqual(new[] { _player3 }, bystanders);
        }
    }
}
