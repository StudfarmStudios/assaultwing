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
        Gob dead;
        DeathCauseType type;
        Gob other;

        /// <summary>
        /// The gob that died.
        /// </summary>
        public Gob Dead { get { return dead; } }

        /// <summary>
        /// The gob that caused the death. May be <c>null</c>.
        /// </summary>
        public Gob Killer { get { return other; } }

        /// <summary>
        /// The type of the cause of death.
        /// </summary>
        public DeathCauseType Type { get { return type; } }

        /// <summary>
        /// Is the death a suicide of a player, i.e. caused by anything but 
        /// an opposing player.
        /// </summary>
        public bool IsSuicide
        {
            get
            {
                if (dead == null || dead.Owner == null) return false;
                if (dead.LastDamager != null && dead.LastDamager.Ship != null && dead.Owner != dead.LastDamager)
                {
                    other = dead.LastDamager.Ship;
                    return false;
                } 
                if (other == null || other.Owner == null) return true;
                return dead.Owner == other.Owner;
            }
        }

        /// <summary>
        /// Is the death a kill by some player.
        /// </summary>
        public bool IsKill
        {
            get
            {
                if (dead == null || dead.Owner == null) return false;
                if (dead.LastDamager != null && dead.LastDamager.Ship != null && dead.Owner != dead.LastDamager)
                {
                    other = dead.LastDamager.Ship;
                    return true;
                }
                if (other == null || other.Owner == null) return false;
                return dead.Owner != other.Owner;
            }
        }

        /// <summary>
        /// Creates a cause of death with information about the killer gob.
        /// </summary>
        /// <param name="dead">The gob that died.</param>
        /// <param name="type">The type of cause of death.</param>
        /// <param name="other">The gob that caused the death.</param>
        public DeathCause(Gob dead, DeathCauseType type, Gob other)
        {
            this.dead = dead;
            this.type = type;
            this.other = other;
        }

        /// <summary>
        /// Creates a cause of death.
        /// </summary>
        /// <param name="dead">The gob that died.</param>
        /// <param name="type">The type of cause of death.</param>
        public DeathCause(Gob dead, DeathCauseType type)
        {
            this.dead = dead;
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
