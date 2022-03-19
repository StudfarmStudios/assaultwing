using System;
using AW2.Net.Connections;

namespace AW2.Net.MessageHandling
{
    /// <summary>
    /// Handler of network messages of type <typeparamref name="T"/>.
    /// </summary>
    public class MessageHandler<T> : MessageHandlerBase where T : Message
    {
        private Action<T> Action { get; set; }

        public MessageHandler(SourceType source, Action<T> action)
            : base(source)
        {
            Action = action;
        }

        protected override void HandleMessagesImpl(Connection connection)
        {
            T message = null;
            while ((message = connection.TryDequeueMessage<T>()) != null) Action(message);
        }
    }
}
