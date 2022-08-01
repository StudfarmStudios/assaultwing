using System;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message for checking connection quality and response time
    /// during gameplay.
    /// </summary>
    [MessageType(0x2a, false)]
    public class PingRequestMessage : Message
    {
        public override MessageSendType SendType { get { return MessageSendType.UDP; } }

        /// <summary>
        /// Timestamp to send in the message. This is the time of sending
        /// the original ping request, in real time from start of game instance.
        /// </summary>
        public TimeSpan Timestamp { private get; set; }

        /// <summary>
        /// Returns a ping reply message corresponding to this received message.
        /// </summary>
        public PingReplyMessage GetPingReplyMessage(TimeSpan totalGameTime, int frameNumber)
        {
            var reply = new PingReplyMessage
            {
                Timestamp = Timestamp,
                TotalGameTimeOnReply = totalGameTime,
                FrameNumberOnReply = frameNumber,
            };
            return reply;
        }

        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                // Ping request message structure (during game):
                // long: ticks of the timestamp
                writer.Write((long)Timestamp.Ticks);
            }
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            Timestamp = TimeSpan.FromTicks(reader.ReadInt64());
        }
    }
}
