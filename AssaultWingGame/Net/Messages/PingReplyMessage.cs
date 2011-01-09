using System;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A reply to a message for checking connection quality and response time
    /// during gameplay.
    /// </summary>
    [MessageType(0x2a, true)]
    public class PingReplyMessage : Message
    {
        public override MessageSendType SendType { get { return MessageSendType.UDP; } }

        /// <summary>
        /// Timestamp received in the message. Valid only for messages received 
        /// from the network. This is the timestamp that was first sent in the 
        /// original ping request.
        /// </summary>
        public TimeSpan Timestamp { get; set; }

        /// <summary>
        /// Total game time at the remote game instance at the time the reply
        /// was sent.
        /// </summary>
        public TimeSpan TotalGameTimeOnReply { get; set; }

        /// <summary>
        /// Frame number at the remote game instance at the time the reply was sent.
        /// </summary>
        public int FrameNumberOnReply { get; set; }

        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Ping request message structure (during game):
            // TimeSpan: timestamp originally sent in a ping request message
            // TimeSpan: total game time when reply was sent, on the instance who sent the reply
            // int: frame number when reply was sent, on the instance who sent the reply
            writer.Write((TimeSpan)Timestamp);
            writer.Write((TimeSpan)TotalGameTimeOnReply);
            writer.Write((int)FrameNumberOnReply);
        }

        protected override void Deserialize(NetworkBinaryReader reader)
        {
            Timestamp = reader.ReadTimeSpan();
            TotalGameTimeOnReply = reader.ReadTimeSpan();
            FrameNumberOnReply = reader.ReadInt32();
        }
    }
}
