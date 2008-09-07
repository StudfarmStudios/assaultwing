using System;
using System.Collections.Generic;
using System.Text;

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
        DeathCauseType type;
        Gob other;

        /// <summary>
        /// Creates a cause of death with information about the killer gob.
        /// </summary>
        /// <param name="type">The type of cause of death.</param>
        /// <param name="other">The gob that caused the death.</param>
        public DeathCause(DeathCauseType type, Gob other)
        {
            this.type = type;
            this.other = other;
        }

        /// <summary>
        /// Creates a cause of death.
        /// </summary>
        /// <param name="type">The type of cause of death.</param>
        public DeathCause(DeathCauseType type)
        {
            this.type = type;
            other = null;
        }

        /// <summary>
        /// Returns a human-readable textual representation of the cause of death.
        /// </summary>
        /// <returns>A textual representation of the cause of death.</returns>
        public override string ToString()
        {
            if (other == null || other.Owner == null)
                return type.ToString();
            return type.ToString() + " by " + other.Owner.Name;
        }

        /// <summary>
        /// Returns a human-readable textual representation of the cause of death,
        /// formatted especially for a player.
        /// </summary>
        /// <param name="player">The player to write the text for.</param>
        /// <returns>A personalised textual representation of the cause of death.</returns>
        public string ToPersonalizedString(Player player)
        {
            if (other == null)
                return type.ToString();
            if (other.Owner == null)
                return type + " by nature (" + other.TypeName + ")";
            if (other.Owner == player)
                return type + " by you";
            return type + " by " + other.Owner.Name;
        }
    }
}
