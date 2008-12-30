using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Net.Messages
{
    /// <summary>
    /// Parameters for creating a gob.
    /// </summary>
    public struct GobCreationParameters
    {
        /// <summary>
        /// Type name of the gob.
        /// </summary>
        public string typeName;

        /// <summary>
        /// Position of the gob.
        /// </summary>
        public Vector2 pos;

        /// <summary>
        /// Rotation of the gob.
        /// </summary>
        public float rotation;
    }

    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the creation of a gob.
    /// </summary>
    public class GobCreationMessage : Message
    {
        /// <summary>
        /// Parameters of the creation of the gob.
        /// </summary>
        public GobCreationParameters Parameters { get; set; }

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x22, false);

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        protected override void SerializeBody()
        {
            // Player controls (request) message structure:
            // string in 32 bytes: gob type name
            // float: pos X
            // float: pos Y
            // float: rotation
            WriteString(Parameters.typeName, 32, true);
            WriteFloat(Parameters.pos.X);
            WriteFloat(Parameters.pos.Y);
            WriteFloat(Parameters.rotation);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        protected override void Deserialize(byte[] body)
        {
            int index = 0;
            GobCreationParameters parameters = new GobCreationParameters();
            parameters.typeName = ReadString(body, index, 32); index += 32;
            parameters.pos.X = ReadFloat(body, index); index += 4;
            parameters.pos.Y = ReadFloat(body, index); index += 4;
            parameters.rotation = ReadFloat(body, index); index += 4;
            Parameters = parameters;
        }
    }
}
