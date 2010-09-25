using System;
using AW2.Helpers.Serialization;

namespace AW2.Net.Messages
{
    /// <summary>
    /// Flags for connection reply messages.
    /// </summary>
    [Flags]
    public enum ConnectionReplyFlags : byte
    {
        /// <summary>
        /// Firewall has been detected (???)
        /// </summary>
        TcpOk = 0x80,
    }

    /// <summary>
    /// A message from a management server to a game client
    /// for replying to a connection request.
    /// </summary>
    public class ConnectionReplyMessage : Message
    {
        /// <summary>
        /// Flags of the message.
        /// </summary>
        public ConnectionReplyFlags Flags { get; set; }

        /// <summary>
        /// Chosen authentication method.
        /// </summary>
        public ConnectionRequestAuthenticationMethod AuthenticationMethod { get; set; }

        /// <summary>
        /// Authentication challenge.
        /// </summary>
        public byte[] Challenge { get; set; }

        /// <summary>
        /// Challenge field length.
        /// </summary>
        const int challengeFieldLength = 32;

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x01, true);

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Connection reply message structure:
            // byte flags
            // byte authentication_method
            // byte[challengeFieldLength] challenge
            writer.Write((byte)Flags);
            writer.Write((byte)AuthenticationMethod);
            if (Challenge == null)
                throw new NullReferenceException("Null challenge field on serialization");
            if (Challenge.Length != challengeFieldLength)
                throw new MessageException("Invalid challenge field length on serialization (" + Challenge.Length + ", not " + challengeFieldLength + ")");
            writer.Write(Challenge);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            Flags = (ConnectionReplyFlags)reader.ReadByte();
            AuthenticationMethod = (ConnectionRequestAuthenticationMethod)reader.ReadByte();
            Challenge = reader.ReadBytes(challengeFieldLength);
        }
    }
}
