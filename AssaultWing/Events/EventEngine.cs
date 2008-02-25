using System;
using System.Collections.Generic;
using System.Text;

namespace AW2.Events
{
    /// <summary>
    /// Interface for Event Engine.
    /// </summary>
    interface EventEngine
    {
        /// <summary>
        /// Pushes an event to the event queue.
        /// </summary>
        /// <param name="eventToSend">The event to push.</param>
        void SendEvent(Event eventToSend);

        /// <summary>
        /// Returns the oldest event of the given type from the event queue,
        /// or null if there are no events of the given type.
        /// </summary>
        /// <param name="type">The type of event to get.</param>
        /// <returns>The oldest event of that type, or null if no such events are waiting.</returns>
        Event GetEvent(Type type);
    }
}
