using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace AW2.Helpers.Serialization
{
    [TestFixture]
    public class NetworkBinaryWriterTest
    {
        [Test]
        public void TestByteOrderInt32()
        {
            int[] values = { 0x01020304, -0x01020304, -0x7ffdfbf7, 0x7ffdfbf7 };
            foreach (int value in values)
            {
                NetworkBinaryWriter writer = new NetworkBinaryWriter(new MemoryStream());
                writer.Write(value);
                byte[] data = ((MemoryStream)writer.BaseStream).ToArray();
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(data);
                Assert.AreEqual(value, BitConverter.ToInt32(data, 0));
            }
        }

        [Test]
        public void TestByteOrderSingle()
        {
            float[] values = { 1234.5678f, -1234.5678f, 987e20f, -987e20f, 123e-20f, -123e-20f, float.NaN, float.PositiveInfinity };
            foreach (float value in values)
            {
                NetworkBinaryWriter writer = new NetworkBinaryWriter(new MemoryStream());
                writer.Write(value);
                byte[] data = ((MemoryStream)writer.BaseStream).ToArray();
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(data);
                Assert.AreEqual(value, BitConverter.ToSingle(data, 0));
            }
        }

        [Test]
        public void TestByteOrderUint16()
        {
            ushort[] values = { 0x0102, 0xfffd };
            foreach (ushort value in values)
            {
                NetworkBinaryWriter writer = new NetworkBinaryWriter(new MemoryStream());
                writer.Write(value);
                byte[] data = ((MemoryStream)writer.BaseStream).ToArray();
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(data);
                Assert.AreEqual(value, BitConverter.ToUInt16(data, 0));
            }
        }
    }
}
