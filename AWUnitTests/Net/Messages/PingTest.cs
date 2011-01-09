using System;
using NUnit.Framework;

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

            var pingData = ping.Serialize();
            var ping2 = (PingRequestMessage)Message.Deserialize(pingData, 0, TimeSpan.Zero);
            var totalGameTime = new TimeSpan(23, 45, 67);
            var frameNumber = 123;
            var pong = ping2.GetPingReplyMessage(totalGameTime, frameNumber);

            var pongData = pong.Serialize();
            var pong2 = (PingReplyMessage)Message.Deserialize(pongData, 0, TimeSpan.Zero);
            Assert.AreEqual(timestamp, pong2.Timestamp);
            Assert.AreEqual(totalGameTime, pong2.TotalGameTimeOnReply);
            Assert.AreEqual(frameNumber, pong2.FrameNumberOnReply);
        }
    }
}
