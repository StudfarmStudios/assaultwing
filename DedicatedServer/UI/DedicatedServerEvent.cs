using System;

namespace AW2.UI
{

    public struct DedicatedServerEvent
    {
        public enum EventType
        {
            Stop,
            EndRound,
            Say,
            SelectNextArena,
            Kick,
            Players,
            SetBotsEnabled,
            SetRoundLength
        }

        public EventType Type;

        // NOTE: Since I can't efficiently figure out how to do a proper tagged
        // union, we simply have all necessary payloads types here and the
        // property Type implicitly determines which one is used. It is not a
        // huge problem because the correct payload mostly self explanatory. Of
        // course a proper discriminated union / algebraic datatype would be
        // better ;)

        public string StringPayload;
        public string StringPayload2;
        public TimeSpan TimeSpanPayload;
        public bool BooleanPayload;

        /// <summary>
        /// A string describing what issued this command. Appeneded to messages
        /// to let players know what is going on.
        /// </summary>
        public string EventSourceMessage;
    }

}
