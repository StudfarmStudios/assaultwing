using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Net
{
    /// <summary>
    /// Message about the state of an ongoing game.
    /// </summary>
    /// A gameplay message carries a piece of information about the state of
    /// an ongoing game session at a particular point in game time. Some
    /// gameplay messages may become irrelevant as they grow old.
    public abstract class GameplayMessage : StreamMessage
    {
        /// <summary>
        /// Total elapsed game time at the point in game time the message is 
        /// current.
        /// </summary>
        public TimeSpan TotalGameTime { get; set; }

        /// <summary>
        /// Creates a gameplay message, initialising its timestamp to the
        /// current time of gameplay.
        /// </summary>
        public GameplayMessage()
        {
            TotalGameTime = AssaultWing.Instance.GameTime.TotalGameTime;
        }
    }
}
