using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.IO;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message from a game server to a game client notifying
    /// that the game session starts. The message contains information
    /// about the player setup of the game session.
    /// </summary>
    public class StartGameMessage : Message
    {
        byte[] writeBytes;
        NetworkBinaryWriter writer;
        MemoryStream readBuffer;
        NetworkBinaryReader reader;

        /// <summary>
        /// Identifier of the message type.
        /// </summary>
        protected static MessageType messageType = new MessageType(0x23, false);

        /// <summary>
        /// Number of players in the game session.
        /// </summary>
        public int PlayerCount { get; set; }

        /// <summary>
        /// Begins a write of serialised data of the players in the game session. 
        /// Call <c>EndWrite</c> when you're finished.
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
        /// Ends a previously begun write of serialised data of the players in the game session.
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
        /// Begins a read of serialised data of the players in the game session.
        /// Call <c>EndRead</c> when you're finished.
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
        /// Ends a previously begun read of serialised data of the players in the game session.
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
        protected override void Serialize(NetworkBinaryWriter writer)
        {
            // Start game (request) message structure:
            // int: total number of players in the game, N
            // word: length of serialised data of all players, L
            // L bytes: serialised data of N players (content known only by Player)
            if (writeBytes == null || this.writer != null)
                throw new InvalidOperationException("Previous write hasn't finished or didn't even begin");
            writer.Write((int)PlayerCount);
            writer.Write(checked((ushort)writeBytes.Length));
            writer.Write(writeBytes, 0, writeBytes.Length);
        }

        /// <summary>
        /// Reads the body of the message from serialised form.
        /// </summary>
        /// <param name="reader">Reader of serialised data.</param>
        protected override void Deserialize(NetworkBinaryReader reader)
        {
            PlayerCount = reader.ReadInt32();
            int byteCount = reader.ReadUInt16();
            readBuffer = new MemoryStream(reader.ReadBytes(byteCount));
        }
    }
}
