using System;
using AW2.Core;
using AW2.Net.Connections;

namespace AW2.Net.MessageHandling
{
    /// <summary>
    /// Handler of gameplay related network messages of type <typeparamref name="T"/>.
    /// </summary>
    public class GameplayMessageHandler<T> : MessageHandlerBase where T : GameplayMessage
    {
        public delegate void GameplayMessageAction(T message, int framesAgo);

        private NetworkEngine _networkEngine;
        private GameplayMessageAction _action;

        public bool OneMessageAtATime { get; set; }

        public GameplayMessageHandler(SourceType source, NetworkEngine networkEngine, GameplayMessageAction action)
            : base(source)
        {
            _networkEngine = networkEngine;
            _action = action;
        }

        protected override void HandleMessagesImpl(Connection connection)
        {
            T message = null;
            while ((message = connection.TryDequeueMessage<T>()) != null)
            {
                var framesAgo = _networkEngine.GetMessageAge(message, connection);
                _action(message, framesAgo);
                if (OneMessageAtATime) break;
            }
        }
    }
}
