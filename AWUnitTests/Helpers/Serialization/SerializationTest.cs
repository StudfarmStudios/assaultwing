using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace AW2.Helpers.Serialization
{
    [TestFixture]
    public class SerializationTest
    {
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
