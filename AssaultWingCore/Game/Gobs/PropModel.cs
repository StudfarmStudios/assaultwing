using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Graphics.Content;
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

        private Vector2 _drawBoundsMin;
        private Vector2 _drawBoundsMax;

        public override bool IsRelevant { get { return false; } }

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

        public override void Activate()
        {
            base.Activate();
            ComputeDrawBounds();
        }

        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);
            ModelName = _propModelName;
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.Serialize(writer, mode);
                if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer))
                {
                    writer.Write((CanonicalString)_propModelName);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer))
            {
                _propModelName = reader.ReadCanonicalString();
                ModelName = _propModelName;
            }
        }

        public override void GetDraw3DBounds(out Vector2 min, out Vector2 max)
        {
            min = _drawBoundsMin;
            max = _drawBoundsMax;
        }

        private void ComputeDrawBounds()
        {
            var modelGeometry = Game.Content.Load<ModelGeometry>(_propModelName);
            VertexPositionNormalTexture[] vertexData;
            short[] indexData;
            Graphics3D.GetVertexAndIndexData(modelGeometry, out vertexData, out indexData);
            var gobVertices = vertexData.Select(AWMathHelper.ProjectXY).ToArray();
            var worldMatrix = WorldMatrix;
            Vector2.Transform(gobVertices, ref worldMatrix, gobVertices);
            var boundingBox = gobVertices.GetBoundingBox();
            _drawBoundsMin = boundingBox.Min;
            _drawBoundsMax = boundingBox.Max;
        }
    }
}
