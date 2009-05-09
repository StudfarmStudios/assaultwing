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
        /// <summary>
        /// The game instance doesn't want to be a server.
        /// </summary>
        DontWannaBeServer = 0x80,
    }

    /// <summary>
    /// Method of authentication for connection request messages.
    /// </summary>
    public enum ConnectionRequestAuthenticationMethod : byte
    {
        /// <summary>
        /// No authentication.
        /// </summary>
        None = 0,
    }

    /// <summary>
    /// A message from a game instance to a management server 
    /// for requesting connection.
    /// </summary>
    public class ConnectionRequestMessage : Message
    {
        /// <summary>
        /// Flags of the message.
        /// </summary>
        public ConnectionRequestFlags Flags { get; set; }

        /// <summary>
        /// Chosen authentication method.
        /// </summary>
        public ConnectionRequestAuthenticationMethod AuthenticationMethod { get; set; }

        /// <summary>
        /// TCP port of the game instance, in case the game instance becomes the game server.
        /// </summary>
        public ushort ServerPort { get; set; }

        /// <summary>
        /// Name of the user on the game instance.
        /// </summary>
        public string Username { get; set; }

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
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Connection request message structure:
            // byte flags
            // byte authentication_method
            // word server_port
            // char[usernameFieldLength] username
            writer.Write((byte)Flags);
            writer.Write((byte)AuthenticationMethod);
            writer.Write((ushort)ServerPort);
            writer.Write((string)Username, usernameFieldLength, true);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            Flags = (ConnectionRequestFlags)reader.ReadByte();
            AuthenticationMethod = (ConnectionRequestAuthenticationMethod)reader.ReadByte();
            ServerPort = reader.ReadUInt16();
            Username = reader.ReadString(usernameFieldLength);
        }
    }
}
