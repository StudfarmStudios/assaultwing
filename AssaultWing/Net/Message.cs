using System;
using System.IO;
using System.Linq;
using System.Net;
using AW2.Helpers.Serialization;

namespace AW2.Net
{
    /// <summary>
    /// A message to send over a connection.
    /// </summary>
    /// <remarks>Subclasses should use the protected serialisation interface
    /// for their serialisation needs. Serialisation starts by calling one of 
    /// the <c>BeginSerialize</c> methods. Then a number of <c>WriteXxx</c>
    /// methods write the data to the message. Finally, a call to 
    /// <c>EndSerialize</c> finishes the serialisation and returns its result.
    /// Every subclass must define their message type identifier with <see cref="MessageTypeAttribute"/>.
    /// </remarks>
    public abstract class Message
    {
        private enum DataIndex
        {
            ProtocolIdentifier = 0, // byte
            MessageTopic = 1, // byte
            MessageFlags = 2, // byte
            ProtocolVersion = 3, // byte
            MessageBodyLength = 4, // word
            Reserved = 6, // word
            Body = 8,
        }

        /// <summary>
        /// Length of the message header in bytes.
        /// </summary>
        public const int HEADER_LENGTH = (int)DataIndex.Body;

        /// <summary>
        /// Maximum length of the message body in bytes.
        /// </summary>
        public const int BODY_MAXIMUM_LENGTH = ushort.MaxValue;

        /// <summary>
        /// Maximum length of a message in bytes.
        /// </summary>
        public const int MAXIMUM_LENGTH = HEADER_LENGTH + BODY_MAXIMUM_LENGTH;

        private const byte PROTOCOL_IDENTIFIER = (byte)'A';
        private const byte VERSION_IDENTIFIER = 0x00;

        private MessageHeaderFlags _headerFlags;

        /// <summary>
        /// The type identifier of the message.
        /// </summary>
        public MessageType Type { get { return GetMessageType(GetType()); } }

        /// <summary>
        /// How to send this message over a network.
        /// </summary>
        public virtual MessageSendType SendType { get { return MessageSendType.TCP; } }

        /// <summary>
        /// Message's header flags.
        /// </summary>
        public MessageHeaderFlags HeaderFlags { get { return _headerFlags; } set { _headerFlags = value; } }

        /// <summary>
        /// Identifier of the connection from which this message came,
        /// or negative if the message was created locally and was not
        /// received from a connection.
        /// </summary>
        public int ConnectionID { get; set; }

        public TimeSpan CreationTime { get; protected set; }

        #region Public interface

        /// <summary>
        /// Is a message header valid.
        /// </summary>
        /// <param name="header">Buffer containing the header (and maybe something else).</param>
        /// <returns><c>true</c> if the header is valid, or <c>false</c> otherwise.</returns>
        public static bool IsValidHeader(ArraySegment<byte> header)
        {
            // Message header structure:
            // byte protocol_identifier
            // byte message_type
            // byte message_flags
            // byte protocol_version
            // word message_body_length
            // word reserved
            if (header.Array == null) throw new ArgumentNullException("header.Array");
            if (header.Count < HEADER_LENGTH) return false;
            byte protocolIdentifier = header.Array[header.Offset + (int)DataIndex.ProtocolIdentifier];
            if (protocolIdentifier != Message.PROTOCOL_IDENTIFIER) return false;
            if (GetMessageSubclass(header) == null) return false;
            byte versionIdentifier = header.Array[header.Offset + (int)DataIndex.ProtocolVersion];
            if (versionIdentifier != Message.VERSION_IDENTIFIER) return false;
            // These can be anything:
            // word message_body_length
            // word reserved
            return true;
        }

        /// <summary>
        /// Returns the length of message body in bytes.
        /// </summary>
        /// <param name="header">Buffer containing the header of the message.</param>
        public static int GetBodyLength(ArraySegment<byte> header)
        {
            return unchecked((ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(header.Array, header.Offset + 4)));
        }

        /// <summary>
        /// Returns the message subclass referred to in a deserialised message header.
        /// </summary>
        /// <param name="header">Buffer containing the header of the message.</param>
        public static Type GetMessageSubclass(ArraySegment<byte> header)
        {
            byte topicIdentifier = header.Array[header.Offset + (int)DataIndex.MessageTopic];
            var flags = (MessageHeaderFlags)header.Array[header.Offset + (int)DataIndex.MessageFlags];
            return MessageType.GetMessageSubclass(topicIdentifier, (flags & MessageHeaderFlags.Reply) != 0);
        }

