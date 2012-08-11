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
    /// A piece of wall initialised from a 3D model.
    /// </summary>
    /// Note that a wall has no position or movement like other gobs have. 
    /// Instead, a wall acts like a polygon. For visual purposes, walls have 
    /// also a third dimension.
    /// <see cref="AW2.Game.Gobs.Wall"/>
    public class WallModel : Wall
    {
        /// <summary>
        /// The name of the 3D model to draw the wall with. Overrides <see cref="Gob._modelName"/>.
        /// </summary>
        [RuntimeState]
        private CanonicalString _wallModelName;

        public override IEnumerable<CanonicalString> ModelNames
        {
            get { return base.ModelNames.Union(new CanonicalString[] { _wallModelName }); }
        }

        /// <summary>
        /// Only for serialisation.
        /// </summary>
        public WallModel()
        {
            _wallModelName = (CanonicalString)"dummymodel";
        }

        public WallModel(CanonicalString typeName)
            : base(typeName)
        {
        }

        #region Methods related to gobs' functionality in the game world

        public override void LoadContent()
        {
            base.LoadContent();
            Set3DModel();
        }

        public override void Activate()
        {
            if (!Arena.IsForPlaying) ModelName = _wallModelName;
            base.Activate();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.Serialize(writer, mode);
                if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
                {
                    writer.Write((CanonicalString)_wallModelName);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantDataFromServer) != 0)
            {
                _wallModelName = reader.ReadCanonicalString();
                var model = Game.Content.Load<Model>(_wallModelName);
                Effect = GetEffect(model);
                Texture = GetTexture(model);
            }
        }

        public override void Cloned()
        {
            _wallModelName = ModelName;
            base.Cloned();
        }

        #endregion Methods related to serialisation

        /// <summary>
        /// Sets the wall's 3D model based on '_wallModelName'.
        /// </summary>
        private void Set3DModel()
        {
            var data = Game.Content.Load<ModelGeometry>(_wallModelName);
            if (data.Meshes.Length != 1) throw new ApplicationException("WallModel only supports one Mesh");
            var mesh = data.Meshes[0];
            if (mesh.MeshParts.Length != 1) throw new ApplicationException("WallModel only supports one MeshPart");
            var mehsPartToworldMatrix = Matrix.Identity;
            for (var bone = mesh.ParentBone; bone != null; bone = bone.Parent) mehsPartToworldMatrix *= bone.Transform;
            var meshPart = mesh.MeshParts[0];
            var vertices = meshPart.VertexBuffer.Vertices
                .Select(vertex => new VertexPositionNormalTexture(
                    position: Vector3.Transform(vertex.Position, mehsPartToworldMatrix),
                    normal: Vector3.TransformNormal(vertex.Normal, mehsPartToworldMatrix),
                    textureCoordinate: vertex.TextureCoordinate))
                .ToArray();
            var effect = Game.CommandLineOptions.DedicatedServer ? null : GetEffect(Game.Content.Load<Model>(_wallModelName));
            var texture = effect == null ? null : effect.Texture;
            var indices = new short[meshPart.PrimitiveCount * 3];
            Array.Copy(meshPart.IndexBuffer.Indices, meshPart.StartIndex, indices, 0, indices.Length);
            Set3DModel(vertices, indices, texture, effect);
        }

        private static BasicEffect GetEffect(Model model)
        {
            if (model.Meshes.Count > 1)
                throw new ArgumentOutOfRangeException("Model has more than one mesh");
            if (model.Meshes[0].Effects.Count > 1)
                throw new ArgumentOutOfRangeException("Model mesh has more than one effect");
            var effect = model.Meshes[0].Effects[0] as BasicEffect;
            if (effect == null)
                throw new ArgumentException("Model mesh's effect isn't a BasicEffect");
            return effect;
        }

        private static Texture2D GetTexture(Model model)
        {
            return GetEffect(model).Texture;
        }
    }
}
