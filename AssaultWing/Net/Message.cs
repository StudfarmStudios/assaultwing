using System;
using System.IO;
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
    /// Every subclass must define their message type identifier as
    /// <c>static MessageType messageType</c>.
    /// </remarks>
    public abstract class Message
    {
        private enum MessageHeaderIndex
        {
            ProtocolIdentifier = 0, // byte
            MessageTopic = 1, // byte
            MessageFlags = 2, // byte
            ProtocolVersion = 3, // byte
            MessageBodyLength = 4, // word
            Reserved = 6, // word
        }

        /// <summary>
        /// Length of the message header in bytes.
        /// </summary>
        public const int HEADER_LENGTH = 8;

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
        public int ConnectionID { get; private set; }

        #region Public interface

        /// <summary>
        /// Is a message header valid.
        /// </summary>
        /// <param name="header">Buffer containing the header (and maybe something else).</param>
        /// <returns><c>true</c> if the header is valid, or <c>false</c> otherwise.</returns>
        public static bool IsValidHeader(byte[] header)
        {
            // Message header structure:
            // byte protocol_identifier
            // byte message_type
            // byte message_flags
            // byte protocol_version
            // word message_body_length
            // word reserved
            if (header == null) throw new ArgumentNullException("Null header");
            if (header.Length < HEADER_LENGTH) return false;
            byte protocolIdentifier = header[(int)MessageHeaderIndex.ProtocolIdentifier];
            if (protocolIdentifier != Message.PROTOCOL_IDENTIFIER) return false;
            if (GetMessageSubclass(header) == null) return false;
            byte versionIdentifier = header[(int)MessageHeaderIndex.ProtocolVersion];
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
        public static int GetBodyLength(byte[] header)
        {
            return unchecked((ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(header, 4)));
        }

        /// <summary>
        /// Returns the message subclass referred to in a deserialised message header.
        /// </summary>
        /// <param name="header">Buffer containing the header of the message.</param>
        public static Type GetMessageSubclass(byte[] header)
        {
            byte topicIdentifier = header[(int)MessageHeaderIndex.MessageTopic];
            var flags = (MessageHeaderFlags)header[(int)MessageHeaderIndex.MessageFlags];
            return MessageType.GetMessageSubclass(topicIdentifier, (flags & MessageHeaderFlags.Reply) != 0);
        }

        /// <summary>
        /// Serialises the message's content into a sequence of bytes.
        /// </summary>
        /// <returns>The serialised message.</returns>
        public virtual byte[] Serialize()
        {
            var writer = new NetworkBinaryWriter(new MemoryStream());

            // Message header structure:
            // byte protocol_identifier
            // byte message_type
            // byte message_flags
            // byte protocol_version
            // word message_body_length
            // word reserved
            writer.Write((byte)PROTOCOL_IDENTIFIER);
            System.Reflection.BindingFlags bindingFlags = 
                System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static;
            var messageType = (MessageType)GetType().GetField("messageType", bindingFlags).GetValue(null);
            writer.Write((byte)messageType.TopicIdentifier);
            if (messageType.IsReply) _headerFlags |= MessageHeaderFlags.Reply;
            writer.Write((byte)_headerFlags); // flags
            writer.Write((byte)VERSION_IDENTIFIER);
            writer.Write((ushort)0); // body length
            writer.Write((ushort)0); // reserved
            long headerDataLength = writer.BaseStream.Length;

            Serialize(writer);
            var data = ((MemoryStream)writer.BaseStream).ToArray();

            // Write body length to header.
            ushort bodyDataLength = checked((ushort)(writer.BaseStream.Length - headerDataLength));
            short lengthBits = IPAddress.HostToNetworkOrder(unchecked((short)bodyDataLength));
            if (BitConverter.IsLittleEndian)
            {
                data[4] = (byte)(lengthBits & 0x00ff);
                data[5] = (byte)((lengthBits >> 8) & 0x00ff);
            }
            else
            {
                data[4] = (byte)((lengthBits >> 8) & 0x00ff);
                data[5] = (byte)(lengthBits & 0x00ff);
            }

            writer.Close();
            return data;
        }

        /// <param name="headerAndBody">Buffer containing the header and the body of the message.</param>
        /// <param name="connectionId">Identifier of the connection where the message was received.</param>
        public static Message Deserialize(byte[] headerAndBody, int connectionId)
        {
            if (headerAndBody == null) throw new ArgumentNullException("headerAndBody", "Null message content");
            if (!IsValidHeader(headerAndBody)) throw new ArgumentException("Invalid message header", "headerAndBody");
            int bodyLength = GetBodyLength(headerAndBody);
            int expectedLength = HEADER_LENGTH + bodyLength;
            if (expectedLength > headerAndBody.Length) throw new ArgumentException("Message length mismatch (" + expectedLength + " expected, " + headerAndBody.Length + " got)");
            var message = (Message)GetMessageSubclass(headerAndBody).GetConstructor(System.Type.EmptyTypes).Invoke(null);
            message.Deserialize(new NetworkBinaryReader(new MemoryStream(headerAndBody, HEADER_LENGTH, BODY_MAXIMUM_LENGTH)));
            message.ConnectionID = connectionId;
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
        protected abstract void Serialize(NetworkBinaryWriter writer);

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
            var flags =
                System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static;
            var field = type.GetField("messageType", flags);
            if (field == null) return null;
            return (MessageType)field.GetValue(null);
        }

        #endregion Private deserialisation stuff
    }
}
