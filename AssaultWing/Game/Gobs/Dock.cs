using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A docking platform.
    /// </summary>
    public class Dock : Gob, IThick
    {
        #region Dock fields

        /// <summary>
        /// Speed of repairing damageable gobs, measured in damage/second.
        /// Use a negative value for repairing, positive for damaging.
        /// </summary>
        [TypeParameter]
        float repairSpeed;

        /// <summary>
        /// Index of the general collision area in <b>base.collisionAreas</b>.
        /// </summary>
        int generalAreaI;

        #endregion Dock fields

        /// <summary>
        /// Creates an uninitialised dock.
        /// </summary>
        /// This constructor is only for serialisation.
        public Dock()
            : base()
        {
            this.repairSpeed = -10;
        }

        /// <summary>
        /// Creates a dock.
        /// </summary>
        /// <param name="typeName">The type of the dock.</param>
        public Dock(string typeName)
            : base(typeName)
        {
            this.physicsApplyMode = PhysicsApplyMode.None;

            // Find our general collision area.
            this.generalAreaI = -1;
            for (int i = 0; i < collisionAreas.Length; ++i)
                if (collisionAreas[i].Name == "General")
                {
                    this.generalAreaI = i;
                    break;
                }
            if (this.generalAreaI == -1)
                Log.Write("Warning: Dock couldn't find its general collision area");
        }

        #region ICollidable Members
        // Some members are implemented in class Gob.

        /// <summary>
        /// Performs collision operations with a gob whose general collision area
        /// has collided with one of our receptor areas.
        /// </summary>
        /// <param name="gob">The gob we collided with.</param>
        /// <param name="receptorName">The name of our colliding receptor area.</param>
        public override void Collide(ICollidable gob, string receptorName)
        {
            IDamageable damaGob = gob as IDamageable;
            if (receptorName == "Dock" && damaGob != null)
            {
                damaGob.InflictDamage(physics.ApplyChange(repairSpeed));
            }
        }

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
            if (generalAreaI != -1)
                return Geometry.GetNormal((Polygon)base.collisionAreas[generalAreaI].Area, new AW2.Helpers.Point(pos));
            else
                return Vector2.UnitY;
        }

        /// <summary>
        /// Removes an area from the thick gob. 
        /// </summary>
        /// <param name="area">The area to remove. The polygon must be convex.</param>
        public void MakeHole(Polygon area)
        {
            // TODO: Replace MakeHole from IThick into some new interface?
        }

        #endregion IThick Members
    }
}
