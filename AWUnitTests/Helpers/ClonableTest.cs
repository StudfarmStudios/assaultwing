using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using AW2.Game;
using AW2.Helpers.Geometric;
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
            AssertClone(x => x.Clone(), GetTypeParameters);
        }

        [Test]
        public void TestSubclassesCloneRuntimeState()
        {
            AssertClone(x => x.CloneWithRuntimeState(), GetRuntimeState);
        }

        private void AssertClone(Func<Clonable, Clonable> getClone, Func<Type, IEnumerable<FieldInfo>> getFields)
        {
            CanonicalString.IsForLocalUseOnly = true;
            foreach (var type in ClonableSubclasses)
            {
                Clonable template;
                if (!Templates.TryGetValue(type, out template)) continue; // No need to test types that don't have templates
                var clone = getClone(template);
                foreach (var field in getFields(type))
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

        private static Dictionary<Type, Clonable> g_templates;
        private static Dictionary<Type, Clonable> Templates
        {
            get
            {
                if (g_templates == null) g_templates = LogicEngine.LoadTemplates()
                   .OfType<Clonable>()
                   .GroupBy(x => x.GetType())
                   .ToDictionary(group => group.Key, group => Mock(group.First(), typeof(RuntimeStateAttribute)));
                return g_templates;
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

        /// <summary>
        /// Returns <paramref name="obj"/>, modified with mock values in its fields marked
        /// with <paramref name="limitationAttribute"/>.
        /// </summary>
        private static T Mock<T>(T obj, Type limitationAttribute)
        {
            foreach (var field in Serialization.Serialization.GetFields(obj.GetType(), limitationAttribute, null))
                field.SetValue(obj, GetMockValue(field.FieldType, limitationAttribute));
            return obj;
        }

        private static object GetMockValue(Type type, Type limitationAttribute)
        {
            if (type == typeof(bool)) return true;
            if (type == typeof(int)) return 42;
            if (type == typeof(float)) return 123.456f;
            if (type == typeof(string)) return "foobar";
            if (type == typeof(IGeomPrimitive)) return new Circle(new Vector2(42, 69), 99);
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var array = (Array)Activator.CreateInstance(type, 1);
                array.SetValue(GetMockValue(elementType, limitationAttribute), 0);
                return array;
            }
            try
            {
                return Mock(Activator.CreateInstance(type), limitationAttribute);
            }
            catch (MissingMethodException)
            {
                throw new NotImplementedException("Mock value of type " + type.FullName);
            }
            throw new NotImplementedException("Mock value of type " + type.FullName);
        }
    }
}
