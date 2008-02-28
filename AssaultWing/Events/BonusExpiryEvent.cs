using System;
using System.Collections.Generic;
using System.Text;
using AW2.Game;

namespace AW2.Events
{
    /// <summary>
    /// An event of the expiration of a player's bonus.
    /// </summary>
    class BonusExpiryEvent : Event
    {
        string playerName;
        PlayerBonus bonus;

        /// <summary>
        /// The name of the player whose bonus we are signalling about.
        /// </summary>
        public string PlayerName { get { return playerName; } set { playerName = value; } }

        /// <summary>
        /// The type of bonus we are signalling about.
        /// </summary>
        public PlayerBonus Bonus { get { return bonus; } set { bonus = value; } }

        public BonusExpiryEvent(string playerName, PlayerBonus bonus, TimeSpan expiryTime)
            : base(expiryTime)
        {
            this.playerName = playerName;
            this.bonus = bonus;
        }
    }
}
