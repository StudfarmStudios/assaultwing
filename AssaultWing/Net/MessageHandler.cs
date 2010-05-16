using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Net
{
    public interface IMessageHandler
    {
        bool Disposed { get; }
        void Dispose();
        void HandleMessages();
    }

    /// <summary>
    /// Handler of network messages of type <typeparamref name="T"/>.
    /// </summary>
    public class MessageHandler<T> : IMessageHandler where T : Message
    {
        private bool OnlyOneMessage { get; set; }
        private Action<T> Action { get; set; }
        private IConnection Source { get; set; }

        public bool Disposed { get; private set; }

        /// <summary>
        /// Creates a new MessageHander&lt;T&gt;.
        /// </summary>
        /// <param name="onlyOneMessage">Should the handler disactive itself after receiving one message.</param>
        /// <param name="source">The <see cref="IConnection"/> to receive messages from.</param>
        /// <param name="action">What to do for each received message.</param>
        public MessageHandler(bool onlyOneMessage, IConnection source, Action<T> action)
        {
            OnlyOneMessage = onlyOneMessage;
            Source = source;
            Action = action;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public void HandleMessages()
        {
            if (Disposed) throw new InvalidOperationException("Cannot use disposed MessageHandler");
            T message = null;
            while ((message = Source.Messages.TryDequeue<T>()) != null)
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

    /// <summary>
    /// Handler of gameplay related network messages of type <typeparamref name="T"/>.
    /// </summary>
    public class GameplayMessageHandler<T> : IMessageHandler where T : GameplayMessage
    {
        private bool OnlyOneMessage { get; set; }
        private Action<T, TimeSpan> Action { get; set; }
        private PingedConnection Source { get; set; }

        public bool Disposed { get; private set; }

        /// <summary>
        /// Creates a new <see cref="GameplayMessageHandler&lt;T&gt;"/>
        /// </summary>
        /// <param name="onlyOneMessage">Should the handler disactive itself after receiving one message.</param>
        /// <param name="source">The <see cref="IConnection"/> to receive messages from.</param>
        /// <param name="action">What to do for each received message, given the age of the message.</param>
        public GameplayMessageHandler(bool onlyOneMessage, PingedConnection source, Action<T, TimeSpan> action)
        {
            OnlyOneMessage = onlyOneMessage;
            Source = source;
            Action = action;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public void HandleMessages()
        {
            if (Disposed) throw new InvalidOperationException("Cannot use disposed GameplayMessageHandler");
            T message = null;
            while ((message = Source.Messages.TryDequeue<T>()) != null)
            {
                var messageAge = AssaultWing.Instance.DataEngine.ArenaTotalTime
                    - (message.TotalGameTime + Source.RemoteGameTimeOffset);
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
