using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace AW2.Helpers
{
    /// <summary>
    /// A clonable entity. Supports cloning in respect of the <see cref="TypeParameterAttribute"/>
    /// limitation attribute.
    /// </summary>
    /// Subclasses only need to mark their fields with <see cref="TypeParameterAttribute"/> to
    /// support cloning.
    public abstract class Clonable
    {
        [TypeParameter, RuntimeState]
        CanonicalString typeName;

        delegate void CloneDelegate(Clonable clone);
        delegate Clonable ConstructorDelegate();
        static Dictionary<Type, DynamicMethod> cloneMethods = new Dictionary<Type, DynamicMethod>();
        static Dictionary<Type, DynamicMethod> constructors = new Dictionary<Type, DynamicMethod>();
        List<CloneDelegate> cloneDelegates;
        ConstructorDelegate constructorDelegate;

        public CanonicalString TypeName { get { return typeName; } }

        static Clonable()
        {
            Log.Write("Checking Clonable subclass constructors...");
            foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!typeof(Clonable).IsAssignableFrom(type)) continue;
                if (type.IsAbstract) continue;
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
                if (null == type.GetConstructor(flags, null, Type.EmptyTypes, null))
                {
                    string message = "BUG: Missing constructor " + type.Name + "()";
                    Log.Write(message);
                    throw new ApplicationException(message);
                }
                if (null == type.GetConstructor(flags, null, new Type[] { typeof(CanonicalString) }, null))
                {
                    string message = "BUG: Missing constructor " + type.Name + "(CanonicalString)";
                    Log.Write(message);
                    throw new ApplicationException(message);
                }
            }
            Log.Write("...Clonable subclass constructors are OK");

            CreateCloneMethods();
        }

        /// <summary>
        /// For serialization only.
        /// </summary>
        /// In their parameterless constructors, subclasses should initialise
        /// all their fields marked with any limitation attribute, setting their
        /// values to representative defaults for XML templates.
        public Clonable()
        {
            typeName = (CanonicalString)"unknown type";
        }

        /// <summary>
        /// Creates an instance of the specified type. Common game logic should call
        /// <see cref="CreateGob(string, Action&lt;Gob&gt;)"/> instead of this method
        /// to create gobs.
        /// </summary>
        public static Clonable Instantiate(CanonicalString typeName)
        {
            var template = (Clonable)AssaultWing.Instance.DataEngine.GetTypeTemplate(typeName);
            return template.Clone();
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// The object's serialised fields are initialised according to the template instance
        /// associated with the specified type. This applies also to fields declared
        /// in subclasses, so a subclass constructor must not initialise its fields
        /// marked with <see cref="TypeDefinitionAttribute"/>.
        protected Clonable(CanonicalString typeName)
        {
            this.typeName = typeName;
        }

        /// <summary>
        /// Called on a cloned object after the cloning.
        /// </summary>
        public virtual void Cloned() { }

        /// <summary>
        /// Returns a clone of the instance.
        /// </summary>
        public Clonable Clone()
        {
            if (cloneDelegates == null)
            {
                cloneDelegates = new List<CloneDelegate>();
                for (var klass = GetType(); klass != null && cloneMethods.ContainsKey(klass); klass = klass.BaseType)
                    cloneDelegates.Add((CloneDelegate)cloneMethods[klass].CreateDelegate(typeof(CloneDelegate), this));
            }
            if (constructorDelegate == null)
                constructorDelegate = (ConstructorDelegate)constructors[GetType()].CreateDelegate(typeof(ConstructorDelegate), this);
            var clone = constructorDelegate();
            for (int i = 0; i < cloneDelegates.Count; ++i) cloneDelegates[i](clone);
            clone.Cloned();
            return clone;
        }

        static void CreateCloneMethods()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!typeof(Clonable).IsAssignableFrom(type) || type == typeof(Clonable)) continue;
                cloneMethods[type] = CreateCloneMethod(type);
                constructors[type] = CreateConstructor(type);
            }
        }

        static DynamicMethod CreateConstructor(Type type)
        {
            Type returnType = typeof(Clonable);
            Type[] parameterTypes = { typeof(Clonable) };
            var dyna = new DynamicMethod("ConstructorInvoker", returnType, parameterTypes, typeof(Clonable));
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var constructor = type.GetConstructor(flags, null, new Type[] { typeof(CanonicalString) }, null);
            var gob_GetTypeName = typeof(AW2.Game.Gob).GetProperty("TypeName").GetGetMethod(); // HACK: Reference to Gob
            var generator = dyna.GetILGenerator();

            // HACK: get value of ((Gob)this).TypeName
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, gob_GetTypeName);

            // call constructor and return its return value
            generator.Emit(OpCodes.Newobj, constructor);
            generator.Emit(OpCodes.Ret);
            return dyna;
        }

        static DynamicMethod CreateCloneMethod(Type type)
        {
            Type returnType = null;
            Type[] parameterTypes = { typeof(Clonable), typeof(Clonable) };
            var dyna = new DynamicMethod("Clone", returnType, parameterTypes, type);
            var generator = dyna.GetILGenerator();
            var deepCopyMethod = typeof(Serialization).GetMethod("DeepCopy");

            // copy all fields from this to parameter
            foreach (var field in Serialization.GetDeclaredFields(type, typeof(TypeParameterAttribute)))
            {
                if (field.FieldType.IsValueType || field.IsDefined(typeof(ShallowCopyAttribute), false))
                {
                    // shallow copy - copy value by simple assignment
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, field);
                    generator.Emit(OpCodes.Stfld, field);
                }
                else
                {
                    // deep copy - call special method for obtaining a deep copy
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, field);
                    generator.Emit(OpCodes.Call, deepCopyMethod);
                    generator.Emit(OpCodes.Stfld, field);
                }
            }
            generator.Emit(OpCodes.Ret);
            return dyna;
        }
    }
}
