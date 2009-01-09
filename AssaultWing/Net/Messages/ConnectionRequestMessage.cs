using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace AW2.Net.Messages
{
    /// <summary>
    /// Flags for connection request messages.
    /// </summary>
    [Flags]
    public enum ConnectionRequestFlags : byte
    {
        DontWannaBeServer = 0x80,
    }

    /// <summary>
    /// Method of authentication for connection request messages.
    /// </summary>
    public enum ConnectionRequestAuthenticationMethod : byte
    {
        None = 0,
    }

    /// <summary>
    /// A message from a game client to a management server 
    /// for requesting connection.
    /// </summary>
    public class ConnectionRequestMessage : Message
    {
        ConnectionRequestFlags flags;
        public ConnectionRequestFlags Flags { get { return flags; } set { flags = value; } }

        ConnectionRequestAuthenticationMethod authenticationMethod;
        public ConnectionRequestAuthenticationMethod AuthenticationMethod { get { return authenticationMethod; } set { authenticationMethod = value; } }

        ushort serverPort;
        public ushort ServerPort { get { return serverPort; } set { serverPort = value; } }

        string username;
        public string Username { get { return username; } set { username = value; } }

        /// <summary>
        /// User name field length. Including trailing zero!
        /// </summary>
        const int usernameFieldLength = 32;

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x01, false);

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
            // Connection request message structure:
            // byte flags
            // byte authentication_method
            // word server_port
            // char[usernameFieldLength] username
            writer.Write((byte)flags);
            writer.Write((byte)authenticationMethod);
            writer.Write((ushort)serverPort);
            writer.Write((string)username, usernameFieldLength, true);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            flags = (ConnectionRequestFlags)reader.ReadByte();
            authenticationMethod = (ConnectionRequestAuthenticationMethod)reader.ReadByte();
            serverPort = reader.ReadUInt16();
            username = reader.ReadString(usernameFieldLength);
        }
    }
}
