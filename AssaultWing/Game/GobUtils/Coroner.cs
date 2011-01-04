#if DEBUG
using NUnit.Framework;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Analyses the death of a gob based on the BoundDamageInfo about
    /// the last damage the gob received.
    /// </summary>
    public class Coroner
    {
        public enum DeathTypeType { Suicide, Kill };
        private enum RecipientType { ScoringPlayer, Bystander, Corpse };
        private delegate SubjectWord SubjectWordProvider(RecipientType recipient);

        private static string[] g_suicidePhrases;
        private static string[] g_killPhrases;
        private static string[] g_omgs;
        private string _killPhrase;
        private string _specialPhrase;
        private SubjectWordProvider[] _subjectNameProviders;

        /// <summary>
        /// The reason of the death.
        /// </summary>
        public DeathTypeType DeathType { get; private set; }

        /// <summary>
        /// The player who gets a frag for the death. May be <c>null</c> if no-one gets a frag.
        /// </summary>
        public Player ScoringPlayer { get; private set; }

        /// <summary>
        /// The player whose gob was killed. May be <c>null</c>.
        /// </summary>
        public Player KilledPlayer { get; private set; }

        public string MessageToScoringPlayer { get { return GetMessageFor(RecipientType.ScoringPlayer); } }
        public string MessageToBystander { get { return GetMessageFor(RecipientType.Bystander); } }
        public string MessageToCorpse { get { return GetMessageFor(RecipientType.Corpse); } }

        /// <summary>
        /// Special message for everyone. May be <c>null</c>.
        /// </summary>
        public string SpecialMessage { get { return _specialPhrase; } }

        private SubjectWord ScoringPlayerName { get { return SubjectWord.FromProperNoun(ScoringPlayer != null ? ScoringPlayer.Name : "Nature"); } }
        private SubjectWord CorpseName { get { return SubjectWord.FromProperNoun(KilledPlayer.Name); } }
        private SubjectWordProvider ScoringPlayerNameProvider { get { return recipient => recipient == RecipientType.ScoringPlayer ? SubjectWord.You : ScoringPlayerName; } }
        private SubjectWordProvider CorpseNameProvider { get { return recipient => recipient == RecipientType.Corpse ? SubjectWord.You : CorpseName; } }

        static Coroner()
        {
            ResetPhraseSets();
        }

        public static void SetPhraseSets(string[] suicidePhrases, string[] killPhrases, string[] omgs)
        {
            if (suicidePhrases == null || suicidePhrases.Length == 0) throw new ArgumentNullException("suicidePhrases");
            if (killPhrases == null || killPhrases.Length == 0) throw new ArgumentNullException("killPhrases");
            if (omgs == null) throw new ArgumentNullException("omgs");
            g_suicidePhrases = suicidePhrases;
            g_killPhrases = killPhrases;
            g_omgs = omgs;
        }

        public static void ResetPhraseSets()
        {
            g_suicidePhrases = new[]
            {
                "{0} nailed {0:reflexivePronoun}", "{0} ended up as {0:genetivePronoun} own nemesis",
                "{0} stumbled over {0:genetivePronoun} own feet", "{0} screwed up",
                "{0} terminated {0:reflexivePronoun}", "{0} crushed {0:reflexivePronoun}",
                "{0} destroyed {0:reflexivePronoun}", "{0} iced {0:reflexivePronoun}",
                "{0} got it all wrong",
            };
            g_killPhrases = new[]
            {
                "{0} nailed {1}", "{0} put {1} to rest", "{0} did {1} in",  "{0} iced {1}",
                "{0} put {1} on {1:genetivePronoun} knees", "{0} terminated {1}", "{0} crushed {1}", "{0} destroyed {1}",
                "{0} ran over {1}", "{0} showed {1} how it's done", "{0} taught {1} a lesson",
                "{0} made {1} appreciate life", "{0} survived, {1} didn't", "{0} stepped on {1:genetive} foot",
                "{0:genetive} forcefulness broke {1}",
            };
            g_omgs = new[]
            {
                "OMG", "W00T", "WHOA", "GROOVY", "WICKED", "AWESOME", "INSANE", "SLAMMIN'",
                "CRACKIN'", "KINKY", "JIGGY", "NEAT", "FAR OUT", "SLICK", "SMOKING", "SOLID",
                "SPIFFY", "CHICKY", "COOL", "L33T", "BRUTAL",
            };
        }

        public Coroner(BoundDamageInfo info)
        {
            AnalyzeDeath(info);
            AssignKillPhrase();
            AssignSpecialPhrase();
        }

        public IEnumerable<Player> GetBystanders(IEnumerable<Player> everybody)
        {
            return everybody.Except(new[] { KilledPlayer, ScoringPlayer });
        }

        private void AnalyzeDeath(BoundDamageInfo info)
        {
            Action markAsSuicide = () =>
            {
                DeathType = DeathTypeType.Suicide;
                ScoringPlayer = null;
            };
            Action<Player> markAsKill = killer =>
            {
                if (killer == null) throw new ArgumentNullException("killer");
                DeathType = DeathTypeType.Kill;
                ScoringPlayer = killer;
            };
            if (info.SourceType == BoundDamageInfo.SourceTypeType.EnemyPlayer)
                markAsKill(info.PlayerCause);
            else if (info.Target.LastDamagerTimeout >= info.Time && info.Target.LastDamager != null)
                markAsKill(info.Target.LastDamager);
            else
                markAsSuicide();
            KilledPlayer = info.Target.Owner;
        }

        private string GetMessageFor(RecipientType recipient)
        {
            return string.Format(_killPhrase, SubjectNamesFor(recipient)).Capitalize();
        }

        private SubjectWord[] SubjectNamesFor(RecipientType recipient)
        {
            return _subjectNameProviders.Select(x => x(recipient)).ToArray();
        }

        private void AssignKillPhrase()
        {
            switch (DeathType)
            {
                default: throw new ApplicationException("Unexpected DeathType " + DeathType);
                case DeathTypeType.Suicide:
                    _killPhrase = ChoosePhrase(g_suicidePhrases);
                    _subjectNameProviders = new[] { CorpseNameProvider };
                    break;
                case DeathTypeType.Kill:
                    _killPhrase = ChoosePhrase(g_killPhrases);
                    _subjectNameProviders = new[] { ScoringPlayerNameProvider, CorpseNameProvider };
                    break;
            }
        }

        private static string ChoosePhrase(string[] phraseSet)
        {
            return phraseSet[RandomHelper.GetRandomInt(phraseSet.Length)];
        }

        private void AssignSpecialPhrase()
        {
            if (ScoringPlayer == null) return;
            if (ScoringPlayer.KillsWithoutDying < 3) return;
            var hypePhrase =
                ScoringPlayer.KillsWithoutDying < 6 ? "is on fire" :
                ScoringPlayer.KillsWithoutDying < 12 ? "is unstoppable" :
                ScoringPlayer.KillsWithoutDying < 24 ? "wreaks havoc" :
                "rules everyone";
            var randomOmg = RandomHelper.GetRandomFloat() < 0.6f ? "" : ", " + g_omgs[RandomHelper.GetRandomInt(g_omgs.Length)];
            _specialPhrase = string.Format("{0} {1} with {2} kills{3}!", ScoringPlayer.Name, hypePhrase, ScoringPlayer.KillsWithoutDying, randomOmg);
        }
    }

