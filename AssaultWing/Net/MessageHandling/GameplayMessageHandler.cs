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
                try
                {
                    var framesAgo = _networkEngine.GetMessageAge(message, connection);
                    _action(message, framesAgo);
                    if (OneMessageAtATime) break;
                }
                catch (System.IO.EndOfStreamException e)
                {
                    var errorText = "Error while handling message " + message + " with read buffer " + message.ReadBufferToString();
                    // Note: Print outer exception details to log because the outer exception will be peeled off
                    // by Forms. Only the innermost exception is handed to the Application.ThreadException event.
                    AW2.Helpers.Log.Write("Details about EndOfStreamException: " + errorText);
                    throw new NetworkException(errorText, e);
                }
            }
        }
    }
}
