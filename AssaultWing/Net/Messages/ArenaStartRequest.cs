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
        /// <summary>
        /// Amount of real time the client should wait from receiving this message before starting the arena.
        /// </summary>
        public TimeSpan StartDelay { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            base.Serialize(writer);
            // Arena start request structure:
            // TimeSpan: start delay
            writer.Write(StartDelay);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            base.Deserialize(reader);
            StartDelay = reader.ReadTimeSpan();
        }

        public override string ToString()
        {
            return base.ToString() + " [Delay " + StartDelay + "]";
        }
    }
}
