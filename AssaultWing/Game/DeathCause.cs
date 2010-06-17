using System;

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
    public struct DeathCause
    {
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
                if (_dead.LastDamager != null && _dead.LastDamager.Ship != null && _dead.Owner != _dead.LastDamager)
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
                if (_dead.LastDamager != null && _dead.LastDamager.Ship != null && _dead.Owner != _dead.LastDamager)
                {
                    _other = _dead.LastDamager.Ship;
                    return true;
                }
                if (_other == null || _other.Owner == null) return false;
                return _dead.Owner != _other.Owner;
            }
        }

        /// <param name="dead">The gob that died.</param>
        /// <param name="type">The type of cause of death.</param>
        /// <param name="other">The gob that caused the death.</param>
        public DeathCause(Gob dead, DeathCauseType type, Gob other)
        {
            _dead = dead;
            _type = type;
            _other = other;
        }

        /// <param name="dead">The gob that died.</param>
        /// <param name="type">The type of cause of death.</param>
        public DeathCause(Gob dead, DeathCauseType type)
        {
            _dead = dead;
            _type = type;
            _other = null;
        }

        public override string ToString()
        {
            if (_other == null || _other.Owner == null)
                return _type.ToString();
            return _type.ToString() + " by " + _other.Owner.Name;
        }

        /// <summary>
        /// Returns a human-readable textual representation of the cause of death,
        /// formatted especially for a player.
        /// </summary>
        /// <param name="player">The player to write the text for.</param>
        /// <returns>A personalised textual representation of the cause of death.</returns>
        public string ToPersonalizedString(Player player)
        {
            if (_other == null)
                return _type.ToString();
            if (_other.Owner == null)
                return _type + " by nature (" + _other.TypeName + ")";
            if (_other.Owner == player)
                return _type + " by you";
            return _type + " by " + _other.Owner.Name;
        }
    }
}
