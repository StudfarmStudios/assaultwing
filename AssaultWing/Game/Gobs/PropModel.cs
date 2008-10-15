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

        /// <summary>
        /// Copies the gob's runtime state from another gob.
        /// </summary>
        /// <param name="runtimeState">The gob whose runtime state to imitate.</param>
        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);
            base.ModelName = propModelName;
        }
    }
}
