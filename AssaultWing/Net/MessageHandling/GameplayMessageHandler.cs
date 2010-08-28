using System;

namespace AW2.Net.MessageHandling
{
    /// <summary>
    /// Handler of gameplay related network messages of type <typeparamref name="T"/>.
    /// </summary>
    public class GameplayMessageHandler<T> : IMessageHandler where T : GameplayMessage
    {
        public delegate void GameplayMessageAction(T message, int framesAgo);

        private bool OnlyOneMessage { get; set; }
        private GameplayMessageAction Action { get; set; }
        private SourceType Source { get; set; }

        public override bool Disposed { get; protected set; }

        /// <summary>
        /// Creates a new <see cref="GameplayMessageHandler&lt;T&gt;"/>
        /// </summary>
        /// <param name="onlyOneMessage">Should the handler disactive itself after receiving one message.</param>
        /// <param name="source">The type of source to receive messages from.</param>
        /// <param name="action">What to do for each received message, given the age of the message.</param>
        public GameplayMessageHandler(bool onlyOneMessage, SourceType source, GameplayMessageAction action)
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
            var connections = GetConnections(Source);
            T message = null;
            foreach (var connection in connections)
            {
                while ((message = connection.Messages.TryDequeue<T>()) != null)
                {
                    var framesAgo = AssaultWing.Instance.NetworkEngine.GetMessageAge(message, connection);
                    Action(message, framesAgo);
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
