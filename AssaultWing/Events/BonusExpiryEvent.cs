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

        /// <summary>
        /// The name of the player whose bonus we are signalling about.
        /// </summary>
        public string PlayerName { get { return playerName; } set { playerName = value; } }
    }
}
