using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// of the creation of a gob.
    /// </summary>
    /// To initialise a message for sending, call <c>BeginWrite</c> and
    /// serialise the gob's state with appropriate calls to the various
    /// write methods of the returned writer. Then call <c>EndWrite</c>
    /// and send the message.
    /// 
    /// To get the serialised data from a message, call <c>BeginRead</c>
    /// and deserialise the gob's state with appropriate calls to the various
    /// read methods of the returned reader. Then call <c>EndRead</c>.
    public class GobCreationMessage : Message
    {
        byte[] writeBytes;
        NetworkBinaryWriter writer;
        MemoryStream readBuffer;
        NetworkBinaryReader reader;

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x22, false);

        /// <summary>
        /// Type name of the gob to create.
        /// </summary>
        public string GobTypeName { get; set; }

        /// <summary>
        /// Begins a write of serialised data of the gob 
        /// whose creation we are signalling. Call <c>EndWrite</c> when you're finished.
        /// </summary>
        /// <returns>Where the serialised data is to be written.</returns>
        /// <seealso cref="EndWrite"/>
        public NetworkBinaryWriter BeginWrite()
        {
            if (writer != null)
                throw new InvalidOperationException("Write already in progress");
            writer = new NetworkBinaryWriter(new MemoryStream());
            return writer;
        }

        /// <summary>
        /// Ends a previously begun write of serialised data of the gob
        /// whose creation we are signalling.
        /// </summary>
        /// <seealso cref="BeginWrite"/>
        public void EndWrite()
        {
            if (writer == null)
                throw new InvalidOperationException("No write is in progress");
            writer.Flush();
            writeBytes = ((MemoryStream)writer.BaseStream).ToArray();
            writer.Close();
            writer = null;
        }

        /// <summary>
        /// Begins a read of serialised data of the gob 
        /// whose creation we are signalling. Call <c>EndRead</c> when you're finished.
        /// </summary>
        /// <returns>Where the serialised data is to be read.</returns>
        /// <seealso cref="EndRead"/>
        public NetworkBinaryReader BeginRead()
        {
            if (reader != null)
                throw new InvalidOperationException("Read already in progress");
            if (readBuffer == null)
                throw new InvalidOperationException("There is no data to read");
            reader = new NetworkBinaryReader(readBuffer);
            return reader;
        }

        /// <summary>
        /// Ends a previously begun read of serialised data of the gob
        /// whose creation we are signalling.
        /// </summary>
        /// <seealso cref="BeginRead"/>
        public void EndRead()
        {
            if (reader == null)
                throw new InvalidOperationException("No read is in progress");
            reader.Close();
            reader = null;
        }

        /// <summary>
        /// Writes the body of the message in serialised form.
        /// </summary>
        /// <param name="writer">Writer of serialised data.</param>
        protected override void SerializeBody(NetworkBinaryWriter writer)
        {
            // Player controls (request) message structure:
            // 32 bytes string: gob type name
            // word: data length N
            // N bytes: serialised data of the gob (content known only by the Gob subclass in question)
            if (writeBytes == null || this.writer != null)
                throw new InvalidOperationException("Previous write hasn't finished or didn't even begin");
            writer.Write((string)GobTypeName, 32, true);
            writer.Write(checked((ushort)writeBytes.Length));
            writer.Write(writeBytes, 0, writeBytes.Length);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            GobTypeName = reader.ReadString(32);
            int byteCount = reader.ReadUInt16();
            readBuffer = new MemoryStream(reader.ReadBytes(byteCount));
        }
    }
}
