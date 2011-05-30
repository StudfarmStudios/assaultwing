using System;
using AW2.Core;
using AW2.Helpers;
using AW2.Net.Connections;
using AW2.Net.Messages;

namespace AW2.Net.ConnectionUtils
{
    /// <summary>
    /// Manages ping related information about a <see cref="Connection"/>.
    /// </summary>
    public class PingInfo
    {
        private static readonly TimeSpan PING_INTERVAL = TimeSpan.FromSeconds(1);
        private const int PING_AVERAGED_COUNT = 4;

        /// <summary>
        /// Time at which the next ping request should be sent, in real time.
        /// </summary>
        private TimeSpan _nextPingSend;

        private TimeSpan[] _pingTimes;
        private int[] _remoteFrameNumberOffsets;
        private int _nextIndex; // indexes _pingTimes and _remoteFrameNumberOffsets

        /// <summary>
        /// The connection whose ping this instance is measuring.
        /// </summary>
        public Connection BaseConnection { get; set; }

        /// <summary>
        /// Round-trip ping time of the underlying connection.
        /// </summary>
        public TimeSpan PingTime { get { return AWMathHelper.AverageWithoutExtremes(_pingTimes); } }

        /// <summary>
        /// What needs to be added to the current remote frame number to get the current local frame number.
        /// </summary>
        public int RemoteFrameNumberOffset { get { return AWMathHelper.AverageWithoutExtremes(_remoteFrameNumberOffsets); } }

        /// <summary>
        /// If true, ping time won't be updated. The old results will remain unchanged.
        /// </summary>
        public bool IsMeasuringFreezed { get; set; }

        public PingInfo(Connection baseConnection)
        {
            BaseConnection = baseConnection;
            _pingTimes = new TimeSpan[PING_AVERAGED_COUNT];
            _remoteFrameNumberOffsets = new int[PING_AVERAGED_COUNT];
        }

        public void AdjustRemoteFrameNumberOffset(int localFrameNumberShift)
        {
            for (int i = 0; i < PING_AVERAGED_COUNT; i++)
                _remoteFrameNumberOffsets[i] -= localFrameNumberShift;
        }

        /// <summary>
        /// Calling this method every frame keeps ping information up to date.
        /// </summary>
        public void Update()
        {
            var now = AssaultWingCore.Instance.GameTime.TotalGameTime;
            SendPing(now);
            ReceivePingAndSendPong();
            ReceivePong(now);
        }

        private void SendPing(TimeSpan now)
        {
            if (now < _nextPingSend || IsMeasuringFreezed) return;
            _nextPingSend = now + PING_INTERVAL;
            var pingSend = new PingRequestMessage { Timestamp = now };
            BaseConnection.Send(pingSend);
        }

        private void ReceivePingAndSendPong()
        {
            var pingReceive = BaseConnection.TryDequeueMessage<PingRequestMessage>();
            if (pingReceive == null) return;
            var pongSend = pingReceive.GetPingReplyMessage(AssaultWingCore.Instance.DataEngine.ArenaTotalTime, AssaultWingCore.Instance.DataEngine.ArenaFrameCount);
            BaseConnection.Send(pongSend);
        }

        private void ReceivePong(TimeSpan now)
        {
            var pongReceive = BaseConnection.TryDequeueMessage<PingReplyMessage>();
            if (pongReceive == null || IsMeasuringFreezed) return;
            var pingTime = now - pongReceive.Timestamp;
            _pingTimes[_nextIndex] = pingTime;
            var pongDelay = pingTime.Divide(2);
            var localFrameCountNow = AssaultWingCore.Instance.DataEngine.ArenaFrameCount;
            var remoteFrameCountNow = pongReceive.FrameNumberOnReply + pongDelay.Frames();
            _remoteFrameNumberOffsets[_nextIndex] = localFrameCountNow - remoteFrameCountNow;
            _nextIndex = (_nextIndex + 1) % _pingTimes.Length;
        }
    }
}
