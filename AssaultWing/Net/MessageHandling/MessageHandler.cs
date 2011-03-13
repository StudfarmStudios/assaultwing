using System;

namespace AW2.Net.MessageHandling
{
    /// <summary>
    /// Handler of network messages of type <typeparamref name="T"/>.
    /// </summary>
    public class MessageHandler<T> : IMessageHandler where T : Message
    {
        private Action<T> Action { get; set; }
        private SourceType Source { get; set; }

        public override bool Disposed { get; protected set; }

        public MessageHandler(SourceType source, Action<T> action)
        {
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
                while ((message = connection.TryDequeueMessage<T>()) != null)
                {
                    Action(message);
                }
            }
        }
    }
}
