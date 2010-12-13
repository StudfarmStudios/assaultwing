using System;
using System.Collections.Generic;
using System.Linq;

namespace AW2.Net
{
    /// <summary>
    /// Message type identifier. Each <c>Message</c> subclass
    /// must specify their identifier as a static instance of
    /// this type.
    /// </summary>
    public class MessageType
    {
        /// <summary>
        /// Topic of the message.
        /// </summary>
        public byte TopicIdentifier;

        /// <summary>
        /// Is the message a reply to a previous message.
        /// </summary>
        public bool IsReply;

        /// <summary>
        /// A mapping of message type identifier hash codes to message types.
        /// </summary>
        /// <c>GetMessageTypeIndex</c> produces valid indices for this array.
        /// <seealso cref="GetMessageTypeIndex"/>
        private static Type[] g_messageTypes = new Type[256 * 2];

        /// <summary>
        /// The subclass of Message that this message type identifier represents,
        /// or <c>null</c> if no such subclass exists.
        /// </summary>
        public Type MessageSubclass
        {
            get
            {
                int hashCode = GetHashCode();
                if (hashCode < 0 || hashCode >= g_messageTypes.Length)
                    return null;
                return g_messageTypes[hashCode];
            }
        }

        /// <summary>
        /// Registers a message type identifier to refer to a type.
        /// </summary>
        /// <param name="messageType">The message type identifier</param>
        /// <param name="type">The type</param>
        public static void Register(MessageType messageType, Type type)
        {
            int hashCode = messageType.GetHashCode();
            if (g_messageTypes[hashCode] != null)
                throw new Exception("Two message types have the same identifier: " + type.Name + " and " + g_messageTypes[hashCode].Name);
            g_messageTypes[hashCode] = type;
        }

        /// <summary>
        /// Returns the message type (i.e. a nonabstract subclass of Message)
        /// corresponding to a topic and reply status.
        /// </summary>
        public static Type GetMessageSubclass(byte topicIdentifier, bool isReply)
        {
            return g_messageTypes[GetMessageTypeIndex(topicIdentifier, isReply)];
        }

        /// <summary>
        /// Returns the hash code for this object.
        /// </summary>
        /// <returns>This object's hash code.</returns>
        public override int GetHashCode()
        {
            return GetMessageTypeIndex(TopicIdentifier, IsReply);
        }

        private static int GetMessageTypeIndex(byte topicIdentifier, bool isReply)
        {
            return isReply ? byte.MaxValue + 1 + topicIdentifier : topicIdentifier;
        }

        /// <summary>
        /// Returns a textural, human readable description of this object.
        /// </summary>
        /// <returns>A textural, human readable description of this object.</returns>
        public override string ToString()
        {
            Type messageSubclass = MessageSubclass;
            return (messageSubclass == null ? "unknown" : messageSubclass.Name)
                + (IsReply ? "-reply" : "-request");
        }

        /// <summary>
        /// Creates a message type.
        /// </summary>
        /// <param name="topicIdentifier">Topic of the message.</param>
        /// <param name="isReply">Is the message a reply.</param>
        public MessageType(byte topicIdentifier, bool isReply)
        {
            TopicIdentifier = topicIdentifier;
            IsReply = isReply;
        }
    }
}
