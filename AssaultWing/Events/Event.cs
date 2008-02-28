using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Events
{
    /// <summary>Event base class, all events must inherit this</summary>
    /// Not a really usable class on it's own as it contains no other info than time of event.
    public class Event
    {
        /// <summary>
        /// Time at which the event is to be processed, or 
        /// <b>0</b> if the event is to be processed immediately.
        /// Measured in game time.
        /// </summary>
        protected TimeSpan eventTime;

        /// <summary>
        /// Time at which the event is to be processed, or 
        /// <b>0</b> if the event is to be processed immediately.
        /// Measured in game time.
        /// </summary>
        public TimeSpan EventTime { get { return eventTime; } set { eventTime = value; } }

        /// <summary>
        /// Creates a new event.
        /// </summary>
        /// <param name="eventTime">Undefined.</param>
        public Event(TimeSpan eventTime)
        {
            this.eventTime = eventTime;
        }

        /// <summary>
        /// Creates a new event.
        /// </summary>
        public Event()
        {
        }
    }
}
