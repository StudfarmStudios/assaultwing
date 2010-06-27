using System;
using System.Text;
#if DEBUG
using NUnit.Framework;
#endif

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
        }
#endif
    }
}
