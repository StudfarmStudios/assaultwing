using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using AW2.Game;
using AW2.Helpers.Serialization;

namespace AW2.Helpers
{
    [TestFixture]
    public class ClonableTest
    {
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

        [Test]
        public void TestSubclassesCloneTypeParameters()
        {
            CanonicalString.IsForLocalUseOnly = true;
            var templates = LogicEngine.LoadTemplates()
                .OfType<Clonable>()
                .GroupBy(x => x.GetType())
                .ToDictionary(group => group.Key, group => group.First());
            foreach (var type in ClonableSubclasses)
            {
                Clonable template;
                if (!templates.TryGetValue(type, out template)) continue; // No need to test types that don't have templates
                var clone = template.Clone();
                foreach (var field in GetTypeParameters(type))
                    Assert.That(Serialization.Serialization.DeepEquals(field.GetValue(template), field.GetValue(clone)),
                        type.FullName + "." + field.Name + " wasn't cloned: " +
                        field.GetValue(template) + " != " + field.GetValue(clone));
            }
        }

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

        private IEnumerable<FieldInfo> GetTypeParameters(Type type)
        {
            return Serialization.Serialization.GetDeclaredFields(type, typeof(Serialization.TypeParameterAttribute), null);
        }

        private IEnumerable<FieldInfo> GetRuntimeState(Type type)
        {
            return Serialization.Serialization.GetDeclaredFields(type, typeof(Serialization.RuntimeStateAttribute), null);
        }
    }
}
