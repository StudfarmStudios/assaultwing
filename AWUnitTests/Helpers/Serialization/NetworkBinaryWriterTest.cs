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
                NetworkBinaryWriter writer = NetworkBinaryWriter.Create(new MemoryStream());
                writer.Write(value);
                byte[] data = ((MemoryStream)writer.GetBaseStream()).ToArray();
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
                NetworkBinaryWriter writer = NetworkBinaryWriter.Create(new MemoryStream());
                writer.Write(value);
                byte[] data = ((MemoryStream)writer.GetBaseStream()).ToArray();
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
                NetworkBinaryWriter writer = NetworkBinaryWriter.Create(new MemoryStream());
                writer.Write(value);
                byte[] data = ((MemoryStream)writer.GetBaseStream()).ToArray();
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(data);
                Assert.AreEqual(value, BitConverter.ToUInt16(data, 0));
            }
        }

        [Test]
        public static void TestNetworkVsRegularWriter()
        {
            MemoryStream ms1 = new MemoryStream();
            MemoryStream ms2 = new MemoryStream();
            NetworkBinaryWriter writer1 = new NetworkBinaryWriter(ms1);
            NetworkBinaryWriter writer2 = new ProfilingNetworkBinaryWriter(ms2);
            
            writer1.Write(new byte[] { 0xca, 0xfe, 0xd0 }, 1, 2);
            writer2.Write(new byte[] { 0xca, 0xfe, 0xd0 }, 1, 2);

            writer1.Write("Testixxx");
            writer2.Write("Testixxx");

            writer1.Write((bool)true);
            writer2.Write((bool)true);

            writer1.Write((char)'m');
            writer2.Write((char)'m');
            
            writer1.Write((short)32100);
            writer2.Write((short)32100);
            writer1.Write((ushort)49200);
            writer2.Write((ushort)49200);
            
            writer1.Write((int)0x1337f00d);
            writer2.Write((int)0x1337f00d);
            writer1.Write((uint)0xdeadbeef);
            writer2.Write((uint)0xdeadbeef);
            
            writer1.Write((byte)0x3c);
            writer2.Write((byte)0x3c);

            writer1.Write((long)0x1337d0d0cafef00d);
            writer2.Write((long)0x1337d0d0cafef00d);
            writer1.Write((ulong)0xdead000fcafef00d);
            writer2.Write((ulong)0xdead000fcafef00d);

            writer1.Write((Half)3.141592);
            writer2.Write((Half)3.141592);
            writer1.Write((float)3.333333);
            writer2.Write((float)3.333333);
            byte[] data1 = ms1.ToArray();
            byte[] data2 = ms2.ToArray();

            Assert.AreEqual(data1, data2);
        }
    }
}
