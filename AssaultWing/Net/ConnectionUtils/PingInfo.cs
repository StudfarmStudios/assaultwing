using System;
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
        private const int PING_AVERAGED_COUNT = 3;

        /// <summary>
        /// Time at which the next ping request should be sent, in real time.
        /// </summary>
        private TimeSpan _nextPingSend;

        private TimeSpan[] _pingTimes;
        private TimeSpan[] _remoteGameTimeOffsets;
        private int _nextIndex; // indexes _pingTimes and _remoteGameTimeOffsets

        /// <summary>
        /// The connection whose ping this instance is measuring.
        /// </summary>
        public Connection BaseConnection { get; set; }

        /// <summary>
        /// Round-trip ping time of the underlying connection.
        /// </summary>
        public TimeSpan PingTime { get { return AWMathHelper.Average(_pingTimes); } }

        /// <summary>
        /// Offset of game time on the remote game instance compared to this game instance.
        /// </summary>
        /// Adding the offset to the remote game time gives our game time.
        public TimeSpan RemoteGameTimeOffset { get { return AWMathHelper.Average(_remoteGameTimeOffsets); } }

        /// <summary>
        /// If true, ping time won't be updated. The old results will remaing unchanged.
        /// </summary>
        public bool IsMeasuringFreezed { get; set; }

        public PingInfo(Connection baseConnection)
        {
            BaseConnection = baseConnection;
            _pingTimes = new TimeSpan[PING_AVERAGED_COUNT];
            _remoteGameTimeOffsets = new TimeSpan[PING_AVERAGED_COUNT];
        }

        /// <summary>
        /// Calling this method every frame keeps ping information up to date.
        /// </summary>
        public void Update()
        {
            var now = AssaultWingCore.Instance.GameTime.TotalRealTime;
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
            var pingReceive = BaseConnection.Messages.TryDequeue<PingRequestMessage>();
            if (pingReceive == null) return;
            var pongSend = pingReceive.GetPingReplyMessage(AssaultWingCore.Instance.DataEngine.ArenaTotalTime);
            BaseConnection.Send(pongSend);
        }

        private void ReceivePong(TimeSpan now)
        {
            var pongReceive = BaseConnection.Messages.TryDequeue<PingReplyMessage>();
            if (pongReceive == null || IsMeasuringFreezed) return;
            var pingTime = now - pongReceive.Timestamp;
            _pingTimes[_nextIndex] = pingTime;
            _remoteGameTimeOffsets[_nextIndex] =
                AssaultWingCore.Instance.DataEngine.ArenaTotalTime
                - pongReceive.TotalGameTimeOnReply
                - pingTime.Divide(2);
            _nextIndex = (_nextIndex + 1) % _pingTimes.Length;
        }
    }
}
