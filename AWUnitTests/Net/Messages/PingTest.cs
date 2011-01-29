using System;
using NUnit.Framework;
using System.IO;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    [TestFixture]
    public class PingTest
    {
        [Test]
        public void TestPingTimestamp()
        {
            var timestamp = new TimeSpan(12, 34, 56);
            var ping = new PingRequestMessage();
            ping.Timestamp = timestamp;

            var pingData = new byte[65536];
            var pingWriter = new NetworkBinaryWriter(new MemoryStream(pingData));
            ping.Serialize(pingWriter);
            var ping2 = (PingRequestMessage)Message.Deserialize(new ArraySegment<byte>(pingData), TimeSpan.Zero);
            var totalGameTime = new TimeSpan(23, 45, 67);
            var frameNumber = 123;
            var pong = ping2.GetPingReplyMessage(totalGameTime, frameNumber);

            var pongData = new byte[65536];
            var pongWriter = new NetworkBinaryWriter(new MemoryStream(pongData));
            pong.Serialize(pongWriter);
            var pong2 = (PingReplyMessage)Message.Deserialize(new ArraySegment<byte>(pongData), TimeSpan.Zero);
            Assert.AreEqual(timestamp, pong2.Timestamp);
            Assert.AreEqual(totalGameTime, pong2.TotalGameTimeOnReply);
            Assert.AreEqual(frameNumber, pong2.FrameNumberOnReply);
        }
    }
}
