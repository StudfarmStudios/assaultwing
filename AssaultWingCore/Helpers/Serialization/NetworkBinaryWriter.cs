using System;
using System.Text;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework;

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
        public void Write(string value)
        {
            checked
            {
                Write((ushort)value.Length);
                var bytes = Encoding.UTF8.GetBytes((char[])value.ToCharArray());
                Write(bytes);
            }
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

        public void Write(CanonicalString value)
        {
            checked
            {
                Write((short)value.Canonical);
            }
        }

        public void Write(Vector2 vector)
        {
            Write((float)vector.X);
            Write((float)vector.Y);
        }

        public void Write(Vector3 vector)
        {
            Write((float)vector.X);
            Write((float)vector.Y);
            Write((float)vector.Z);
        }

        public void Write(TimeSpan timeSpan)
        {
            Write((long)timeSpan.Ticks);
        }

        public void Write(Color color)
        {
            Write((uint)color.PackedValue);
        }

        public void WriteNormalized16(Vector2 vector, float minNormalized, float maxNormalized)
        {
            checked
            {
                var scale = ushort.MaxValue / (maxNormalized - minNormalized);
                Write((ushort)MathHelper.Clamp(((vector.X - minNormalized) * scale), 0, ushort.MaxValue));
                Write((ushort)MathHelper.Clamp(((vector.Y - minNormalized) * scale), 0, ushort.MaxValue));
            }
        }

        public void WriteNormalized8(Vector2 vector, float minNormalized, float maxNormalized)
        {
            checked
            {
                var scale = byte.MaxValue / (maxNormalized - minNormalized);
                Write((byte)MathHelper.Clamp(((vector.X - minNormalized) / scale), 0, byte.MaxValue));
                Write((byte)MathHelper.Clamp(((vector.Y - minNormalized) / scale), 0, byte.MaxValue));
            }
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
