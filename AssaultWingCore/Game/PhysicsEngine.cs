using System;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Helpers;

namespace AW2.Game
{
    /// <summary>
    /// A physics engine.
    /// </summary>
    public class PhysicsEngine : AWGameComponent
    {
        public PhysicsEngine(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        /// <summary>
        /// Applies drag to a gob. Drag is a force that manipulates the gob's
        /// movement closer to that of the medium. Drag constant measures the
        /// amount of this manipulation, 0 meaning no drag and 1 meaning
        /// absolute drag where the gob cannot escape the flow of the medium.
        /// Practical values are very small, under 0.1.
        /// </summary>
        /// Drag is the force that resists movement in a medium.
        /// <param name="gob">The gob to apply drag to.</param>
        /// <param name="flow">Direction and speed of flow of medium
        /// at the gob's location.</param>
        /// <param name="drag">Drag constant for the medium and the gob.</param>
        public void ApplyDrag(Gob gob, Vector2 flow, float drag)
        {
            gob.Move = (1 - drag) * (gob.Move - flow) + flow;
        }
    }
}
