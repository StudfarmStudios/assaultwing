using System;
using System.Linq;
using AW2.Helpers;
using AW2.Helpers.Collections;
using AW2.Net.Messages;

namespace AW2.Net
{
    /// <summary>
    /// A connection that has automatic ping time measuring.
    /// </summary>
    public class PingedConnection : IConnection
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
        /// The underlying connection.
        /// </summary>
        public Connection BaseConnection { get; set; }

        /// <summary>
        /// Round-trip ping time of the underlying connection.
        /// </summary>
        public TimeSpan PingTime { get { return TimeSpan.FromTicks(_pingTimes.Sum(pingTime => pingTime.Ticks) / _pingTimes.Length); } }

        /// <summary>
        /// Offset of game time on the remote game instance compared to this game instance.
        /// </summary>
        /// Adding the offset to the remote game time gives our game time.
        public TimeSpan RemoteGameTimeOffset { get { return TimeSpan.FromTicks(_remoteGameTimeOffsets.Sum(pingTime => pingTime.Ticks) / _remoteGameTimeOffsets.Length); } }

        /// <summary>
        /// Unique identifier of the connection. Nonnegative.
        /// </summary>
        public int ID { get { return BaseConnection.ID; } }

        /// <summary>
        /// Short, human-readable name of the connection.
        /// </summary>
        public string Name { get { return BaseConnection.Name; } set { BaseConnection.Name = value; } }

        public bool IsDisposed { get { return BaseConnection.IsDisposed; } }

        /// <summary>
        /// Received messages that are waiting for consumption by the client program.
        /// </summary>
        public ITypedQueue<Message> Messages { get { return BaseConnection.Messages; } }

        /// <summary>
        /// Creates a pinged connection from an established connection.
        /// </summary>
        /// <param name="baseConnection">The underlying connection to
        /// equip with automatic ping time measurement.</param>
        public PingedConnection(Connection baseConnection)
        {
            this.BaseConnection = baseConnection;
            _pingTimes = new TimeSpan[PING_AVERAGED_COUNT];
            _remoteGameTimeOffsets = new TimeSpan[PING_AVERAGED_COUNT];
        }

        public void Update()
        {
            var now = AssaultWing.Instance.GameTime.TotalRealTime;

            // Send ping requests every now and then.
            if (now >= _nextPingSend)
            {
                _nextPingSend = now + PING_INTERVAL;
                var pingSend = new PingRequestMessage();
                pingSend.Timestamp = now;
                BaseConnection.Send(pingSend);
            }

            // Respond to received ping requests.
            var pingReceive = BaseConnection.Messages.TryDequeue<PingRequestMessage>();
            if (pingReceive != null)
            {
                var pongSend = pingReceive.GetPingReplyMessage(AssaultWing.Instance.DataEngine.ArenaTotalTime);
                BaseConnection.Send(pongSend);
            }

            // Respond to received ping replies.
            var pongReceive = BaseConnection.Messages.TryDequeue<PingReplyMessage>();
            if (pongReceive != null)
            {
                var pingTime = now - pongReceive.Timestamp;
                _pingTimes[_nextIndex] = pingTime;
                _remoteGameTimeOffsets[_nextIndex] =
                    AssaultWing.Instance.DataEngine.ArenaTotalTime
                    - pongReceive.TotalGameTimeOnReply
                    - pingTime.Divide(2);
                _nextIndex = (_nextIndex + 1) % _pingTimes.Length;
            }
        }

        public void Send(Message message)
        {
            BaseConnection.Send(message);
        }

        public void Dispose()
        {
            BaseConnection.Dispose();
        }

        public void HandleErrors()
        {
            BaseConnection.HandleErrors();
        }

        public int GetSendQueueSize()
        {
            return BaseConnection.GetSendQueueSize();
        }
    }
}
