using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A piece of wall that is defined with a polygonal outline.
    /// </summary>
    public class WallPolygon : Wall
    {
        /// <summary>
        /// The name of the texture to fill the wall with.
        /// The name indexes the static texture bank in DataEngine.
        /// </summary>
        [RuntimeState]
        private CanonicalString _textureName;

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public WallPolygon() : base()
        {
            _textureName = (CanonicalString)"dummytexture";
        }

        public WallPolygon(CanonicalString typeName)
            : base(typeName)
        {
        }

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public override void LoadContent()
        {
            Texture = Game.Content.Load<Texture2D>(_textureName);
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        /// <summary>
        /// Copies the gob's runtime state from another gob.
        /// </summary>
        /// <param name="runtimeState">The gob whose runtime state to imitate.</param>
        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);
            TriangleCount = _indexData.Length / 3;
        }

        #endregion Methods related to serialisation
    }
}
