using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers.Collections;

namespace AW2.Net
{
    /// <summary>
    /// Connection to several remote hosts. There is one <see cref="Connection"/> 
    /// for each remote host.
    /// </summary>
    public class MultiConnection : IConnection
    {
        class ConnectionList : List<IConnection>
        {
            TypedQueueCollection<Message> messages;
            public ConnectionList(TypedQueueCollection<Message> messages)
            {
                if (messages == null) throw new ArgumentNullException("Null message queue for MultiConnection.ConnectionList");
                this.messages = messages;
            }

            public new void Add(IConnection item)
            {
                base.Add(item);
                messages.Add(item.Messages);
            }

            public new bool Remove(IConnection item)
            {
                bool success = base.Remove(item);
                if (success) messages.Remove(item.Messages);
                return success;
            }
        }

        TypedQueueCollection<Message> messages = new TypedQueueCollection<Message>();

        /// <summary>
        /// The connections this <see cref="MultiConnection"/> consists of.
        /// </summary>
        public IList<IConnection> Connections { get; private set; }

        /// <summary>
        /// Access to individual connections by their identifier.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public IConnection this[int connectionId] { get { return Connections.First(conn => conn.Id == connectionId); } }

        /// <summary>
        /// Unique identifier of the connection. Nonnegative.
        /// </summary>
        public int Id { get { throw new NotImplementedException("MultiConnection has no identifier"); } }

        /// <summary>
        /// Creates a new <see cref="MultiConnection"/>.
        /// </summary>
        public MultiConnection()
        {
            Connections = new ConnectionList(messages);
        }

        /// <summary>
        /// Closes the connections and frees resources they have allocated.
        /// </summary>
        public void Dispose()
        {
            foreach (var conn in Connections) conn.Dispose();
        }

        /// <summary>
        /// Short, human-readable name of the connection.
        /// </summary>
        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Received messages that are waiting for consumption by the client program.
        /// </summary>
        public ITypedQueue<Message> Messages { get { return messages; } }

        /// <summary>
        /// Sends a message to the remote host. The message is sent asynchronously,
        /// so there is no guarantee when the transmission will be finished.
        /// </summary>
        public void Send(Message message)
        {
            foreach (var conn in Connections) conn.Send(message);
        }

        /// <summary>
        /// Updates the connection. Call this regularly.
        /// </summary>
        public void Update()
        {
            foreach (var conn in Connections) conn.Update();
        }

        /// <summary>
        /// Reacts to errors that may have occurred during the connection's
        /// operation in background threads.
        /// </summary>
        public void HandleErrors()
        {
            foreach (var conn in Connections) conn.HandleErrors();
        }

        /// <summary>
        /// Returns the number of bytes waiting to be sent through this connection.
        /// </summary>
        public int GetSendQueueSize()
        {
            return Connections.Sum(conn => conn.GetSendQueueSize());
        }
    }
}
