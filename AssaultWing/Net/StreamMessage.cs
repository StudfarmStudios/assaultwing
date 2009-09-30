using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;

namespace AW2.Net
{
    /// <summary>
    /// A message containing a data sequence consisting of serialised 
    /// forms of objects (called "streamed data") in addition to 
    /// structured data built from primitive types.
    /// </summary>
    /// To initialise the streamed data of a message for sending, 
    /// call <c>Write</c> as many times as you like and make appropriate 
    /// calls to the various write methods of the returned writer. Then 
    /// call <c>EndWrite</c> before sending the message.
    /// 
    /// To get the streamed data from a message, call <c>BeginRead</c>
    /// and make appropriate calls to the various
    /// read methods of the returned reader. Then call <c>EndRead</c>.
    /// 
    /// Streamed data in a stream message is modal. See <c>DataMode</c>.
    public abstract class StreamMessage : Message
    {
        byte[] writeBytes;
        NetworkBinaryWriter writer;
        MemoryStream readBuffer;
        NetworkBinaryReader reader;

        /// <summary>
        /// How streamed data is being handled in a stream message.
        /// </summary>
        protected enum DataModeType
        {
            /// <summary>
            /// Streamed data is uninitialised. Nothing has been done with it.
            /// </summary>
            Uninitialized,

            /// <summary>
            /// Streamed data is being written to by calls to <c>Write()</c>.
            /// </summary>
            SettingDataByWrite,

            /// <summary>
            /// Streamed data has been written to by calls to <c>Write()</c>
            /// and the written buffer has been accessed, forbidding further
            /// write calls.
            /// </summary>
            DataSetByWrite,

            /// <summary>
            /// Streamed data has been set for reading.
            /// </summary>
            DataSetForRead,

            /// <summary>
            /// Streamed data has been set and it is being read by calls to <c>Read()</c>,
            /// forbidding an overwriting setting of the data.
            /// </summary>
            ReadingSetData,
        };

        /// <summary>
        /// Mode of streamed data in the stream message.
        /// </summary>
        protected DataModeType DataMode { get; set; }

        /// <summary>
        /// The streamed data in the message.
        /// </summary>
        /// Streamed data can be accessed after any number of calls to <c>Write()</c>.
        /// Prior to calls to <c>Read()</c>, the data must be set.
        /// It is not possible to read and write the same message instance.
        protected byte[] StreamedData
        {
            get
            {
                switch (DataMode)
                {
                    case DataModeType.Uninitialized:
                        writeBytes = new byte[0];
                        break;
                    case DataModeType.SettingDataByWrite:
                        writer.Flush();
                        writeBytes = ((MemoryStream)writer.BaseStream).ToArray();
                        writer.Close();
                        writer = null;
                        break;
                    case DataModeType.DataSetByWrite:
                        break;
                    default:
                        throw new InvalidOperationException("Cannot get streamed data in mode " + DataMode);
                }
                DataMode = DataModeType.DataSetByWrite;
                return writeBytes;
            }
            set {
                switch (DataMode)
                {
                    case DataModeType.Uninitialized:
                    case DataModeType.DataSetForRead:
                        break;
                    default:
                        throw new InvalidOperationException("Cannot set streamed data in mode " + DataMode);
                }
                DataMode = DataModeType.DataSetForRead;
                readBuffer = new MemoryStream(value);
            }
        }

        /// <summary>
        /// Initialises a stream message.
        /// </summary>
        public StreamMessage()
        {
            DataMode = DataModeType.Uninitialized;
        }

        /// <summary>
        /// Writes a serialisable object to streamed data, 
        /// appending to previously written streamed data.
        /// </summary>
        /// <param name="serializable">The object to serialise.</param>
        /// <param name="mode">What to serialise of the serialisable object.</param>
        /// <returns>The number of serialised bytes.</returns>
        public int Write(INetworkSerializable serializable, SerializationModeFlags mode)
        {
            switch (DataMode)
            {
                case DataModeType.Uninitialized:
                    writer = new NetworkBinaryWriter(new MemoryStream());
                    break;
                case DataModeType.SettingDataByWrite:
                    break;
                default:
                    throw new InvalidOperationException("Cannot Write() streamed data in mode " + DataMode);
            }
            DataMode = DataModeType.SettingDataByWrite;
            long oldPos = writer.Seek(0, SeekOrigin.Current);
            serializable.Serialize(writer, mode);
            long newPos = writer.Seek(0, SeekOrigin.Current);
            return (int)(newPos - oldPos);
        }

        /// <summary>
        /// Reads the state of a serialisable object from streamed data
        /// at the current read position.
        /// </summary>
        /// <param name="serializable">The object to adopt the serialised state.</param>
        /// <param name="mode">What kind of state we are deserialising. Must match 
        /// the mode the data was written in.</param>
        /// <param name="messageAge">How long ago was the message current.</param>
        public void Read(INetworkSerializable serializable, SerializationModeFlags mode, TimeSpan messageAge)
        {
            SetDataModeToRead();
            serializable.Deserialize(reader, mode, messageAge);

            // The reader will be closed when the message goes to garbage collection.
            // For now, we leave it open for successive calls to Read().
            //reader.Close();
        }

        /// <summary>
        /// Skips a number of bytes, as if they were read.
        /// </summary>
        /// <param name="byteCount">The number of bytes to skip.</param>
        public void Skip(int byteCount)
        {
            SetDataModeToRead();
            reader.ReadBytes(byteCount);
        }

        private void SetDataModeToRead()
        {
            switch (DataMode)
            {
                case DataModeType.DataSetForRead:
                    reader = new NetworkBinaryReader(readBuffer);
                    break;
                case DataModeType.ReadingSetData:
                    break;
                default:
                    throw new InvalidOperationException("Cannot read streamed data in mode " + DataMode);
            }
            DataMode = DataModeType.ReadingSetData;
        }
    }
}
