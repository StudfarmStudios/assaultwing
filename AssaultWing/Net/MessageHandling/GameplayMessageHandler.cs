using System;
using AW2.Core;

namespace AW2.Net.MessageHandling
{
    /// <summary>
    /// Handler of gameplay related network messages of type <typeparamref name="T"/>.
    /// </summary>
    public class GameplayMessageHandler<T> : IMessageHandler where T : GameplayMessage
    {
        public delegate void GameplayMessageAction(T message, int framesAgo);

        private GameplayMessageAction Action { get; set; }
        private SourceType Source { get; set; }

        public override bool Disposed { get; protected set; }

        public GameplayMessageHandler(SourceType source, GameplayMessageAction action)
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
            if (Disposed) throw new InvalidOperationException("Cannot use disposed GameplayMessageHandler");
            var connections = GetConnections(Source);
            T message = null;
            foreach (var connection in connections)
            {
                while ((message = connection.TryDequeueMessage<T>()) != null)
                {
                    var framesAgo = AssaultWing.Instance.NetworkEngine.GetMessageAge(message, connection);
                    Action(message, framesAgo);
                }
            }
        }
    }
}
