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
    public class WallModel : Gob, IThick
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
            base.physicsApplyMode = PhysicsApplyMode.None;
        }

        #region Methods related to gobs' functionality in the game world

        #endregion Methods related to gobs' functionality in the game world

        /// <summary>
        /// Copies the gob's runtime state from another gob.
        /// </summary>
        /// <param name="runtimeState">The gob whose runtime state to imitate.</param>
        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);
            base.ModelName = wallModelName;

            // Create a collision polygon out of the 3D model.
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Model model = data.GetModel(wallModelName);
            Polygon poly = Graphics3D.GetOutline(model);
            base.collisionAreas = new CollisionArea[] {
                new CollisionArea("General", poly, this),
            };
        }

        #region ICollidable Members
        // Some members are implemented in class Gob.

        #endregion ICollidable Members

        #region IThick Members

        /// <summary>
        /// Returns the unit normal vector from the thick gob
        /// pointing towards the given location.
        /// </summary>
        /// <param name="pos">The location for the normal to point to.</param>
        /// <returns>The unit normal pointing to the given location.</returns>
        public Vector2 GetNormal(Vector2 pos)
        {
            return Helpers.Geometry.GetNormal((Polygon)(base.collisionAreas[0].Area), new Helpers.Point(pos));
        }

        /// <summary>
        /// Removes an area from the thick gob. 
        /// </summary>
        /// <param name="area">The area to remove. The polygon must be convex.</param>
        public void MakeHole(Polygon area)
        {
            // TODO
            //Helpers.Math.RemoveArea(ref vertexData, ref indexData, area);
        }

        #endregion IThick Members
    }
}
