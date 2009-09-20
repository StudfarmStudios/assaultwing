#if DEBUG
using NUnit.Framework;
#endif
using System;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A reply to a message for checking connection quality and response time
    /// during gameplay.
    /// </summary>
    public class PingReplyMessage : Message
    {
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
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x2a, true);

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Ping request message structure (during game):
            // long: timestamp originally sent in a ping request message
            // long: total game time when reply was sent, on the instance who sent the reply
            writer.Write((long)Timestamp.Ticks);
            writer.Write((long)TotalGameTimeOnReply.Ticks);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            Timestamp = TimeSpan.FromTicks(reader.ReadInt64());
            TotalGameTimeOnReply = TimeSpan.FromTicks(reader.ReadInt64());
        }
    }

    #region Unit tests
#if DEBUG
    /// <summary>
    /// Ping message test class.
    /// </summary>
    [TestFixture]
    public class PingTest
    {
        /// <summary>
        /// Tests passing of timestamp through ping messages.
        /// </summary>
        [Test]
        public void TestPingTimestamp()
        {
            TimeSpan timestamp = new TimeSpan(12, 34, 56);
            var ping = new PingRequestMessage();
            ping.Timestamp = timestamp;

            byte[] pingData = ping.Serialize();
            byte[] pingHeader = new byte[8];
            byte[] pingBody = new byte[pingData.Length - 8];
            Array.Copy(pingData, pingHeader, 8);
            Array.Copy(pingData, 8, pingBody, 0, pingBody.Length);

            var ping2 = (PingRequestMessage)Message.Deserialize(pingHeader, pingBody, 0);
            TimeSpan totalGameTime = new TimeSpan(23, 45, 67);
            var pong = ping2.GetPingReplyMessage(totalGameTime);

            byte[] pongData = pong.Serialize();
            byte[] pongHeader = new byte[8];
            byte[] pongBody = new byte[pongData.Length - 8];
            Array.Copy(pongData, pongHeader, 8);
            Array.Copy(pongData, 8, pongBody, 0, pongBody.Length);

            var pong2 = (PingReplyMessage)Message.Deserialize(pongHeader, pongBody, 0);
            Assert.AreEqual(timestamp, pong2.Timestamp);
            Assert.AreEqual(totalGameTime, pong2.TotalGameTimeOnReply);
        }
    }
#endif
    #endregion Unit tests
}
