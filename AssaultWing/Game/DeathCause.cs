using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Core;
using AW2.Helpers;

namespace AW2.Game
{
    /// <summary>
    /// Type of cause of death.
    /// </summary>
    public enum DeathCauseType
    {
        /// <summary>
        /// Cause of death: some other reason.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Cause of death: damage inflicted by a collision into some other gob.
        /// </summary>
        Collision,

        /// <summary>
        /// Cause of death: some other gob manifested its characteristic behaviour by inflicting damage.
        /// </summary>
        Damage,
    }

    /// <summary>
    /// Cause of death of a gob.
    /// </summary>
    public class DeathCause
    {
        public static readonly TimeSpan LAST_DAMAGER_KILL_TIMEWINDOW = TimeSpan.FromSeconds(6);
        private static readonly string[] g_killPhrases = new[]
        {
            "{0} nailed {1}", "{0} put {1} to rest", "{0} did {1} in",  "{0} iced {1}",
            "{0} put {1} on his knees", "{0} terminated {1}", "{0} crushed {1}", "{0} destroyed {1}",
            "{0} ran over {1}", "{0} showed {1} how it's done", "{0} taught {1} a lesson",
            "{0} made {1} appreciate life", "{0} survived, {1} didn't", "{0} stepped on {1}'s foot",
        };
        private static readonly string[] g_omgs = new[]
        {
            "OMG!", "W00T!", "WHOA!", "GROOVY!", "WICKED!", "AWESOME!", "INSANE!", "SLAMMIN'!",
            "CRACKIN'!", "KINKY!", "JIGGY!", "NEAT!", "FAR OUT!", "SLICK!", "SMOKING!", "SOLID!",
            "SPIFFY!", "CHICKY!", "COOL!", "L33T!",
        };
        private string _killPhrase;
        private string _specialPhrase;
        private Gob _dead;
        private DeathCauseType _type;
        private Gob _other;

        /// <summary>
        /// The gob that died.
        /// </summary>
        public Gob Dead { get { return _dead; } }

        /// <summary>
        /// The gob that caused the death. May be <c>null</c>.
        /// </summary>
        public Gob Killer { get { return _other; } }

        /// <summary>
        /// The type of the cause of death.
        /// </summary>
        public DeathCauseType Type { get { return _type; } }

        /// <summary>
        /// Is the death a suicide of a player, i.e. caused by anything but 
        /// an opposing player.
        /// </summary>
        public bool IsSuicide
        {
            get
            {
                if (_dead == null || _dead.Owner == null) return false;
                if (_dead.LastDamager != null && _dead.LastDamager.Ship != null && _dead.Owner != _dead.LastDamager &&
                    _dead.LastDamagerTime + LAST_DAMAGER_KILL_TIMEWINDOW > AssaultWingCore.Instance.DataEngine.ArenaTotalTime)
                {
                    _other = _dead.LastDamager.Ship;
                    return false;
                }
                if (_other == null || _other.Owner == null) return true;
                return _dead.Owner == _other.Owner;
            }
        }

        /// <summary>
        /// Is the death a kill by some player.
        /// </summary>
        public bool IsKill
        {
            get
            {
                if (_dead == null || _dead.Owner == null) return false;
                if (_dead.LastDamager != null && _dead.LastDamager.Ship != null && _dead.Owner != _dead.LastDamager &&
                    _dead.LastDamagerTime + LAST_DAMAGER_KILL_TIMEWINDOW > AssaultWingCore.Instance.DataEngine.ArenaTotalTime)
                {
                    _other = _dead.LastDamager.Ship;
                    return true;
                }
                if (_other == null || _other.Owner == null) return false;
                return _dead.Owner != _other.Owner;
            }
        }

        public bool IsSpecial { get { return _specialPhrase != null; } }

        /// <summary>
        /// Message for the killer player.
        /// </summary>
        public string KillMessage { get { return string.Format(_killPhrase, "You", Dead.Owner.Name); } }

        /// <summary>
        /// Message for bystanders.
        /// </summary>
        public string BystanderMessage { get { return string.Format(_killPhrase, KillerName, Dead.Owner.Name); } }

        /// <summary>
        /// Message for bystanders.
        /// </summary>
        public string DeathMessage { get { return string.Format(_killPhrase, KillerName, "you"); } }

        /// <summary>
        /// Special message for everyone. Defined only if <see cref="IsSpecial"/> is true.
        /// </summary>
        public string SpecialMessage { get { return string.Format(_specialPhrase, KillerName, Dead.Owner.Name); } }

        private string KillerName { get { return Killer != null && Killer.Owner != null ? Killer.Owner.Name : "Nature"; } }

        /// <param name="dead">The gob that died.</param>
        /// <param name="type">The type of cause of death.</param>
        /// <param name="other">The gob that caused the death.</param>
        public DeathCause(Gob dead, DeathCauseType type, Gob other)
        {
            _dead = dead;
            _type = type;
            _other = other;
            _killPhrase = g_killPhrases[RandomHelper.GetRandomInt(g_killPhrases.Length)];
            AssignSpecialPhrase();
        }

        /// <param name="dead">The gob that died.</param>
        /// <param name="type">The type of cause of death.</param>
        public DeathCause(Gob dead, DeathCauseType type)
            : this(dead, type, null)
        {
        }

        public override string ToString()
        {
            if (_other == null || _other.Owner == null)
                return _type.ToString();
            return _type.ToString() + " by " + _other.Owner.Name;
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

        private void AssignSpecialPhrase()
        {
            if (Killer == null || Killer.Owner == null) return;
            if (Killer.Owner.KillsWithoutDying < 3) return;
            var hypePhrase =
                Killer.Owner.KillsWithoutDying < 6 ? "IS ON FIRE" :
                Killer.Owner.KillsWithoutDying < 12 ? "IS UNSTOPPABLE" :
                Killer.Owner.KillsWithoutDying < 24 ? "WREAKS HAVOC" :
                "RULES EVERYONE";
            var randomOmg = RandomHelper.GetRandomFloat() < 0.6f ? "" : g_omgs[RandomHelper.GetRandomInt(g_omgs.Length)];
            _specialPhrase = string.Format("{0} {1} ({2} kills) {3}", Killer.Owner.Name, hypePhrase, Killer.Owner.KillsWithoutDying, randomOmg);
        }
    }
}
