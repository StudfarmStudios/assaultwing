using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;

namespace AW2.Net.Messages
{
    /// <summary>
    /// A message containing streamed data with unspecified structure
    /// in addition to structured data built from primitive types.
    /// </summary>
    /// To initialise the streamed data of a message for sending, 
    /// call <c>BeginWrite</c> and make appropriate calls to the various
    /// write methods of the returned writer. Then call <c>EndWrite</c>
    /// before sending the message.
    /// 
    /// To get the streamed data from a message, call <c>BeginRead</c>
    /// and make appropriate calls to the various
    /// read methods of the returned reader. Then call <c>EndRead</c>.
    public abstract class StreamMessage : Message
    {
        byte[] writeBytes;
        NetworkBinaryWriter writer;
        MemoryStream readBuffer;
        NetworkBinaryReader reader;

        /// <summary>
        /// The streamed data in the message.
        /// </summary>
        protected byte[] StreamedData
        {
            get
            {
                if (writer != null)
                    throw new InvalidOperationException("Previous StreamMessage write hasn't finished");
                if (writeBytes == null)
                    throw new InvalidOperationException("No StreamMessage data has been written");
                return writeBytes;
            }
            set { readBuffer = new MemoryStream(value); }
        }

        /// <summary>
        /// Begins a write of streamed data.
        /// Call <c>EndWrite</c> when you're finished.
        /// </summary>
        /// <returns>Where the serialised data is to be written.</returns>
        /// <seealso cref="EndWrite"/>
        public NetworkBinaryWriter BeginWrite()
        {
            if (writer != null)
                throw new InvalidOperationException("StreamMessage write already in progress");
            writer = new NetworkBinaryWriter(new MemoryStream());
            return writer;
        }

        /// <summary>
        /// Ends a previously begun write of streamed data.
        /// </summary>
        /// <seealso cref="BeginWrite"/>
        public void EndWrite()
        {
            if (writer == null)
                throw new InvalidOperationException("No StreamMessage write is in progress");
            writer.Flush();
            writeBytes = ((MemoryStream)writer.BaseStream).ToArray();
            writer.Close();
            writer = null;
        }

        /// <summary>
        /// Begins a read of streamed data.
        /// Call <c>EndRead</c> when you're finished.
        /// </summary>
        /// <returns>Where the serialised data is to be read.</returns>
        /// <seealso cref="EndRead"/>
        public NetworkBinaryReader BeginRead()
        {
            if (reader != null)
                throw new InvalidOperationException("StreamMessage read already in progress");
            if (readBuffer == null)
                throw new InvalidOperationException("There is no streamed data in StreamMessage to read");
            reader = new NetworkBinaryReader(readBuffer);
            return reader;
        }

        /// <summary>
        /// Ends a previously begun read of streamed data.
        /// </summary>
        /// <seealso cref="BeginRead"/>
        public void EndRead()
        {
            if (reader == null)
                throw new InvalidOperationException("No StreamMessage read is in progress");
            reader.Close();
            reader = null;
        }
    }
}
