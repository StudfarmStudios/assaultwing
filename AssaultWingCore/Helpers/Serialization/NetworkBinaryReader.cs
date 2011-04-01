using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Reads primitive types in binary from a network stream 
    /// and supports reading strings in a specific encoding.
    /// Takes care of byte order.
    /// </summary>
    public class NetworkBinaryReader : BinaryReader
    {
        static char[] nullCharArray = new char[] { '\0' };

        /// <summary>
        /// Creates a new network binary reader that writes to an output stream.
        /// </summary>
        /// <param name="input">The stream to read from.</param>
        public NetworkBinaryReader(Stream input)
            : base(input, Encoding.UTF8)
        {
        }

        /// <summary>
        /// Reads an unsigned short.
        /// </summary>
        /// <returns>The read value.</returns>
        public override ushort ReadUInt16()
        {
            return unchecked((ushort)IPAddress.NetworkToHostOrder(base.ReadInt16()));
        }

        /// <summary>
        /// Reads an int.
        /// </summary>
        /// <returns>The read value.</returns>
        public override int ReadInt32()
        {
            return IPAddress.NetworkToHostOrder(base.ReadInt32());
        }

        /// <summary>
        /// Reads a float.
        /// </summary>
        /// <returns>The read value.</returns>
        public override float ReadSingle()
        {
            return Converter.IntToFloat(ReadInt32());
        }

        /// <summary>
        /// Reads a 16-bit floating point value.
        /// </summary>
        /// <returns>The read value.</returns>
        public Half ReadHalf()
        {
            short bits = ReadInt16();
            return Converter.ShortToHalf(bits);
        }

        /// <summary>
        /// Reads a length-prefixed string.
        /// </summary>
        public override string ReadString()
        {
            int length = ReadInt32();
            var chars = ReadChars(length);
            return new string(chars);
        }

        /// <summary>
        /// Reads a given number of bytes containing a zero-terminated string.
        /// The string will be read in UTF-8 encoding. 
        /// </summary>
        /// The same number of bytes will be read regardless of the length of the string.
        /// <param name="byteCount">The number of bytes to read.</param>
        /// <returns>The string.</returns>
        public string ReadString(int byteCount)
        {
            byte[] bytes = base.ReadBytes(byteCount);
            return Encoding.UTF8.GetString(bytes).TrimEnd(nullCharArray);
        }

        public CanonicalString ReadCanonicalString()
        {
            return (CanonicalString)ReadInt32();
        }

        /// <summary>
        /// Reads a Vector2 value.
        /// </summary>
        public Vector2 ReadVector2()
        {
            return new Vector2
            {
                X = ReadSingle(),
                Y = ReadSingle()
            };
        }

        /// <summary>
        /// Reads a Vector3 value.
        /// </summary>
        public Vector3 ReadVector3()
        {
            return new Vector3
            {
                X = ReadSingle(),
                Y = ReadSingle(),
                Z = ReadSingle()
            };
        }

        /// <summary>
        /// Reads a 3D model vertex.
        /// </summary>
        public VertexPositionNormalTexture ReadVertexPositionTextureNormal()
        {
            return new VertexPositionNormalTexture
            {
                Position = ReadVector3(),
                Normal = ReadVector3(),
                TextureCoordinate = ReadVector2()
            };
        }

        public TimeSpan ReadTimeSpan()
        {
            return new TimeSpan(ReadInt64());
        }

        public Color ReadColor()
        {
            return new Color { PackedValue = ReadUInt32() };
        }

        public Vector2 ReadVector2Normalized16(float minNormalized, float maxNormalized)
        {
            var scale = (maxNormalized - minNormalized) / ushort.MaxValue;
            return new Vector2
            {
                X = minNormalized + ReadUInt16() * scale,
                Y = minNormalized + ReadUInt16() * scale
            };
        }

        public Vector2 ReadVector2Normalized8(float minNormalized, float maxNormalized)
        {
            var scale = (maxNormalized - minNormalized) / byte.MaxValue;
            return new Vector2
            {
                X = minNormalized + ReadByte() * scale,
                Y = minNormalized + ReadByte() * scale
            };
        }

        /// <summary>
        /// Reads a Vector2 value given in half precision.
        /// </summary>
        public Vector2 ReadHalfVector2()
        {
            return new Vector2
            {
                X = ReadHalf(),
                Y = ReadHalf()
            };
        }

        /// <summary>
        /// Reads a Vector3 value given in half precision.
        /// </summary>
        public Vector3 ReadHalfVector3()
        {
            return new Vector3
            {
                X = ReadHalf(),
                Y = ReadHalf(),
                Z = ReadHalf()
            };
        }

        /// <summary>
        /// Reads a 3D model vertex given in half precision.
        /// </summary>
        public VertexPositionNormalTexture ReadHalfVertexPositionTextureNormal()
        {
            return new VertexPositionNormalTexture
            {
                Position = ReadHalfVector3(),
                Normal = ReadHalfVector3(),
                TextureCoordinate = ReadHalfVector2()
            };
        }
    }
}
