using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Writes primitive types in binary to a network stream 
    /// and supports writing strings in a specific encoding.
    /// Takes care of byte order.
    /// </summary>
    public class NetworkBinaryWriter
    {
        /// <summary>
        /// Creates a new network binary writer that writes to an output stream.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// 
        public static NetworkBinaryWriter Create(Stream output)
        {
#if NETWORK_PROFILING
            return new ProfilingNetworkBinaryWriter(output);
#else
            return new NetworkBinaryWriter(output);
#endif
        }
        
         public NetworkBinaryWriter(Stream output)
        {
            writer = new BinaryWriter(output, Encoding.UTF8);
        }

         public void Write(bool p)
         {
             Write(BitConverter.GetBytes(p));
         }

         public void Write(byte b)
         {
             Write(new byte[] { b });
         }

         public void Write(byte[] bytes)
         {
             WriteBytes(bytes, 0, bytes.Length);
         }
        public void Write(Char ch)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(new char[] { ch });
            Write(bytes);
        }

        public void Write(Char[] chars)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(chars);
            Write(bytes);
        }

        public void Write(Decimal d)
        {
            throw new NotSupportedException();
        }

        public void Write(double d)
        {
            throw new NotSupportedException();
        }

        public void Write(short value)
        {
            Write(BitConverter.GetBytes(value));
        }

        public void Write(int value)
        {
            Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
        }

        public void Write(long value)
        {
            Write(BitConverter.GetBytes(value));
        }

        public void Write(sbyte b)
        {
            Write(new byte[] { unchecked((byte)b) });
        }

        /// <summary>
        /// Writes a length-prefixed string.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public void Write(string value)
        {
            Write((int)value.Length);
            byte[] bytes = Encoding.UTF8.GetBytes((char[])value.ToCharArray());
            Write(bytes);
        }

        public void Write(ushort value)
        {
            Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(unchecked((short)value))));
        }

        public void Write(uint value)
        {
            Write(BitConverter.GetBytes(unchecked((int)value)));
        }

        public void Write(ulong value)
        {
            Write(BitConverter.GetBytes(unchecked((long)value)));
        }

        public void Write(float value)
        {
            Write(Converter.FloatToInt(value));
        }

        /// <summary>
        /// Writes a 16-bit floating point value.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(Half value)
        {
            short half = Converter.HalfToShort(value);
            Write(BitConverter.GetBytes((short)half));
        }

        public void Flush()
        {
            writer.Flush();
        }
        
        public void Close()
        {
            writer.Close();
        }
        
        public long Seek(int offset, SeekOrigin origin)
        {
            return writer.Seek(offset, origin);
        }

        public Stream GetBaseStream()
        {
            return writer.BaseStream;
        }

        /// <summary>
        /// Writes a given number of 
        /// bytes containing a string and a trailing sequence of one or more zero bytes.
        /// The string is truncated to fit the byte count, and an optional exception is 
        /// thrown if this happens. The string will be written in UTF-8 encoding. 
        /// </summary>
        /// <param name="value">The string to write.</param>
        /// <param name="byteCount">The exact number of bytes to write, including the
        /// trailing zero.</param>
        /// <param name="throwOnTruncate">If <c>true</c> then an exception will be
        /// thrown if the string is too long to fit the given number of bytes.</param>
        public void Write(string value, int byteCount, bool throwOnTruncate)
        {
            if (byteCount < 1)
                throw new ArgumentException("Need at least one byte to write a string with a trailing zero");
            Encoding encoding = Encoding.UTF8;
            int bytesNeeded = encoding.GetByteCount(value);
            if (bytesNeeded + 1 > byteCount)
            {
                if (throwOnTruncate)
                    throw new ArgumentException("String too long (" + (bytesNeeded + 1) + ") to fit given byte count (" + byteCount + ")");

                // Binary search for the maximum number of chars that fit.
                char[] valueChars = value.ToCharArray();
                int goodCharCount = 0, badCharCount = valueChars.Length;
                bytesNeeded = 0;
                while (badCharCount - goodCharCount > 1)
                {
                    int charCount = (goodCharCount + badCharCount) / 2;
                    int bytesNeededNow = encoding.GetByteCount(valueChars, 0, charCount);
                    if (bytesNeededNow + 1 > byteCount)
                        badCharCount = charCount;
                    else
                    {
                        goodCharCount = charCount;
                        bytesNeeded = bytesNeededNow;
                    }
                }
                WriteBytes(encoding.GetBytes(valueChars, 0, goodCharCount), 0, bytesNeeded);
            }
            else
                WriteBytes(encoding.GetBytes(value), 0, bytesNeeded);

            // Pad with zero bytes.
            for (int i = bytesNeeded; i < byteCount; ++i)
                Write((byte)0);
        }

        public void Write(CanonicalString value)
        {
            Write((int)value.Canonical);
        }

        /// <summary>
        /// Writes a Vector2 value.
        /// </summary>
        public void Write(Vector2 vector)
        {
            Write((float)vector.X);
            Write((float)vector.Y);
        }

        /// <summary>
        /// Writes a Vector3 value.
        /// </summary>
        public void Write(Vector3 vector)
        {
            Write((float)vector.X);
            Write((float)vector.Y);
            Write((float)vector.Z);
        }

        /// <summary>
        /// Writes a 3D model vertex.
        /// </summary>
        public void Write(VertexPositionNormalTexture vertex)
        {
            Write((Vector3)vertex.Position);
            Write((Vector3)vertex.Normal);
            Write((Vector2)vertex.TextureCoordinate);
        }

        public void Write(TimeSpan timeSpan)
        {
            Write((long)timeSpan.Ticks);
        }

        public void Write(Color color)
        {
            Write((uint)color.PackedValue);
        }

        /// <summary>
        /// Writes a Vector2 value using half precision.
        /// </summary>
        public void WriteHalf(Vector2 vector)
        {
            Write((Half)vector.X);
            Write((Half)vector.Y);
        }

        /// <summary>
        /// Writes a Vector3 value using half precision.
        /// </summary>
        public void WriteHalf(Vector3 vector)
        {
            Write((Half)vector.X);
            Write((Half)vector.Y);
            Write((Half)vector.Z);
        }

        /// <summary>
        /// Writes a 3D model vertex using half precision.
        /// </summary>
        public void WriteHalf(VertexPositionNormalTexture vertex)
        {
            WriteHalf((Vector3)vertex.Position);
            WriteHalf((Vector3)vertex.Normal);
            WriteHalf((Vector2)vertex.TextureCoordinate);
        }

        public void Write(byte[] writeBytes, int idx, int count)
        {
            WriteBytes(writeBytes, idx, count);
        }
        
        // ONLY WriteBytes is allowed to call writer.Write methods!

        protected virtual void WriteBytes(byte[] bytes, int index, int count)
        {
            writer.Write(bytes, index, count);
        }

        // Delegate to writer instead of inheriting, to prevent calling BinaryWriter methods.

        private BinaryWriter writer;

    }
}
