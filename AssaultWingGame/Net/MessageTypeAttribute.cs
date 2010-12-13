using System;

namespace AW2.Net
{
    /// <summary>
    /// Carries the identifier of a <see cref="Message"/> subclass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MessageTypeAttribute : Attribute
    {
        public bool ToBeRegistered { get; private set; }
        public MessageType Type { get; private set; }

        public MessageTypeAttribute()
        {
        }

        public MessageTypeAttribute(byte topicIdentifier, bool isReply)
        {
            ToBeRegistered = true;
            Type = new MessageType(topicIdentifier, isReply);
        }
    }
}
