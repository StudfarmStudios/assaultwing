using System;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message for checking connection quality and response time.
    /// </summary>
    public class PingRequestMessage : Message
    {
        /// <summary>
        /// Received payload. Used only for deserialised messages.
        /// </summary>
        byte[] receivedBytes;

        /// <summary>
        /// Timestamp to send in the message. This is the time of sending
        /// the original ping request, in real time from start of game instance.
        /// </summary>
        public TimeSpan Timestamp { private get; set; }

        /// <summary>
        /// Returns a ping reply message corresponding to this received message.
        /// </summary>
        public PingReplyMessage GetPingReplyMessage()
        {
            if (receivedBytes == null)
                throw new InvalidOperationException("Cannot create ping reply message for ping request message that has not been received");
            var reply = new PingReplyMessage();
            reply.Contents = new ArraySegment<byte>(receivedBytes, 0, receivedBytes.Length / 2);
            return reply;
        }

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x00, false);

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Ping request message structure (during game):
            // long: ticks of the timestamp
            // 100 bytes: unspecified data
            writer.Write((long)Timestamp.Ticks);
            writer.Write("PINGpingpingping -- pingpingpingping -- pingpingpingping -- pingpingpingping -- pingpingpingping!!!!", 100, false);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            receivedBytes = reader.ReadBytes(8 + 100);
        }
    }
}
