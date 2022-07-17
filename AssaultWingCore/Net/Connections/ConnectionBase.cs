using AW2.Net.ConnectionUtils;
using AW2.Core;
using AW2.Helpers;
using AW2.Helpers.Collections;

namespace AW2.Net.Connections
{
    /// <summary>
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
    public abstract class ConnectionBase : Connection, IDisposable
    {
        private static readonly TimeSpan SIMULATED_NETWORK_LAG = TimeSpan.FromSeconds(0.0);

        /// <summary>
        /// If greater than zero, then the connection is disposed and thus no longer usable.
        /// </summary>
        private int _isDisposed;

        public bool IsDisposed { get { return _isDisposed > 0; } }

        /// <summary>
        /// Closes the connection and frees resources it has allocated.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error has occurred.</param>
        protected void Dispose(bool error)
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) > 0) return;
            DisposeImpl(error);
        }

        /// <summary>
        /// Closes the connection and frees resources it has allocated.
        /// </summary>
        public void Dispose()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs the actual disposing.
        /// </summary>
        /// <param name="error">If <c>true</c> then an internal error has occurred.</param>
        protected abstract void DisposeImpl(bool error);

        /// <summary>
        /// Least int that is known not to have been used as a connection identifier.
        /// </summary>
        private static Queue<int> g_unusedIDs = new Queue<int>(Enumerable.Range(0, Connection.MAX_CONNECTIONS));

        protected AssaultWingCore Game { get; init; }

        /// <summary>
        /// Unique identifier of the connection. At least zero and less than <see cref="MAX_CONNECTIONS"/>.
        /// </summary>
        public int ID { get; init; }

        /// <summary>
        /// Short, human-readable name of the connection.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Received messages that are waiting for consumption by the client program.
        /// </summary>
        public ThreadSafeWrapper<ITypedQueue<Message>> Messages { get; }
        
        public PingInfo PingInfo { get; init; }

        abstract public void Send(Message message);

        public T TryDequeueMessage<T>() where T : Message
        {
            T value = default(T);
            Messages.Do(queue => value = queue.TryDequeue<T>(m => m.CreationTime <= Game.GameTime.TotalRealTime - SIMULATED_NETWORK_LAG));
            return value;
        }

        /// <summary>
        /// Messages received from the network are queued for handling using this method.
        /// </summary>
        public void HandleMessage(Message message)
        {
            Messages.Do(queue => queue.Enqueue(message));
        }

        /// <summary>
        /// Implementation must call this when disposing a connection.
        /// </summary>
        protected void DisposeId() {
            Log.Write("Disposing " + Name);
            g_unusedIDs.Enqueue(ID);
        }

        /// <summary>
        /// Creates a new connection to a remote host.
        /// </summary>
        protected ConnectionBase(AssaultWingCore game)
        {
            Game = game;
            ID = g_unusedIDs.Dequeue();
            Name = "Connection " + ID;
            Messages = new ThreadSafeWrapper<ITypedQueue<Message>>(new TypedQueue<Message>());
            PingInfo = new PingInfo(this);
        }

        /// <summary>
        /// Add a detected error to this connection. This will cause the closing
        /// of the connection. The error will also be logged with appropriate meta dat
        /// </summary>
        abstract public void QueueError(string message);

    }
}