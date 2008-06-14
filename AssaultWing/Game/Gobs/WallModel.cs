using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;

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
        #region WallModel Fields

        /// <summary>
        /// The name of the 3D model to draw the wall with.
        /// </summary>
        /// Note: This field overrides the type parameter Gob.modelName.
        [RuntimeState]
        string wallModelName;

        #endregion // WallModel Fields

        #region WallModel Properties

        /// <summary>
        /// Names of all models that this gob type will ever use.
        /// </summary>
        public override List<string> ModelNames
        {
            get
            {
                List<string> names = base.ModelNames;
                names.Add(wallModelName);
                return names;
            }
        }

        /// <summary>
        /// Names of all textures that this gob type will ever use.
        /// </summary>
        public override List<string> TextureNames
        {
            get
            {
                return base.TextureNames;
            }
        }

        #endregion // WallModel Properties

        /// <summary>
        /// Creates an uninitialised piece of wall.
        /// </summary>
        /// This constructor is only for serialisation.
        public WallModel() : base() 
        {
            wallModelName = "dummymodel";
        }

        /// <summary>
        /// Creates a piece of wall.
        /// </summary>
        /// <param name="typeName">The type of the wall.</param>
        public WallModel(string typeName)
            : base(typeName)
        {
            this.wallModelName = "dummymodel";
        }

        /// <summary>
        /// Copies the gob's runtime state from another gob.
        /// </summary>
        /// <param name="runtimeState">The gob whose runtime state to imitate.</param>
        protected override void SetRuntimeState(Gob runtimeState)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            base.SetRuntimeState(runtimeState);

            // Recover wall data from its 3D model, overwriting what
            // Wall.SetRuntimeState erroneously set before.
            Model model = data.GetModel(wallModelName);
            VertexPositionNormalTexture[] vertexData;
            short[] indexData;
            Graphics3D.GetModelData(model, out vertexData, out indexData);
            Matrix worldMatrix = WorldMatrix;
            for (int i = 0; i < vertexData.Length; ++i)
            {
                vertexData[i].Position = Vector3.Transform(vertexData[i].Position, worldMatrix);
                vertexData[i].Normal = Vector3.TransformNormal(vertexData[i].Normal, worldMatrix);
            }
            if (model.Meshes.Count > 1)
                throw new ArgumentOutOfRangeException("Wall model has more than one mesh");
            if (model.Meshes[0].Effects.Count > 1)
                throw new ArgumentOutOfRangeException("Wall model mesh has more than one effect");
            BasicEffect effect = model.Meshes[0].Effects[0] as BasicEffect;
            if (effect == null)
                throw new ArgumentException("Wall model mesh's effect isn't a BasicEffect");
            Set3DModel(vertexData, indexData, effect.Texture, effect);
        }
    }
}
