using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A prop represented by a 3D model.
    /// Props are only for the looks. They don't participate in gameplay.
    /// </summary>
    public class PropModel : Gob
    {
        /// <summary>
        /// The name of the 3D model to draw the prop with.
        /// This field overrides the type parameter <see cref="Gob._modelName"/>.
        /// </summary>
        [RuntimeState]
        private CanonicalString _propModelName;

        public override IEnumerable<CanonicalString> ModelNames
        {
            get { return base.ModelNames.Union(new[] { _propModelName }); }
        }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public PropModel()
        {
            _propModelName = (CanonicalString)"dummymodel";
        }

        public PropModel(CanonicalString typeName)
            : base(typeName)
        {
            ModelName = _propModelName;
            Gravitating = false;
        }

        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);
            base.ModelName = _propModelName;
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.Serialize(writer, mode);
                if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
                {
                    writer.Write((CanonicalString)_propModelName);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
                _propModelName = reader.ReadCanonicalString();
                ModelName = _propModelName;
            }
        }
    }
}