#if DEBUG
    [TestFixture]
    public class CoronerTests
    {
        private Player _player1, _player2, _player3;
        private Gob _gob1, _gob2, _gob2Nature, _gob1DamagedBy2;
        private Arena _arena;

        [SetUp]
        public void Setup()
        {
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
            Assert.AreEqual(null, coroner.ScoringPlayer);
            Assert.AreEqual(Coroner.DeathTypeType.Suicide, coroner.DeathType);
            Assert.AreEqual(deathMessage, coroner.MessageToCorpse);
            Assert.AreEqual(bystanderMessage, coroner.MessageToBystander);
        }

        private void AssertKillMessages(string killPhrase, string killMessage, string deathMessage, string bystanderMessage)
        {
            Coroner.SetPhraseSets(new[] { "dummy" }, new[] { killPhrase }, new string[0]);
            var info = new DamageInfo(_gob2).Bind(_gob1, TimeSpan.FromSeconds(10));
            var coroner = new Coroner(info);
            Assert.AreEqual(_player2, coroner.ScoringPlayer);
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
                Assert.AreEqual(expectedScoringPlayer, coroner.ScoringPlayer);
            };
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, DamageInfo.Unspecified), null, Coroner.DeathTypeType.Suicide);
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, getInfo(null)), null, Coroner.DeathTypeType.Suicide);
            assertCoroner(getCoronerFromInfo(_player1, getInfo(null), DamageInfo.Unspecified), null, Coroner.DeathTypeType.Suicide);
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, getInfo(_player1)), null, Coroner.DeathTypeType.Suicide);
            assertCoroner(getCoronerFromInfo(_player1, getInfo(_player1), DamageInfo.Unspecified), null, Coroner.DeathTypeType.Suicide);
            assertCoroner(getCoronerFromInfo(_player1, DamageInfo.Unspecified, getInfo(_player2)), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromInfo(_player1, getInfo(_player2), DamageInfo.Unspecified), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, null, null), null, Coroner.DeathTypeType.Suicide);
            assertCoroner(getCoronerFromPlayer(_player1, null, _player1), null, Coroner.DeathTypeType.Suicide);
            assertCoroner(getCoronerFromPlayer(_player1, null, _player2), _player2, Coroner.DeathTypeType.Kill);
            assertCoroner(getCoronerFromPlayer(_player1, _player1, null), null, Coroner.DeathTypeType.Suicide);
            assertCoroner(getCoronerFromPlayer(_player1, _player1, _player1), null, Coroner.DeathTypeType.Suicide);
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
            Assert.AreEqual(Coroner.DeathTypeType.Suicide, coroner.DeathType);
            Assert.AreEqual(null, coroner.ScoringPlayer);
        }

        [Test]
        public void TestBystandersKill()
        {
            var info = new DamageInfo(_gob2).Bind(_gob1, TimeSpan.FromSeconds(10));
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystanders(new[] { _player1, _player2, _player3 }).ToArray();
            Assert.AreEqual(new[] { _player3 }, bystanders);
        }

        [Test]
        public void TestBystandersUnspecifiedDeath()
        {
            var info = DamageInfo.Unspecified.Bind(_gob1, TimeSpan.FromSeconds(10));
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystanders(new[] { _player1, _player2, _player3 }).ToArray();
            Assert.AreEqual(new[] { _player2, _player3 }, bystanders);
        }

        [Test]
        public void TestBystandersSuicide()
        {
            var info = new DamageInfo(_gob1).Bind(_gob1, TimeSpan.FromSeconds(10));
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystanders(new[] { _player1, _player2, _player3 }).ToArray();
            Assert.AreEqual(new[] { _player2, _player3 }, bystanders);
        }

        [Test]
        public void TestBystandersKillLastDamager()
        {
            var info = DamageInfo.Unspecified.Bind(_gob1DamagedBy2, TimeSpan.FromSeconds(11));
            var coroner = new Coroner(info);
            var bystanders = coroner.GetBystanders(new[] { _player1, _player2, _player3 }).ToArray();
            Assert.AreEqual(new[] { _player3 }, bystanders);
        }
    }
#endif
}
