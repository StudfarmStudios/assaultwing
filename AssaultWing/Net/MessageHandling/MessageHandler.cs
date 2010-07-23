using System;

namespace AW2.Net.MessageHandling
{
    /// <summary>
    /// Handler of network messages of type <typeparamref name="T"/>.
    /// </summary>
    public class MessageHandler<T> : IMessageHandler where T : Message
    {
        private bool OnlyOneMessage { get; set; }
        private Action<T> Action { get; set; }
        private SourceType Source { get; set; }

        public override bool Disposed { get; protected set; }

        /// <summary>
        /// Creates a new MessageHander&lt;T&gt;.
        /// </summary>
        /// <param name="onlyOneMessage">Should the handler disactive itself after receiving one message.</param>
        /// <param name="source">The type of source to receive messages from.</param>
        /// <param name="action">What to do for each received message.</param>
        public MessageHandler(bool onlyOneMessage, SourceType source, Action<T> action)
        {
            OnlyOneMessage = onlyOneMessage;
            Source = source;
            Action = action;
        }

        public override void Dispose()
        {
            Disposed = true;
        }

        public override void HandleMessages()
        {
            if (Disposed) throw new InvalidOperationException("Cannot use disposed MessageHandler");
            T message = null;
            foreach (var connection in GetConnections(Source))
            {
                while ((message = connection.Messages.TryDequeue<T>()) != null)
                {
                    Action(message);
                    if (OnlyOneMessage)
                    {
                        Disposed = true;
                        break;
                    }
                }
            }
        }
    }
}
