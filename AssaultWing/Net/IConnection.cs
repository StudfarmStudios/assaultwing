using AW2.Helpers.Collections;

namespace AW2.Net
{
    /// <summary>
    /// A connection to a remote host over a network. Communication between 
    /// the local and remote host is done by messages. 
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Unique identifier of the connection. Nonnegative.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// Short, human-readable name of the connection.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Is the connection closed and its resources disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Received messages that are waiting for consumption by the client program.
        /// </summary>
        ITypedQueue<Message> Messages { get; }

        /// <summary>
        /// Updates the connection. Call this regularly.
        /// </summary>
        void Update();

        /// <summary>
        /// Sends a message to the remote host. The message is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        void Send(Message message);

        /// <summary>
        /// Closes the connection and frees resources it has allocated.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Reacts to errors that may have occurred during the connection's
        /// operation in background threads.
        /// </summary>
        void HandleErrors();

        /// <summary>
        /// Returns the number of bytes waiting to be sent through this connection.
        /// </summary>
        int GetSendQueueSize();
    }
}
