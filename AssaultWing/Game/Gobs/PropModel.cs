using System;
using System.Collections.Generic;
using System.Text;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A prop represented by a 3D model.
    /// </summary>
    /// Props are only for the looks. They don't participate in gameplay.
    class PropModel : Gob
    {
        /// <summary>
        /// The name of the 3D model to draw the prop with.
        /// </summary>
        /// Note: This field overrides the type parameter Gob.modelName.
        [RuntimeState]
        string propModelName;

        /// <summary>
        /// Names of all models that this gob type will ever use.
        /// </summary>
        public override List<string> ModelNames
        {
            get
            {
                List<string> names = base.ModelNames;
                names.Add(propModelName);
                return names;
            }
        }

        /// <summary>
        /// Creates an uninitialised prop.
        /// </summary>
        /// This constructor is only for serialisation.
        public PropModel() : base() 
        {
            propModelName = "dummymodel";
        }

        /// <summary>
        /// Creates a prop.
        /// </summary>
        /// <param name="typeName">The type of the prop.</param>
        public PropModel(string typeName)
            : base(typeName)
        {
            base.ModelName = propModelName;
        }

        #region Methods related to serialisation

        /// <summary>
        /// Copies the gob's runtime state from another gob.
        /// </summary>
        /// <param name="runtimeState">The gob whose runtime state to imitate.</param>
        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);
            base.ModelName = propModelName;
        }

        /// <summary>
        /// Serialises the gob for to a binary writer.
        /// </summary>
        /// <param name="writer">The writer where to write the serialised data.</param>
        /// <param name="mode">Which parts of the gob to serialise.</param>
        public override void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((string)propModelName, 32, true);
            }
        }

        /// <summary>
        /// Deserialises the gob from a binary writer.
        /// </summary>
        /// <param name="reader">The reader where to read the serialised data.</param>
        /// <param name="mode">Which parts of the gob to deserialise.</param>
        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode)
        {
            base.Deserialize(reader, mode);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                propModelName = reader.ReadString(32);
                base.ModelName = propModelName;
            }
        }

        #endregion Methods related to serialisation

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public override void MakeConsistent(Type limitationAttribute)
        {
            base.MakeConsistent(limitationAttribute);
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                // Make sure there's no null references.

                // 'wallModelName' is actually part of our runtime state,
                // but its value is passed onwards by 'ModelNames' even
                // if we were only a gob template. The real problem is
                // that we don't make a difference between gob templates
                // and actual gob instances (that have a proper runtime state).
                if (propModelName == null)
                    propModelName = "dummymodel";
            }
            if (limitationAttribute == typeof(RuntimeStateAttribute))
            {
                // Make sure there's no null references.
                if (propModelName == null)
                    propModelName = "dummymodel";
            }
        }

        #endregion IConsistencyCheckable Members
    }
}