using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

namespace AW2.Net
{
    /// <summary>
    /// Exception denoting an error related to messages.
    /// </summary>
    /// <seealso cref="Message"/>
    public class MessageException : Exception
    {
        /// <summary>
        /// Creates a new message exception.
        /// </summary>
        /// <param name="explanation">Explanation of the occurred error.</param>
        public MessageException(string explanation)
            : base(explanation)
        {
        }
    }

    /// <summary>
    /// Flags for message headers.
    /// </summary>
    public enum MessageHeaderFlags : byte
    {
        /// <summary>
        /// The message is a reply to a previous message.
        /// </summary>
        Reply = 0x80,

        /// <summary>
        /// The message is to be sent to several recipients.
        /// </summary>
        Multicast = 0x40,
    }

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
        MessageHeaderFlags headerFlags;

        /// <summary>
        /// The type identifier of the message.
        /// </summary>
        public MessageType Type { get { return GetMessageType(this.GetType()); } }

        /// <summary>
        /// Message's header flags.
        /// </summary>
        public MessageHeaderFlags HeaderFlags { get { return headerFlags; } set { headerFlags = value; } }

        /// <summary>
        /// Identifier of the connection from which this message came,
        /// or negative if the message was created locally and was not
        /// received from a connection.
        /// </summary>
        public int ConnectionID { get; private set; }

        static byte protocolIdentifier = (byte)'A';
        static byte versionIdentifier = 0x00;
        static char[] nullCharArray = new char[] { '\0' };

        enum MessageHeaderIndex
        {
            ProtocolIdentifier = 0, // byte
            MessageTopic = 1, // byte
            MessageFlags = 2, // byte
            ProtocolVersion = 3, // byte
            MessageBodyLength = 4, // word
            Reserved = 6, // word
        }

        #region Public interface

        /// <summary>
        /// Length of the message header in bytes.
        /// </summary>
        public static int HeaderLength { get { return 8; } }

        /// <summary>
        /// Is a message header valid.
        /// </summary>
        /// <param name="header">The header.</param>
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
            if (header == null)
                throw new ArgumentNullException("Null header");
            if (header.Length != HeaderLength) return false;
            byte protocolIdentifier = header[(int)MessageHeaderIndex.ProtocolIdentifier];
            if (protocolIdentifier != Message.protocolIdentifier) return false;
            if (GetMessageSubclass(header) == null) return false;
            byte versionIdentifier = header[(int)MessageHeaderIndex.ProtocolVersion];
            if (versionIdentifier != Message.versionIdentifier) return false;
            // These can be anything:
            // word message_body_length
            // word reserved
            return true;
        }

        /// <summary>
        /// Returns the length of message body in bytes.
        /// </summary>
        /// <param name="header">The header of the message.</param>
        /// <returns>The length of message body in bytes.</returns>
        public static int GetBodyLength(byte[] header)
        {
            return unchecked((ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(header, 4)));
        }

        /// <summary>
        /// Returns the message subclass referred to in a deserialised message header.
        /// </summary>
        public static Type GetMessageSubclass(byte[] header)
        {
            byte topicIdentifier = header[(int)MessageHeaderIndex.MessageTopic];
            MessageHeaderFlags flags = (MessageHeaderFlags)header[(int)MessageHeaderIndex.MessageFlags];
            return MessageType.GetMessageSubclass(topicIdentifier, (flags & MessageHeaderFlags.Reply) != 0);
        }

        /// <summary>
        /// Serialises the message's content into a sequence of bytes.
        /// </summary>
        /// <returns>The serialised message.</returns>
        public byte[] Serialize()
        {
            NetworkBinaryWriter writer = new NetworkBinaryWriter(new MemoryStream());

            // Message header structure:
            // byte protocol_identifier
            // byte message_type
            // byte message_flags
            // byte protocol_version
            // word message_body_length
            // word reserved
            writer.Write((byte)protocolIdentifier);
            System.Reflection.BindingFlags bindingFlags = 
                System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static;
            MessageType messageType = (MessageType)GetType().GetField("messageType", bindingFlags).GetValue(null);
            writer.Write((byte)messageType.TopicIdentifier);
            if (messageType.IsReply) headerFlags |= MessageHeaderFlags.Reply;
            writer.Write((byte)headerFlags); // flags
            writer.Write((byte)versionIdentifier);
            writer.Write((ushort)0); // body length
            writer.Write((ushort)0); // reserved
            long headerDataLength = writer.BaseStream.Length;

            Serialize(writer);
            byte[] data = ((MemoryStream)writer.BaseStream).ToArray();

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

        /// <summary>
        /// Deserialises a message from raw bytes.
        /// </summary>
        /// <param name="header">Message header.</param>
        /// <param name="body">Buffer containing the message body, 
        /// possibly trailed with extra bytes.</param>
        /// <param name="connectionId">Identifier of the connection 
        /// from which the serialised data came.</param>
        /// <see cref="Connection.ID"/>
        public static Message Deserialize(byte[] header, byte[] body, int connectionId)
        {
            if (header == null || body == null)
                throw new NullReferenceException("Null message header or body");
            if (!IsValidHeader(header))
                throw new InvalidDataException("Invalid message header");

            int bodyLength = GetBodyLength(header);
            if (bodyLength > body.Length)
                throw new InvalidDataException("Body length mismatch (" + bodyLength + " expected, " + body.Length + " got)");

            Message message = (Message)GetMessageSubclass(header).GetConstructor(System.Type.EmptyTypes).Invoke(null);
            message.Deserialize(new NetworkBinaryReader(new MemoryStream(body)));
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
        /// Scans through <c>Message</c> subclasses and registers their message type identifiers
        /// for future deserialisation.
        /// </summary>
        static Message()
        {
            foreach (Type type in Array.FindAll<Type>(System.Reflection.Assembly.GetExecutingAssembly().GetTypes(),
                delegate(Type t) { return typeof(Message).IsAssignableFrom(t) && !t.IsAbstract; }))
            {
                try
                {
                    MessageType.Register(GetMessageType(type), type);
                }
                catch (Exception e)
                {
                    throw new Exception("Error registering Message subclasses", e);
                }
            }
        }

        /// <summary>
        /// Returns the message type identifier of a message type.
        /// </summary>
        /// <param name="type">A message type, i.e. a nonabstract subclass of Message.</param>
        /// <returns>The message type identifier of the message type.</returns>
        private static MessageType GetMessageType(Type type)
        {
            if (!typeof(Message).IsAssignableFrom(type) || type.IsAbstract)
                throw new ArgumentException("Only nonabstract subclasses of Message have a message type");
            System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static;
            return (MessageType)type.GetField("messageType", flags).GetValue(null);
        }

        #endregion Private deserialisation stuff
    }
}
