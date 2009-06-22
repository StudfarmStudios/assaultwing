using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AW2.Net
{
    /// <summary>
    /// Handler of a certain kind of network message.
    /// </summary>
    public delegate void MessageHandler(Message message);

    /// <summary>
    /// Collection of handlers of network messages.
    /// </summary>
    public class MessageHandlerCollection : IEnumerable<KeyValuePair<Type, MessageHandler>>
    {
        Dictionary<Type, MessageHandler> handlers = new Dictionary<Type, MessageHandler>();

        /// <summary>
        /// Registers a message handler, or clears a previous handler 
        /// if <paramref name="handler"/> is <c>null</c>.
        /// </summary>
        public void SetMessageHandler(Type messageType, MessageHandler handler)
        {
            if (handler == null)
                handlers[messageType] = null;
            else
                handlers[messageType] = handler;
        }

        #region IEnumerable<KeyValuePair<Type, MessageHandler>> Members

        /// <summary>
        /// Returns an enumerator of registered message handlers.
        /// </summary>
        public IEnumerator<KeyValuePair<Type, MessageHandler>> GetEnumerator()
        {
            return handlers.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator of registered message handlers.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