        public virtual void Serialize(NetworkBinaryWriter writer)
        {
                writer.Seek(HEADER_LENGTH, SeekOrigin.Begin);
                SerializeBody(writer);
                ushort bodyLength = checked((ushort)(writer.GetBaseStream().Position - HEADER_LENGTH));
                writer.Seek(0, SeekOrigin.Begin);
                SerializeHeader(writer, bodyLength);
                writer.Seek(HEADER_LENGTH + bodyLength, SeekOrigin.Begin);
        }

        /// <summary>
        /// The caller must set Message.ConnectionID by hand.
        /// </summary>
        /// <param name="headerAndBody">Buffer containing the header and the body of the message.</param>
        /// <param name="creationTime">Creation timestamp of the message</param>
        public static Message Deserialize(ArraySegment<byte> headerAndBody, TimeSpan creationTime)
        {
            if (headerAndBody.Array == null) throw new ArgumentNullException("headerAndBody.Array");
            if (!IsValidHeader(headerAndBody)) throw new MessageException("Invalid message header");
            int expectedLength = HEADER_LENGTH + GetBodyLength(headerAndBody);
            if (expectedLength > headerAndBody.Count) throw new ArgumentException("Message length mismatch (" + expectedLength + " expected, " + headerAndBody.Count + " got)");
            var message = (Message)GetMessageSubclass(headerAndBody).GetConstructor(System.Type.EmptyTypes).Invoke(null);
            message.Deserialize(new NetworkBinaryReader(new MemoryStream(headerAndBody.Array, HEADER_LENGTH, headerAndBody.Count - HEADER_LENGTH, false)));
            message.ConnectionID = AW2.Net.Connections.Connection.INVALID_ID;
            message.CreationTime = creationTime;
            return message;
        }

        public override string ToString()
        {
            return Type.ToString();
        }

        #endregion Public interface

        #region Abstract methods

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// Implementors should call the writer's write methods to write the
        /// serialised form piece by piece.
        /// <param name="writer">Writer of serialised data.</param>
        protected abstract void SerializeBody(NetworkBinaryWriter writer);

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected abstract void Deserialize(NetworkBinaryReader reader);

        #endregion Abstract methods

        #region Private deserialisation stuff

        /// <summary>
        /// Creates an uninitialised message.
        /// </summary>
        protected Message()
        {
            ConnectionID = -1;
        }

        /// <summary>
        /// Scans through <see cref="Message"/> subclasses and registers their message type identifiers
        /// for future deserialisation.
        /// </summary>
        static Message()
        {
            foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!typeof(Message).IsAssignableFrom(type)) continue;
                if (type.IsAbstract) continue;
                try
                {
                    var messageType = GetMessageType(type);
                    if (messageType != null) MessageType.Register(messageType, type);
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Error registering Message subclasses", e);
                }
            }
        }

        /// <summary>
        /// Returns the message type identifier of a message type, or null if the message type
        /// is not to be registered.
        /// </summary>
        /// <param name="type">A message type, i.e. a nonabstract subclass of Message.</param>
        private static MessageType GetMessageType(Type type)
        {
            if (!typeof(Message).IsAssignableFrom(type) || type.IsAbstract)
                throw new ArgumentException("Only nonabstract subclasses of Message have a message type");
            var messageTypeAttribute = (MessageTypeAttribute)type.GetCustomAttributes(typeof(MessageTypeAttribute), false).First();
            if (!messageTypeAttribute.ToBeRegistered) return null;
            return messageTypeAttribute.Type;
        }

        private void SerializeHeader(NetworkBinaryWriter writer, ushort bodyLength)
        {
            // Message header structure:
            // byte protocol_identifier
            // byte message_type
            // byte message_flags
            // byte protocol_version
            // word message_body_length
            // word reserved
            writer.Write((byte)PROTOCOL_IDENTIFIER);
            var messageType = GetMessageType(GetType());
            writer.Write((byte)messageType.TopicIdentifier);
            if (messageType.IsReply) _headerFlags |= MessageHeaderFlags.Reply;
            writer.Write((byte)_headerFlags); // flags
            writer.Write((byte)VERSION_IDENTIFIER);
            writer.Write((ushort)bodyLength);
            writer.Write((ushort)0);
        }

        #endregion Private deserialisation stuff
    }
}
