#if DEBUG
using NUnit.Framework;
#endif
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Helpers
{
    /// <summary>
    /// Contains miscellaneous extension methods.
    /// </summary>
    public static class MiscHelper
    {
        /// <summary>
        /// Returns the string starting with a capital letter.
        /// </summary>
        public static string Capitalize(this string value)
        {
            if (value == "") return "";
            return value.Substring(0, 1).ToUpper() + value.Substring(1);
        }

        /// <summary>
        /// Returns the string with each word starting with a capital letter.
        /// </summary>
        public static string CapitalizeWords(this string value)
        {
            if (value == "") return "";
            var result = new StringBuilder(value);
            result[0] = char.ToUpper(result[0]);
            for (int i = 0; i < result.Length - 1; ++i)
                if (result[i] == ' ') result[i + 1] = char.ToUpper(result[i + 1]);
            return result.ToString();
        }

        /// <summary>
        /// Compares two sequences element by element for the first non-equal pair.
        /// </summary>
        /// <param name="a">First sequence</param>
        /// <param name="b">Second sequence</param>
        /// <param name="aDiff">First differing element in <paramref name="a"/>. Is null if either
        /// the sequences were otherwise equal but <paramref name="b"/> had more elements,
        /// or there were no differences in the sequences.</param>
        /// <param name="aDiff">First differing element in <paramref name="b"/>. Is null if either
        /// the sequences were otherwise equal but <paramref name="a"/> had more elements,
        /// or there were no differences in the sequences.</param>
        /// <returns>true if there was a difference; false if the sequences were equal</returns>
        public static bool FirstDifference<T>(IEnumerable<T> a, IEnumerable<T> b, out T aDiff, out T bDiff) where T : class
        {
            aDiff = null;
            bDiff = null;
            using (IEnumerator<T> aEnum = a.GetEnumerator(), bEnum = b.GetEnumerator())
            {
                bool aHas, bHas;
                while (true)
                {
                    aHas = aEnum.MoveNext();
                    bHas = bEnum.MoveNext();
                    if (!aHas || !bHas) break;
                    var aItem = aEnum.Current;
                    var bItem = bEnum.Current;
                    if (aItem == null && bItem == null) continue;
                    if (aItem != null && bItem != null && aItem.Equals(bItem)) continue;
                    aDiff = aItem;
                    bDiff = bItem;
                    return true;
                }
                if (aHas) aDiff = aEnum.Current;
                if (bHas) bDiff = bEnum.Current;
                return aHas || bHas;
            }
        }

        public static Vector2 Dimensions(this Texture2D texture)
        {
            return new Vector2(texture.Width, texture.Height);
        }

#if DEBUG
        [TestFixture]
        public class UnitTests
        {
            [Test]
            public void TestCapitalize()
            {
                Assert.AreEqual("Testing", "testing".Capitalize());
                Assert.AreEqual("", "".Capitalize());
                Assert.AreEqual("Testing", "Testing".Capitalize());
                Assert.AreEqual("TESTING", "TESTING".Capitalize());
                Assert.AreEqual("Testing 123 testing", "Testing 123 testing".Capitalize());
                Assert.AreEqual(" testing", " testing".Capitalize());
                Assert.AreEqual("!\"#¤%&/()=", "!\"#¤%&/()=".Capitalize());
            }

            [Test]
            public void TestCapitalizeWords()
            {
                Assert.AreEqual("Testing", "testing".CapitalizeWords());
                Assert.AreEqual("", "".CapitalizeWords());
                Assert.AreEqual("Testing", "Testing".CapitalizeWords());
                Assert.AreEqual("TESTING", "TESTING".CapitalizeWords());
                Assert.AreEqual("Testing 123 Testing", "Testing 123 testing".CapitalizeWords());
                Assert.AreEqual(" Testing", " testing".CapitalizeWords());
                Assert.AreEqual("!\"#¤%&/()=", "!\"#¤%&/()=".Capitalize());
                Assert.AreEqual("Testing  Testing", "Testing  testing".CapitalizeWords());
                Assert.AreEqual("Testing-testing", "Testing-testing".CapitalizeWords());
            }

            [Test]
            public void TestFirstDifference()
            {
                object a, b;
                Assert.False(FirstDifference(new object[] { 2, 3, 4 }, new object[] { 2, 3, 4 }, out a, out b));
                Assert.Null(a);
                Assert.Null(b);
                Assert.False(FirstDifference(new object[] { null, null }, new object[] { null, null }, out a, out b));
                Assert.Null(a);
                Assert.Null(b);
                Assert.False(FirstDifference(new object[0], new object[0], out a, out b));
                Assert.Null(a);
                Assert.Null(b);
                Assert.True(FirstDifference(new object[] { 1, 3, 4 }, new object[] { 2, 3, 4 }, out a, out b));
                Assert.AreEqual(1, a);
                Assert.AreEqual(2, b);
                Assert.True(FirstDifference(new object[] { 1, 3, 4 }, new object[] { 1, 3, 5 }, out a, out b));
                Assert.AreEqual(4, a);
                Assert.AreEqual(5, b);
                Assert.True(FirstDifference(new object[] { 1, 3, 4 }, new object[] { 1, null, 4 }, out a, out b));
                Assert.AreEqual(3, a);
                Assert.AreEqual(null, b);
                Assert.True(FirstDifference(new object[] { 1, 3, 4 }, new object[] { 1, 3 }, out a, out b));
                Assert.AreEqual(4, a);
                Assert.Null(b);
                Assert.True(FirstDifference(new object[0], new object[] { 1 }, out a, out b));
                Assert.Null(a);
                Assert.AreEqual(1, b);
            }
        }
#endif
    }
}
