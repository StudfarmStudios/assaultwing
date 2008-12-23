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

        /// <summary>
        /// Buffer where to write serialised data.
        /// </summary>
        MemoryStream writeBuffer;

        static byte protocolIdentifier = (byte)'A';
        static byte versionIdentifier = 0x00;

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
            writeBuffer = new MemoryStream();

            // Message header structure:
            // byte protocol_identifier
            // byte message_type
            // byte message_flags
            // byte protocol_version
            // word message_body_length
            // word reserved
            WriteByte(protocolIdentifier);
            System.Reflection.BindingFlags bindingFlags = 
                System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static;
            MessageType messageType = (MessageType)GetType().GetField("messageType", bindingFlags).GetValue(null);
            WriteByte(messageType.topicIdentifier);
            if (messageType.isReply) headerFlags |= MessageHeaderFlags.Reply;
            WriteByte((byte)headerFlags); // flags
            WriteByte(versionIdentifier);
            WriteUShort(0); // body length
            WriteUShort(0); // reserved
            long headerDataLength = writeBuffer.Length;

            SerializeBody();
            byte[] data = new byte[writeBuffer.Length];
            Array.Copy(writeBuffer.GetBuffer(), 0, data, 0, writeBuffer.Length);

            // Write body length to header.
            ushort bodyDataLength = checked((ushort)(writeBuffer.Length - headerDataLength));
            byte[] lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(unchecked((short)bodyDataLength)));
            data[4] = lengthBytes[0];
            data[5] = lengthBytes[1];

            writeBuffer.Dispose();
            writeBuffer = null;
            return data;
        }

        /// <summary>
        /// Deserialises a message from raw bytes.
        /// </summary>
        /// <param name="header">Message header.</param>
        /// <param name="body">Message body.</param>
        /// <param name="connectionId">Identifier of the connection 
        /// from which the serialised data came.</param>
        /// <see cref="Connection.Id"/>
        public static Message Deserialize(byte[] header, byte[] body, int connectionId)
        {
            if (!IsValidHeader(header))
                throw new InvalidDataException("Invalid message header");

            MessageType messageType = GetMessageType(header);

            int bodyLength = GetBodyLength(header);
            if (bodyLength != body.Length)
                throw new InvalidDataException("Body length mismatch (" + bodyLength + " expected, " + body.Length + " got)");

            Message message = (Message)messageTypes[messageType.GetHashCode()].GetConstructor(Type.EmptyTypes).Invoke(null);
            message.Deserialize(body);
            message.ConnectionId = connectionId;
            return message;
        }

        #endregion Public interface

        #region Abstract methods

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// Note to subclasses: Use the <c>WriteXxx</c> methods.
        protected abstract void SerializeBody();

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// Note to subclasses: Use the <c>ReadXxx</c> methods.
        protected abstract void Deserialize(byte[] body);

        #endregion Abstract methods

        #region Serialisation interface for subclasses

        /// <summary>
        /// Writes a byte to the stream of serialised data.
        /// </summary>
        /// <param name="value">The value.</param>
        protected void WriteByte(byte value)
        {
            writeBuffer.WriteByte(value);
        }

        /// <summary>
        /// Writes a byte representing a bool to the stream of serialised data.
        /// </summary>
        /// <param name="value">The value.</param>
        protected void WriteBool(bool value)
        {
            WriteByte(value ? (byte)0x7f : (byte)0);
        }

        /// <summary>
        /// Writes an unsigned short to the stream of serialised data.
        /// </summary>
        /// <param name="value">The value.</param>
        protected void WriteUShort(ushort value)
        {
            writeBuffer.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(unchecked((short)value))), 0, 2);
        }

        /// <summary>
        /// Writes an int to the stream of serialised data.
        /// </summary>
        /// <param name="value">The value.</param>
        protected void WriteInt(int value)
        {
            writeBuffer.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)), 0, 4);
        }

        /// <summary>
        /// Writes a float to the stream of serialised data.
        /// </summary>
        /// <param name="value">The value.</param>
        protected void WriteFloat(float value)
        {
            WriteInt(BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        }

        /// <summary>
        /// Writes to the serialised representation of the message a given number of 
        /// bytes containing a string and a trailing sequence of one or more zero bytes.
        /// The string is truncated to fit the byte count, and an optional exception is 
        /// thrown if this happens. The string will be written in UTF-8 encoding. 
        /// </summary>
        /// <param name="value">The string to write.</param>
        /// <param name="byteCount">The exact number of bytes to write, including the
        /// trailing zero.</param>
        /// <param name="throwOnTruncate">If <c>true</c> then an exception will be
        /// thrown if the string is too long to fit the given number of bytes.</param>
        protected void WriteString(string value, int byteCount, bool throwOnTruncate)
        {
            if (byteCount < 1)
                throw new ArgumentException("Need at least one byte to write a string with a trailing zero");
            Encoding encoding = Encoding.UTF8;
            int bytesNeeded = encoding.GetByteCount(value);
            if (bytesNeeded + 1 > byteCount)
            {
                if (throwOnTruncate)
                    throw new ArgumentException("String too long (" + (bytesNeeded + 1) + ") to fit given byte count (" + byteCount + ")");

                // Binary search for the maximum number of chars that fit.
                char[] valueChars = value.ToCharArray();
                int goodCharCount = 0, badCharCount = valueChars.Length;
                bytesNeeded = 0;
                while (badCharCount - goodCharCount > 1)
                {
                    int charCount = (goodCharCount + badCharCount) / 2;
                    int bytesNeededNow = encoding.GetByteCount(valueChars, 0, charCount);
                    if (bytesNeededNow + 1 > byteCount)
                        badCharCount = charCount;
                    else
                    {
                        goodCharCount = charCount;
                        bytesNeeded = bytesNeededNow;
                    }
                }
                writeBuffer.Write(encoding.GetBytes(valueChars, 0, goodCharCount), 0, bytesNeeded);
            }
            else 
                writeBuffer.Write(encoding.GetBytes(value), 0, bytesNeeded);

            // Pad with zero bytes.
            for (int i = bytesNeeded; i < byteCount; ++i)
                writeBuffer.WriteByte(0);
        }

        /// <summary>
        /// Writes to the serialised representation of the message a string with a 
        /// trailing zero byte. The string will be written in UTF-8 encoding. 
        /// </summary>
        /// <param name="value">The string to write.</param>
        protected void WriteString(string value)
        {
            byte[] encodedValue = Encoding.UTF8.GetBytes(value);
            writeBuffer.Write(encodedValue, 0, encodedValue.Length);
            writeBuffer.WriteByte(0);
        }

        /// <summary>
        /// Writes to the serialised representation of the message a series of bytes.
        /// </summary>
        /// <param name="value">The bytes to write.</param>
        protected void WriteBytes(byte[] value)
        {
            writeBuffer.Write(value, 0, value.Length);
        }

        #endregion Serialisation interface for subclasses

        #region Deserialisation interface for subclasses

        /// <summary>
        /// Reads a byte representing a bool from serialised data.
        /// </summary>
        /// <param name="buffer">The serialised data.</param>
        /// <param name="index">The index at which to read.</param>
        /// <returns>The read value.</returns>
        protected bool ReadBool(byte[] buffer, int index)
        {
            return buffer[index] > 0 ? true : false;
        }

        /// <summary>
        /// Reads an unsigned short from serialised data.
        /// </summary>
        /// <param name="buffer">The serialised data.</param>
        /// <param name="index">The index at which to read.</param>
        /// <returns>The read value.</returns>
        protected ushort ReadUShort(byte[] buffer, int index)
        {
            return unchecked((ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, index)));
        }

        /// <summary>
        /// Reads an int from serialised data.
        /// </summary>
        /// <param name="buffer">The serialised data.</param>
        /// <param name="index">The index at which to read.</param>
        /// <returns>The read value.</returns>
        protected int ReadInt(byte[] buffer, int index)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, index));
        }

        /// <summary>
        /// Reads a float from serialised data.
        /// </summary>
        /// <param name="buffer">The serialised data.</param>
        /// <param name="index">The index at which to read.</param>
        /// <returns>The read value.</returns>
        protected float ReadFloat(byte[] buffer, int index)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(ReadInt(buffer, index)), 0);
        }

        /// <summary>
        /// Reads from the serialised representation of the message a given number of 
        /// bytes containing a zero-terminated string. The string will be read in 
        /// UTF-8 encoding. 
        /// </summary>
        /// <param name="buffer">The serialised data.</param>
        /// <param name="index">The index at which to start reading.</param>
        /// <param name="byteCount">The number of bytes to read.</param>
        /// <returns>The string.</returns>
        protected string ReadString(byte[] buffer, int index, int byteCount)
        {
            return Encoding.UTF8.GetString(buffer, index, byteCount);
        }

        #endregion Deserialisation interface for subclasses

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
