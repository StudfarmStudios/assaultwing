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
        static readonly TimeSpan PING_INTERVAL = TimeSpan.FromSeconds(1);
        const int PING_AVERAGED_COUNT = 3;

        /// <summary>
        /// Time at which the next ping request should be sent, in real time.
        /// </summary>
        TimeSpan nextPingSend;

        TimeSpan[] pingTimes;
        TimeSpan[] remoteGameTimeOffsets;
        int nextIndex; // indexes pingTimes and remoteGameTimeOffsets

        /// <summary>
        /// The underlying connection.
        /// </summary>
        public Connection BaseConnection { get; set; }

        /// <summary>
        /// Round-trip ping time of the underlying connection.
        /// </summary>
        public TimeSpan PingTime { get { return TimeSpan.FromTicks(pingTimes.Sum(pingTime => pingTime.Ticks) / pingTimes.Length); } }

        /// <summary>
        /// Offset of game time on the remote game instance compared to this game instance.
        /// </summary>
        /// Adding the offset to the remote game time gives our game time.
        public TimeSpan RemoteGameTimeOffset { get { return TimeSpan.FromTicks(remoteGameTimeOffsets.Sum(pingTime => pingTime.Ticks) / remoteGameTimeOffsets.Length); } }

        /// <summary>
        /// Unique identifier of the connection. Nonnegative.
        /// </summary>
        public int Id { get { return BaseConnection.Id; } }

        /// <summary>
        /// Short, human-readable name of the connection.
        /// </summary>
        public string Name { get { return BaseConnection.Name; } set { BaseConnection.Name = value; } }

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
            pingTimes = new TimeSpan[PING_AVERAGED_COUNT];
            remoteGameTimeOffsets = new TimeSpan[PING_AVERAGED_COUNT];
        }

        /// <summary>
        /// Updates the ping time measurement.
        /// </summary>
        public void Update()
        {
            TimeSpan now = AssaultWing.Instance.GameTime.TotalRealTime;

            // Send ping requests every now and then.
            if (now >= nextPingSend)
            {
                nextPingSend = now + PING_INTERVAL;
                var pingSend = new PingRequestMessage();
                pingSend.Timestamp = now;
                BaseConnection.Send(pingSend);
            }

            // Respond to received ping requests.
            var pingReceive = BaseConnection.Messages.TryDequeue<PingRequestMessage>();
            if (pingReceive != null)
            {
                var pongSend = pingReceive.GetPingReplyMessage(AssaultWing.Instance.GameTime.TotalArenaTime);
                BaseConnection.Send(pongSend);
            }

            // Respond to received ping replies.
            var pongReceive = BaseConnection.Messages.TryDequeue<PingReplyMessage>();
            if (pongReceive != null)
            {
                var pingTime = now - pongReceive.Timestamp;
                pingTimes[nextIndex] = pingTime;
                remoteGameTimeOffsets[nextIndex] =
                    AssaultWing.Instance.GameTime.TotalArenaTime
                    - pongReceive.TotalGameTimeOnReply
                    - pingTime.Divide(2);
                nextIndex = (nextIndex + 1) % pingTimes.Length;
            }
        }

        /// <summary>
        /// Sends a message to the remote host. The message is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        public void Send(Message message)
        {
            BaseConnection.Send(message);
        }

        /// <summary>
        /// Closes the connection and frees resources it has allocated.
        /// </summary>
        public void Dispose()
        {
            BaseConnection.Dispose();
        }

        /// <summary>
        /// Reacts to errors that may have occurred during the connection's
        /// operation in background threads.
        /// </summary>
        public void HandleErrors()
        {
            BaseConnection.HandleErrors();
        }

        /// <summary>
        /// Returns the number of bytes waiting to be sent through this connection.
        /// </summary>
        public int GetSendQueueSize()
        {
            return BaseConnection.GetSendQueueSize();
        }
    }
}
