using System;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// that playing will start for the previously loaded arena.
    /// </summary>
    [MessageType(0x29, false)]
    public class ArenaStartRequest : GameplayMessage
    {
    }
}
