using System;
using AW2.Net.Messages;

namespace AW2.Net
{
    /// <summary>
    /// A connection that has automatic ping time measuring.
    /// </summary>
    public class PingedConnection
    {
        /// <summary>
        /// Time at which the next ping request should be sent, in real time.
        /// </summary>
        TimeSpan nextPingSend;

        /// <summary>
        /// The underlying connection.
        /// </summary>
        public Connection BaseConnection { get; set; }

        /// <summary>
        /// Round-trip ping time of the underlying connection.
        /// </summary>
        public TimeSpan PingTime { get; private set; }

        /// <summary>
        /// Creates a pinged connection from an established connection.
        /// </summary>
        /// <param name="baseConnection">The underlying connection to
        /// equip with automatic ping time measurement.</param>
        public PingedConnection(Connection baseConnection)
        {
            this.BaseConnection = baseConnection;
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
                nextPingSend = now + TimeSpan.FromSeconds(1);
                var pingSend = new PingRequestMessage();
                pingSend.Timestamp = now;
                BaseConnection.Send(pingSend);
            }

            // Respond to received ping requests.
            var pingReceive = BaseConnection.Messages.TryDequeue<PingRequestMessage>();
            if (pingReceive != null)
            {
                var pongSend = pingReceive.GetPingReplyMessage();
                BaseConnection.Send(pongSend);
            }

            // Respond to received ping replies.
            var pongReceive = BaseConnection.Messages.TryDequeue<PingReplyMessage>();
            if (pongReceive != null)
                PingTime = now - pongReceive.Timestamp;
        }
    }
}
