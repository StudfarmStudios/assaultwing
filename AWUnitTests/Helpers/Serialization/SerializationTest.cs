using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace AW2.Helpers.Serialization
{
    [TestFixture]
    public class SerializationTest
    {
        private Curve _curve;

        [SetUp]
        public void Setup()
        {
            _curve = new Curve();
            _curve.Keys.Add(new CurveKey(1, 2, 3, 4, CurveContinuity.Smooth));
            _curve.Keys.Add(new CurveKey(-5.5f, -6.5f, -7.5f, -8.5f, CurveContinuity.Step));
        }

        [Test]
        public void AssertCurveSerializationBetweenCultures(
            [Values("FI-fi", "EN-us")]
            string writeCulture,
            [Values("FI-fi", "EN-us")]
            string readCulture)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(writeCulture);
            var outputStream = Serialize(_curve);
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(readCulture);
            var newCurve = Deserialize(outputStream);
            Assert.AreEqual(_curve.IsConstant, newCurve.IsConstant);
            Assert.AreEqual(_curve.PostLoop, newCurve.PostLoop);
            Assert.AreEqual(_curve.PreLoop, newCurve.PreLoop);
            Assert.That(_curve.Keys.SequenceEqual(newCurve.Keys));
        }

        private static MemoryStream Serialize(Curve curve)
        {
            var stream = new MemoryStream();
            var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true });
            Serialization.SerializeXml(writer, "curve", curve, null);
            writer.Close();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private static Curve Deserialize(MemoryStream inputStream)
        {
            var reader = XmlReader.Create(inputStream);
            var newCurve = (Curve)Serialization.DeserializeXml(reader, "curve", typeof(Curve), null, tolerant: false);
            reader.Close();
            return newCurve;
        }

        [Test]
        public void TestDeepEquals()
        {
            AssertDeepEquals(true, null, null);
            AssertDeepEquals(false, null, 0);
            AssertDeepEquals(false, 0, null);
            AssertDeepEquals(false, 0, 1);
            AssertDeepEquals(true, int.MinValue, int.MinValue);
            AssertDeepEquals(true, "foo", "foo");
            AssertDeepEquals(false, "bar", "Bar");
            AssertDeepEquals(true, 123.456, 123.456);
            AssertDeepEquals(false, 123.456, -123.456);
            AssertDeepEquals(true, new[] { 2, 3, 4 }, new[] { 2, 3, 4 });
            AssertDeepEquals(false, new[] { 2, 3, 4 }, new[] { 2, 3, 4, 5 });
            AssertDeepEquals(false, new[] { 2, 3, 4 }, new[] { 2, 3 });
            AssertDeepEquals(false, new[] { 2, 3, 4 }, new[] { 2, 3, 4.0 });
            AssertDeepEquals(false, new object[] { null }, new[] { new object() });
            AssertDeepEquals(true, new[] { new object() }, new[] { new object() });
            AssertDeepEquals(true, Tuple.Create("foo", 42), Tuple.Create("foo", 42));
            AssertDeepEquals(false, Tuple.Create("bar", 42), Tuple.Create("foo", 42));
            AssertDeepEquals(false, Tuple.Create("foo", 69), Tuple.Create("foo", 42));
        }

        private void AssertDeepEquals(bool expected, object a, object b)
        {
            Assert.AreEqual(expected, Serialization.DeepEquals(a, b), "DeepEquals(" + GetName(a) + ", " + GetName(b) + ")");
        }

        private string GetName(object a)
        {
            return a == null ? "<null>" : a.ToString();
        }
    }
}
