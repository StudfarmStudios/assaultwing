using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;

namespace AW2.Core
{
    [TestFixture]
    public class CommandLineOptionsTest
    {
        [Test]
        public void TestEmptyOptions()
        {
            var opts = new CommandLineOptions(new string[] { }, new NameValueCollection(), "");
            Assert.IsNull(opts.ArenaFilename);
            Assert.IsFalse(opts.DedicatedServer);
            Assert.IsFalse(opts.DeleteTemplates);
            Assert.IsFalse(opts.SaveTemplates);
        }

        [Test]
        public void TestCommandLineSet()
        {
            var opts = new CommandLineOptions(new string[] { "--dedicated_server", "delete_templates", "--arena", "foo.xml" }, new NameValueCollection(), "");
            Assert.AreEqual("foo.xml", opts.ArenaFilename);
            Assert.IsTrue(opts.DedicatedServer);
            Assert.IsFalse(opts.DeleteTemplates);
            Assert.IsFalse(opts.SaveTemplates);
        }

        [Test]
        public void TestArgumentTextSet()
        {
            var argumentText =
@"dedicated_server
save_templates=false
arena = foo.xml";
            var opts = new CommandLineOptions(new string[] { }, new NameValueCollection(), argumentText);
            Assert.AreEqual("foo.xml", opts.ArenaFilename);
            Assert.IsTrue(opts.DedicatedServer);
            Assert.IsFalse(opts.DeleteTemplates);
            Assert.IsTrue(opts.SaveTemplates);
        }

        [Test]
        public void TestCommandLineAndArgumentTextSet()
        {
            var argumentText =
@"arena  =  foo.xml
dedicated_server=
save_templates  ";
            var opts = new CommandLineOptions(new string[] { "--arena", "bar.xml", "--dedicated_server", "--delete_templates" }, new NameValueCollection(), argumentText);
            Assert.AreEqual("bar.xml", opts.ArenaFilename);
            Assert.IsTrue(opts.DedicatedServer);
            Assert.IsTrue(opts.DeleteTemplates);
            Assert.IsTrue(opts.SaveTemplates);
        }
    }
}
