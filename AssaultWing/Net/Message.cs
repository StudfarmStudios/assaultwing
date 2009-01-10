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
    /// Message type identifier. Each <c>Message</c> subclass
    /// must specify their identifier as a static instance of
    /// this type.
    /// </summary>
    public class MessageType
    {
        /// <summary>
        /// Topic of the message.
        /// </summary>
        public byte topicIdentifier;

        /// <summary>
        /// Is the message a reply to a previous message.
        /// </summary>
        public bool isReply;

        /// <summary>
        /// Returns the hash code for this object.
        /// </summary>
        /// <returns>This object's hash code.</returns>
        public override int GetHashCode()
        {
            return isReply ? byte.MaxValue + 1 + topicIdentifier : topicIdentifier;
        }

        /// <summary>
        /// Returns a textural, human readable description of this object.
        /// </summary>
        /// <returns>A textural, human readable description of this object.</returns>
        public override string ToString()
        {
            return topicIdentifier.ToString() + (isReply ? "-reply" : "-request");
        }

        /// <summary>
        /// Creates a message type.
        /// </summary>
        /// <param name="topicIdentifier">Topic of the message.</param>
        /// <param name="isReply">Is the message a reply.</param>
        public MessageType(byte topicIdentifier, bool isReply)
        {
            this.topicIdentifier = topicIdentifier;
            this.isReply = isReply;
        }
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
        /// Message's header flags.
        /// </summary>
        public MessageHeaderFlags HeaderFlags { get { return headerFlags; } set { headerFlags = value; } }

        /// <summary>
        /// Identifier of the connection from which this message came,
        /// or negative if the message was created locally and was not
        /// received from a connection.
        /// </summary>
        public int ConnectionId { get; private set; }

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
            byte topicIdentifier = header[(int)MessageHeaderIndex.MessageTopic];
            MessageHeaderFlags flags = (MessageHeaderFlags)header[(int)MessageHeaderIndex.MessageFlags];
            MessageType messageType = new MessageType(topicIdentifier, (flags & MessageHeaderFlags.Reply) != 0);
            if (messageTypes[messageType.GetHashCode()] == null) return false;
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
        /// Returns the type of a message.
        /// </summary>
        /// <param name="header">The header of the message.</param>
        /// <returns>The type of the message.</returns>
        public static MessageType GetMessageType(byte[] header)
        {
            if (!IsValidHeader(header))
                throw new InvalidDataException("Invalid message header");
            byte topicIdentifier = header[(int)MessageHeaderIndex.MessageTopic];
            MessageHeaderFlags flags = (MessageHeaderFlags)header[(int)MessageHeaderIndex.MessageFlags];
            return new MessageType(topicIdentifier, (flags & MessageHeaderFlags.Reply) != 0);
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
            writer.Write((byte)messageType.topicIdentifier);
            if (messageType.isReply) headerFlags |= MessageHeaderFlags.Reply;
            writer.Write((byte)headerFlags); // flags
            writer.Write((byte)versionIdentifier);
            writer.Write((ushort)0); // body length
            writer.Write((ushort)0); // reserved
            long headerDataLength = writer.BaseStream.Length;

            Serialize(writer);
            byte[] data = ((MemoryStream)writer.BaseStream).ToArray();

            // Write body length to header.
            ushort bodyDataLength = checked((ushort)(writer.BaseStream.Length - headerDataLength));
            byte[] lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(unchecked((short)bodyDataLength)));
            data[4] = lengthBytes[0];
            data[5] = lengthBytes[1];

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
        /// <see cref="Connection.Id"/>
        public static Message Deserialize(byte[] header, byte[] body, int connectionId)
        {
            if (!IsValidHeader(header))
                throw new InvalidDataException("Invalid message header");

            MessageType messageType = GetMessageType(header);

            int bodyLength = GetBodyLength(header);
            if (bodyLength > body.Length)
                throw new InvalidDataException("Body length mismatch (" + bodyLength + " expected, " + body.Length + " got)");

            Message message = (Message)messageTypes[messageType.GetHashCode()].GetConstructor(Type.EmptyTypes).Invoke(null);
            message.Deserialize(new NetworkBinaryReader(new MemoryStream(body)));
            message.ConnectionId = connectionId;
            return message;
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
            ConnectionId = -1;
        }
        
        /// <summary>
        /// Scans through <c>Message</c> subclasses and registers their message type identifiers
        /// for future deserialisation.
        /// </summary>
        static Message()
        {
            foreach (Type type in Array.FindAll<Type>(System.Reflection.Assembly.GetExecutingAssembly().GetTypes(),
                delegate(Type t) { return typeof(Message).IsAssignableFrom(t) && t != typeof(Message); }))
            {
                try
                {
                    System.Reflection.BindingFlags flags =
                        System.Reflection.BindingFlags.GetField |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Static;
                    MessageType messageType = (MessageType)type.GetField("messageType", flags).GetValue(null);
                    int hashCode = messageType.GetHashCode();
                    if (messageTypes[hashCode] != null)
                        throw new Exception("Two message types have the same identifier: " + type.Name + " and " + messageTypes[hashCode].Name);
                    messageTypes[hashCode] = type;
                }
                catch (Exception e)
                {
                    throw new Exception("Error registering Message subclasses", e);
                }
            }
        }

        /// <summary>
        /// A mapping of message type identifier hash codes to message types.
        /// </summary>
        static Type[] messageTypes = new Type[256 * 2];

        #endregion Private deserialisation stuff
    }
}
