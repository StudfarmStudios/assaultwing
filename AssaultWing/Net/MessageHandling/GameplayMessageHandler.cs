using System;

namespace AW2.Net.MessageHandling
{
    /// <summary>
    /// Handler of gameplay related network messages of type <typeparamref name="T"/>.
    /// </summary>
    public class GameplayMessageHandler<T> : IMessageHandler where T : GameplayMessage
    {
        private bool OnlyOneMessage { get; set; }
        private Action<T, TimeSpan> Action { get; set; }
        private SourceType Source { get; set; }

        public override bool Disposed { get; protected set; }

        /// <summary>
        /// Creates a new <see cref="GameplayMessageHandler&lt;T&gt;"/>
        /// </summary>
        /// <param name="onlyOneMessage">Should the handler disactive itself after receiving one message.</param>
        /// <param name="source">The type of source to receive messages from.</param>
        /// <param name="action">What to do for each received message, given the age of the message.</param>
        public GameplayMessageHandler(bool onlyOneMessage, SourceType source, Action<T, TimeSpan> action)
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
            if (Disposed) throw new InvalidOperationException("Cannot use disposed GameplayMessageHandler");
            var connection = GetConnection(Source) as AW2.Net.Connections.PingedConnection;
            if (connection == null) throw new ApplicationException("GameplayMessageHandler needs a PingedConnection");
            T message = null;
            while ((message = connection.Messages.TryDequeue<T>()) != null)
            {
                var messageAge = AssaultWing.Instance.DataEngine.ArenaTotalTime
                    - (message.TotalGameTime + connection.RemoteGameTimeOffset);
                Action(message, messageAge);
                if (OnlyOneMessage)
                {
                    Disposed = true;
                    break;
                }
            }
        }
    }
}
