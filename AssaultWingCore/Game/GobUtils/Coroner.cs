using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Analyses the death of a gob based on the BoundDamageInfo about
    /// the last damage the gob received.
    /// </summary>
    public class Coroner
    {
        public enum DeathTypeType { Suicide, Kill }; // TODO: rename Suicide -> Accident
        private enum RecipientType { ScoringPlayer, Bystander, Corpse };
        private delegate SubjectWord SubjectWordProvider(RecipientType recipient);

        private static string[] g_suicidePhrases;
        private static string[] g_killPhrases;
        private static string[] g_omgs;
        private string _killPhrase;
        private string _specialPhrase;
        private SubjectWordProvider[] _subjectNameProviders;

        public AssaultWingCore Game { get { return DamageInfo.Target.Game; } }
        public BoundDamageInfo DamageInfo { get; private set; }

        /// <summary>
        /// The reason of the death.
        /// </summary>
        public DeathTypeType DeathType { get; private set; }

        /// <summary>
        /// The one who gets a frag for the death. May be <c>null</c> if no-one gets a frag.
        /// </summary>
        public Spectator ScoringSpectator { get; private set; }

        /// <summary>
        /// The one whose gob was killed. May be <c>null</c>.
        /// </summary>
        public Spectator KilledSpectator { get; private set; }

        public string MessageToScoringPlayer { get { return GetMessageFor(RecipientType.ScoringPlayer); } }
        public string MessageToBystander { get { return GetMessageFor(RecipientType.Bystander); } }
        public string MessageToCorpse { get { return GetMessageFor(RecipientType.Corpse); } }

        /// <summary>
        /// Special message for everyone. May be <c>null</c>.
        /// </summary>
        public string SpecialMessage { get { return _specialPhrase; } }

        private SubjectWord ScoringPlayerName { get { return SubjectWord.FromProperNoun(ScoringSpectator != null ? ScoringSpectator.Name : "Nature"); } }
        private SubjectWord CorpseName { get { return SubjectWord.FromProperNoun(KilledSpectator.Name); } }
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
            DamageInfo = info;
            AnalyzeDeath(info);
            HandleMinionDeath(info);
            AssignKillPhrase();
            AssignSpecialPhrase();
        }

        public IEnumerable<Player> GetBystandingPlayers(IEnumerable<Player> everybody)
        {
            return everybody.Except(new[] { KilledSpectator, ScoringSpectator }.OfType<Player>());
        }

        private void AnalyzeDeath(BoundDamageInfo info)
        {
            Action markAsSuicide = () =>
            {
                DeathType = DeathTypeType.Suicide;
                ScoringSpectator = null;
            };
            Action<Spectator> markAsKill = killer =>
            {
                if (killer == null)
                    markAsSuicide();
                else
                {
                    DeathType = DeathTypeType.Kill;
                    ScoringSpectator = killer;
                }
            };
            if (info.SourceType == BoundDamageInfo.SourceTypeType.EnemyPlayer)
                markAsKill(info.Cause.Owner);
            else if (info.Target.LastDamagerTimeout > info.Time && info.Target.LastDamager != null)
                markAsKill(info.Target.LastDamager);
            else
                markAsSuicide();
            KilledSpectator = info.Target.Owner;
        }

        private void HandleMinionDeath(BoundDamageInfo info)
        {
            if (Game.NetworkMode == NetworkMode.Client || KilledSpectator == null || !KilledSpectator.Minions.Contains(info.Target)) return;
            switch (DeathType)
            {
                default: throw new ApplicationException("Unexpected DeathType " + DeathType);
                case Coroner.DeathTypeType.Suicide:
                    break;
                case Coroner.DeathTypeType.Kill:
                    ScoringSpectator.ArenaStatistics.Kills++;
                    ScoringSpectator.ArenaStatistics.KillsWithoutDying++;
                    break;
            }
            KilledSpectator.ArenaStatistics.Deaths++;
            KilledSpectator.ArenaStatistics.Lives--;
            KilledSpectator.ArenaStatistics.KillsWithoutDying = 0;
            Game.DataEngine.EnqueueArenaStatisticsToClients();
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
            if (ScoringSpectator == null) return;
            if (ScoringSpectator.ArenaStatistics.KillsWithoutDying < 3) return;
            var hypePhrase =
                ScoringSpectator.ArenaStatistics.KillsWithoutDying < 6 ? "is on fire" :
                ScoringSpectator.ArenaStatistics.KillsWithoutDying < 12 ? "is unstoppable" :
                ScoringSpectator.ArenaStatistics.KillsWithoutDying < 24 ? "wreaks havoc" :
                "rules everyone";
            var randomOmg = RandomHelper.GetRandomFloat() < 0.6f ? "" : ", " + g_omgs[RandomHelper.GetRandomInt(g_omgs.Length)];
            _specialPhrase = string.Format("{0} {1} with {2} kills{3}!", ScoringSpectator.Name, hypePhrase, ScoringSpectator.ArenaStatistics.KillsWithoutDying, randomOmg);
        }
    }
}
