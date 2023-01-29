using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace AW2.Helpers.Serialization
{
    [TestFixture]
    public class NetworkBinaryReaderTest
    {
        [Test]
        public void TestByteOrderInt32()
        {
            byte[][] datas = { new byte[] { 1, 2, 3, 4 }, new byte[] { 255, 253, 251, 247 } };
            foreach (byte[] data in datas)
            {
                NetworkBinaryReader reader = new NetworkBinaryReader(new MemoryStream(data));
                int value = reader.ReadInt32();
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(data);
                Assert.AreEqual(BitConverter.ToInt32(data, 0), value);
            }
        }

        [Test]
        public void TestByteOrderSingle()
        {
            byte[] data = { 1, 2, 3, 4 };
            NetworkBinaryReader reader = new NetworkBinaryReader(new MemoryStream(data));
            float value = reader.ReadSingle();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
            Assert.AreEqual(BitConverter.ToSingle(data, 0), value);
        }

        [Test]
        public void TestByteOrderUint16()
        {
            byte[][] datas = { new byte[] { 1, 2 }, new byte[] { 253, 255 } };
            foreach (byte[] data in datas)
            {
                NetworkBinaryReader reader = new NetworkBinaryReader(new MemoryStream(data));
                ushort value = reader.ReadUInt16();
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(data);
                Assert.AreEqual(BitConverter.ToUInt16(data, 0), value);
            }
        }

        [Test]
        public void TestHalf()
        {
            var data = new[]
            {
                0f, -0f, 1f, -1f, 65504f, -65504f, 0.000061035156f, -0.000061035156f, 12.3359375f, -12.3359375f,
                float.NaN, float.PositiveInfinity, float.NegativeInfinity,
            };
            var stream = new MemoryStream();
            var writer = NetworkBinaryWriter.Create(stream);
            foreach (float value in data)
                writer.Write((Half)value);
            writer.Flush();
            var bytes = stream.GetBuffer();
            Assert.That(bytes.Any(x => x != 0), "Something wrong with memory stream usage?");
            stream = new MemoryStream(bytes);
            var reader = new NetworkBinaryReader(stream);
            var result = new float[data.Length];
            for (int i = 0; i < data.Length; ++i)
                result[i] = reader.ReadHalf();
            Assert.AreEqual(data, result);
        }
    }
}
