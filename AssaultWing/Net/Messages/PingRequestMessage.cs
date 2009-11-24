using System;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message for checking connection quality and response time
    /// during gameplay.
    /// </summary>
    public class PingRequestMessage : Message
    {
        /// <summary>
        /// Timestamp to send in the message. This is the time of sending
        /// the original ping request, in real time from start of game instance.
        /// </summary>
        public TimeSpan Timestamp { private get; set; }

        /// <summary>
        /// Returns a ping reply message corresponding to this received message.
        /// </summary>
        public PingReplyMessage GetPingReplyMessage(TimeSpan elapsedGameTime)
        {
            var reply = new PingReplyMessage();
            reply.Timestamp = Timestamp;
            reply.TotalGameTimeOnReply = elapsedGameTime;
            return reply;
        }

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x2a, false);

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Ping request message structure (during game):
            // long: ticks of the timestamp
            writer.Write((long)Timestamp.Ticks);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            Timestamp = TimeSpan.FromTicks(reader.ReadInt64());
        }
    }
}
