using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Graphics.Content
{

    public static class BinaryReaderExtensions
    {
        public static void AssertBytes(this BinaryReader reader, string errorMessage, params byte[] expected)
        {
            AssertArray(errorMessage, expected, () => reader.ReadByte());
        }

        public static void AssertBoolean(this BinaryReader reader, string errorMessage, bool expected)
        {
            AssertArray(errorMessage, new[] { expected }, () => reader.ReadBoolean());
        }

        public static void AssertInt32s(this BinaryReader reader, string errorMessage, params int[] expected)
        {
            AssertArray(errorMessage, expected, () => reader.ReadInt32());
        }

        private static void AssertArray(string errorMessage, Array expected, Func<object> readOne)
        {
            var actual = Enumerable.Range(0, expected.Length).Select(i => readOne()).ToArray();
            if (!Enumerable.SequenceEqual(expected.Cast<object>(), actual))
                throw new InvalidDataException(string.Format("{0} ({1} expected; was {2})", errorMessage, string.Join(", ", expected.Cast<object>()), string.Join(", ", actual)));
        }

        public static int Read7BitEncodedInt(this BinaryReader reader)
        {
            int value = 0;
            int bitsRead = 0;
            while (true)
            {
                var nextByte = reader.ReadByte();
                value |= (nextByte & 0x7f) << bitsRead;
                bitsRead += 7;
                if ((nextByte & 0x80) == 0) break;
            }
            return value;
        }

        public static Vector2 ReadVector2(this BinaryReader reader)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static VertexPositionNormalTexture ReadVertexPositionNormalTexture(this BinaryReader reader)
        {
            return new VertexPositionNormalTexture(reader.ReadVector3(), reader.ReadVector3(), reader.ReadVector2());
        }

        public static Matrix ReadMatrix(this BinaryReader reader)
        {
            return new Matrix(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static BoundingSphere ReadBoundingSphere(this BinaryReader reader)
        {
            return new BoundingSphere(reader.ReadVector3(), reader.ReadSingle());
        }
    }
}
