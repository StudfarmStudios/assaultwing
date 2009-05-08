#if DEBUG
using NUnit.Framework;
#endif
using System;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A reply to a message for checking connection quality and response time.
    /// </summary>
    public class PingReplyMessage : Message
    {
        /// <summary>
        /// Contents of the message. Valid only for messages created for sending out.
        /// The contents should be half the contents of the corresponding
        /// ping request message.
        /// </summary>
        public ArraySegment<byte> Contents { private get; set; }

        /// <summary>
        /// Timestamp received in the message. Valid only for messages received 
        /// from the network. This is the timestamp that was first sent in the 
        /// original ping request.
        /// </summary>
        public TimeSpan Timestamp { get; private set; }

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x00, true);

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Ping request message structure (during game):
            // long: timestamp originally sent in a ping request message
            // some bytes: content given by an external entity
            writer.Write(Contents.Array, Contents.Offset, Contents.Count);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            Timestamp = TimeSpan.FromTicks(reader.ReadInt64());
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
            var pong = ping2.GetPingReplyMessage();

            byte[] pongData = pong.Serialize();
            byte[] pongHeader = new byte[8];
            byte[] pongBody = new byte[pongData.Length - 8];
            Array.Copy(pongData, pongHeader, 8);
            Array.Copy(pongData, 8, pongBody, 0, pongBody.Length);

            var pong2 = (PingReplyMessage)Message.Deserialize(pongHeader, pongBody, 0);
            Assert.AreEqual(timestamp, pong2.Timestamp);
        }
    }
#endif
    #endregion Unit tests
}
