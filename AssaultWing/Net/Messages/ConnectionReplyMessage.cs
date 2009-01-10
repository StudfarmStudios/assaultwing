using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace AW2.Net.Messages
{
    /// <summary>
    /// Flags for connection reply messages.
    /// </summary>
    [Flags]
    public enum ConnectionReplyFlags : byte
    {
        TcpOk = 0x80,
    }

    /// <summary>
    /// A message from a management server to a game client
    /// for replying to a connection request.
    /// </summary>
    public class ConnectionReplyMessage : Message
    {
        ConnectionReplyFlags flags;
        public ConnectionReplyFlags Flags { get { return flags; } set { flags = value; } }

        ConnectionRequestAuthenticationMethod authenticationMethod;
        public ConnectionRequestAuthenticationMethod AuthenticationMethod { get { return authenticationMethod; } set { authenticationMethod = value; } }

        byte[] challenge;
        public byte[] Challenge { get { return challenge; } set { challenge = value; } }

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
            writer.Write((byte)flags);
            writer.Write((byte)authenticationMethod);
            if (challenge == null)
                throw new NullReferenceException("Null challenge field on serialization");
            if (challenge.Length != challengeFieldLength)
                throw new MessageException("Invalid challenge field length on serialization (" + challenge.Length + ", not " + challengeFieldLength + ")");
            writer.Write(challenge);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            flags = (ConnectionReplyFlags)reader.ReadByte();
            authenticationMethod = (ConnectionRequestAuthenticationMethod)reader.ReadByte();
            challenge = reader.ReadBytes(challengeFieldLength);
        }
    }
}
