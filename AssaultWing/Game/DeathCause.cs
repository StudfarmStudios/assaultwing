#if DEBUG
using NUnit.Framework;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;

namespace AW2.Game
{
    /// <summary>
    /// Cause of death of a gob.
    /// </summary>
    public class DeathCause
    {
        private enum RecipientType { ScoringPlayer, Bystander, Corpse };
        private delegate SubjectWord SubjectWordProvider(RecipientType recipient);

        private static string[] g_suicidePhrases;
        private static string[] g_killPhrases;
        private static string[] g_omgs;
        private string _killPhrase;
        private string _specialPhrase;
        private SubjectWordProvider[] _subjectNameProviders;
        private Gob _dead;
        private Gob _other;
        private TimeSpan _killTime;

        /// <summary>
        /// The gob that died.
        /// </summary>
        public Gob Dead { get { return _dead; } }

        /// <summary>
        /// The gob that caused the death. May be <c>null</c>.
        /// </summary>
        public Gob Killer { get { return _other; } }

        /// <summary>
        /// The player that is credited for the death. May be <c>null</c>. May not be <c>Killer.Owner</c>.
        /// </summary>
        public Player ScoringPlayer { get; private set; }

        /// <summary>
        /// Is the death a suicide of a player, i.e. caused by anything but 
        /// an opposing player.
        /// </summary>
        public bool IsSuicide
        {
            get
            {
                if (_dead.Owner == null) return false;
                return ScoringPlayer == null || ScoringPlayer == _dead.Owner;
            }
        }

        /// <summary>
        /// Is the death a kill by some player.
        /// </summary>
        public bool IsKill
        {
            get
            {
                if (_dead.Owner == null) return false;
                return ScoringPlayer != null && ScoringPlayer != _dead.Owner;
            }
        }

        public bool IsSpecial { get { return _specialPhrase != null; } }

        public string MessageToScoringPlayer { get { return GetMessageFor(RecipientType.ScoringPlayer); } }
        public string MessageToBystander { get { return GetMessageFor(RecipientType.Bystander); } }
        public string MessageToCorpse { get { return GetMessageFor(RecipientType.Corpse); } }

        /// <summary>
        /// Special message for everyone. Defined only if <see cref="IsSpecial"/> is true.
        /// </summary>
        public string SpecialMessage { get { return _specialPhrase; } }

        private SubjectWord ScoringPlayerName { get { return SubjectWord.FromProperNoun(ScoringPlayer != null ? ScoringPlayer.Name : "Nature"); } }
        private SubjectWord CorpseName { get { return SubjectWord.FromProperNoun(Dead.Owner.Name); } }
        private SubjectWordProvider ScoringPlayerNameProvider { get { return recipient => recipient == RecipientType.ScoringPlayer ? SubjectWord.You : ScoringPlayerName; } }
        private SubjectWordProvider CorpseNameProvider { get { return recipient => recipient == RecipientType.Corpse ? SubjectWord.You : CorpseName; } }

        static DeathCause()
        {
            ResetPhraseSets();
        }

        /// <param name="dead">The gob that died.</param>
        /// <param name="other">The gob that caused the death.</param>
        public DeathCause(Gob dead, Gob other)
        {
            if (dead == null) throw new ArgumentNullException("dead");
            _dead = dead;
            _other = other;
            _killTime = _dead.Arena.TotalTime;
            FindScoringPlayer();
            AssignKillPhrase();
            AssignSpecialPhrase();
        }

        /// <param name="dead">The gob that died.</param>
        public DeathCause(Gob dead)
            : this(dead, null)
        {
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

        public IEnumerable<Player> GetBystanders(IEnumerable<Player> everybody)
        {
            var excluded = new[]
            {
                Dead.Owner,
                Killer == null ? null : Killer.Owner
            };
            return everybody.Except(excluded);
        }

        private string GetMessageFor(RecipientType recipient)
        {
            return string.Format(_killPhrase, SubjectNamesFor(recipient)).Capitalize();
        }

        private SubjectWord[] SubjectNamesFor(RecipientType recipient)
        {
            return _subjectNameProviders.Select(x => x(recipient)).ToArray();
        }

        private void FindScoringPlayer()
        {
            if (_dead.LastDamagerTimeout >= _killTime && _dead.LastDamager != null)
                ScoringPlayer = _dead.LastDamager;
            else
                ScoringPlayer = _other != null ? _other.Owner : null;
        }

        private void AssignKillPhrase()
        {
            if (IsSuicide)
            {
                _killPhrase = ChoosePhrase(g_suicidePhrases);
                _subjectNameProviders = new[] { CorpseNameProvider };
            }
            else
            {
                _killPhrase = ChoosePhrase(g_killPhrases);
                _subjectNameProviders = new[] { ScoringPlayerNameProvider, CorpseNameProvider };
            }
        }

        private static string ChoosePhrase(string[] phraseSet)
        {
            return phraseSet[RandomHelper.GetRandomInt(phraseSet.Length)];
        }

        private void AssignSpecialPhrase()
        {
            if (Killer == null || Killer.Owner == null) return;
            if (Killer.Owner.KillsWithoutDying < 3) return;
            var hypePhrase =
                Killer.Owner.KillsWithoutDying < 6 ? "is on fire" :
                Killer.Owner.KillsWithoutDying < 12 ? "is unstoppable" :
                Killer.Owner.KillsWithoutDying < 24 ? "wreaks havoc" :
                "rules everyone";
            var randomOmg = RandomHelper.GetRandomFloat() < 0.6f ? "" : ", " + g_omgs[RandomHelper.GetRandomInt(g_omgs.Length)];
            _specialPhrase = string.Format("{0} {1} with {2} kills{3}!", Killer.Owner.Name, hypePhrase, Killer.Owner.KillsWithoutDying, randomOmg);
        }
    }

