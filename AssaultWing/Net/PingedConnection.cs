using System;
using AW2.Helpers.Collections;
using AW2.Net.Messages;

namespace AW2.Net
{
    /// <summary>
    /// A connection that has automatic ping time measuring.
    /// </summary>
    public class PingedConnection : IConnection
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
