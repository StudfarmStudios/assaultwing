using AW2.Net.ConnectionUtils;

namespace AW2.Net.Connections
{
    /// <summary>
    /// A minimal interface for connections.
    /// A connection to a remote host over a network. Communication between 
    /// the local and remote host is done by messages. 
    /// </summary>
    /// <remarks>
    /// Connection operates asynchronously. Both creation of connections and 
    /// sending messages via connections are done asynchronously. Therefore 
    /// their result is not known by the time the corresponding method call returns. 
    /// When results of such actions eventually arrive (as either success or 
    /// failure), they are added to corresponding queues.
    /// It is up to the client program to read the results from the queues.
    /// This can be done handily in the client program main loop. If such 
    /// a loop is not available, or for other reasons, the client program 
    /// can hook up events that notify of finished asynchronous operations.
    /// Such queues exist for connection attempts (static), received messages 
    /// (for each connection) and general error conditions (for each connection).
    /// 
    /// This class is thread safe.
    /// </remarks>
    public interface Connection
    {
        public T TryDequeueMessage<T>() where T : Message;

        /// <summary>
        /// Unique identifier of the connection. At least zero and less than <see cref="MAX_CONNECTIONS"/>.
        /// </summary>
        public int ID { get; }
        public void Send(Message message);

        public PingInfo PingInfo { get; init; }

        /// <summary>
        /// A meta-value for <see cref="ID"/> denoting an invalid value.
        /// </summary>
        public const int INVALID_ID = -1;
        protected const int MAX_CONNECTIONS = 32;

        public bool IsDisposed { get; }

        public string Name { get; }
    }
}