    public class SubjectWord : IFormattable
    {
        public static SubjectWord You { get; private set; }

        private string _nominative, _genetive, _genetivePronoun, _reflexivePronoun;

        static SubjectWord()
        {
            You = new SubjectWord(nominative: "you", genetive: "your", genetivePronoun: "your", reflexiveProunoun: "yourself");
        }

        public static SubjectWord FromProperNoun(string name)
        {
            return new SubjectWord(name, name + "'s", "his", "himself");
        }

        private SubjectWord(string nominative, string genetive, string genetivePronoun, string reflexiveProunoun)
        {
            _nominative = nominative;
            _genetive = genetive;
            _genetivePronoun = genetivePronoun;
            _reflexivePronoun = reflexiveProunoun;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            switch (format)
            {
                case null: return _nominative;
                case "genetive": return _genetive;
                case "genetivePronoun": return _genetivePronoun;
                case "reflexivePronoun": return _reflexivePronoun;
                default: throw new ArgumentException("Invalid format string '" + format + "'");
            }
        }

#if DEBUG
        [TestFixture]
        public class UnitTests
        {
            private Player _player1, _player2;
            private Gob _gob1, _gob2, _gob2Nature;
            private Arena _arena;

            [SetUp]
            public void Setup()
            {
                DeathCause.ResetPhraseSets();
                _player1 = new Player(null, "Player 1", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
                _player2 = new Player(null, "Player 2", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
                _arena = new Arena();
                _gob1 = new Gob { Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
                _gob2 = new Gob { Owner = _player2, MaxDamageLevel = 100, Arena = _arena };
                _gob2Nature = new Gob { Owner = null };
            }

            private void AssertSuicideMessages(string suicidePhrase, string deathMessage, string bystanderMessage)
            {
                DeathCause.SetPhraseSets(new[] { suicidePhrase }, new[] { "dummy" }, new string[0]);
                var cause = new DeathCause(_gob1, _gob2Nature);
                Assert.AreEqual(null, cause.ScoringPlayer);
                Assert.IsFalse(cause.IsKill);
                Assert.IsTrue(cause.IsSuicide);
                Assert.AreEqual(deathMessage, cause.MessageToCorpse);
                Assert.AreEqual(bystanderMessage, cause.MessageToBystander);
            }

            private void AssertKillMessages(string killPhrase, string killMessage, string deathMessage, string bystanderMessage)
            {
                DeathCause.SetPhraseSets(new[] { "dummy" }, new[] { killPhrase }, new string[0]);
                var cause = new DeathCause(_gob1, _gob2);
                Assert.AreEqual(_player2, cause.ScoringPlayer);
                Assert.IsTrue(cause.IsKill);
                Assert.IsFalse(cause.IsSuicide);
                Assert.AreEqual(killMessage, cause.MessageToScoringPlayer);
                Assert.AreEqual(deathMessage, cause.MessageToCorpse);
                Assert.AreEqual(bystanderMessage, cause.MessageToBystander);
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
                _arena.TotalTime = TimeSpan.FromSeconds(10);
                _gob1.InflictDamage(10, new DeathCause(_gob1, _gob2));
                _arena.TotalTime = TimeSpan.FromSeconds(11);
                var cause = new DeathCause(_gob1); // no explicit killer
                Assert.IsFalse(cause.IsSuicide);
                Assert.IsTrue(cause.IsKill); // _gob2 did last recent damage
                Assert.AreEqual(_player2, cause.ScoringPlayer); // so _player2 scores
            }

            [Test]
            public void TestLastDamagerTooLate()
            {
                _arena.TotalTime = TimeSpan.FromSeconds(10);
                _gob1.InflictDamage(10, new DeathCause(_gob1, _gob2));
                _arena.TotalTime = TimeSpan.FromSeconds(30);
                var cause = new DeathCause(_gob1);
                Assert.IsTrue(cause.IsSuicide);
                Assert.IsFalse(cause.IsKill);
                Assert.AreEqual(null, cause.ScoringPlayer);
            }
        }
#endif
    }
}
