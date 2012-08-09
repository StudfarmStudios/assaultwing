using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace AW2.Helpers
{
    [TestFixture]
    public class ClonableTest
    {
        private IEnumerable<Type> ClonableSubclasses
        {
            get
            {
                return
                    from type in Assembly.GetAssembly(typeof(Clonable)).GetTypes()
                    where typeof(Clonable).IsAssignableFrom(type) && !type.IsAbstract
                    select type;
            }
        }

        [Test]
        public void TestSubclassConstructorsExist()
        {
            var constructorFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
            var canonicalStringArray = new Type[] { typeof(CanonicalString) };
            foreach (var type in ClonableSubclasses)
            {
                Assert.NotNull(type.GetConstructor(constructorFlags, null, Type.EmptyTypes, null), "Parameterless constructor missing from " + type.Name);
                Assert.NotNull(type.GetConstructor(constructorFlags, null, canonicalStringArray, null), "Constructor taking CanonicalString missing from " + type.Name);
            }
        }
    }
}
