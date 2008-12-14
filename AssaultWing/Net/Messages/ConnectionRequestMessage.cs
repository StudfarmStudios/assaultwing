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

        protected override void SerializeBody()
        {
            // Connection request message structure:
            // byte flags
            // byte authentication_method
            // word server_port
            // char[usernameFieldLength] username
            WriteByte((byte)flags);
            WriteByte((byte)authenticationMethod);
            WriteUShort(serverPort);
            WriteString(username, usernameFieldLength, true);
        }

        protected override void Deserialize(byte[] body)
        {
            flags = (ConnectionRequestFlags)body[0];
            authenticationMethod = (ConnectionRequestAuthenticationMethod)body[1];
            serverPort = ReadUShort(body, 2);
            username = ReadString(body, 4, usernameFieldLength);
        }
    }
}
