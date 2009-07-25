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
    public class Dock : Gob
    {
        #region Dock fields

        /// <summary>
        /// Speed of repairing damageable gobs, measured in damage/second.
        /// Use a negative value for repairing, positive for damaging.
        /// </summary>
        [TypeParameter]
        float repairSpeed;

        /// <summary>
        /// Speed of charging primary weapons of ships, measured in charge/second.
        /// </summary>
        [TypeParameter]
        float weapon1ChargeSpeed;

        /// <summary>
        /// Speed of charging secondary weapons of ships, measured in charge/second.
        /// </summary>
        [TypeParameter]
        float weapon2ChargeSpeed;

        #endregion Dock fields

        /// <summary>
        /// Creates an uninitialised dock.
        /// </summary>
        /// This constructor is only for serialisation.
        public Dock()
            : base()
        {
            this.repairSpeed = -10;
            this.weapon1ChargeSpeed = 100;
            this.weapon2ChargeSpeed = 100;
        }

        /// <summary>
        /// Creates a dock.
        /// </summary>
        /// <param name="typeName">The type of the dock.</param>
        public Dock(CanonicalString typeName)
            : base(typeName)
        {
            movable = false;
        }

        /// <summary>
        /// Performs collision operations for the case when one of this gob's collision areas
        /// is overlapping one of another gob's collision areas.
        /// </summary>
        /// <param name="myArea">The collision area of this gob.</param>
        /// <param name="theirArea">The collision area of the other gob.</param>
        /// <param name="stuck">If <b>true</b> then the gob is stuck, i.e.
        /// <b>theirArea.Type</b> matches <b>myArea.CannotOverlap</b> and it's not possible
        /// to backtrack out of the overlap. It is then up to this gob and the other gob 
        /// to resolve the overlap.</param>
        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            // We assume we have only one Receptor collision area which handles docking.
            // Then 'theirArea.Owner' must be damageable.
            if (myArea.Name == "Dock")
            {
                theirArea.Owner.InflictDamage(AssaultWing.Instance.PhysicsEngine.ApplyChange(repairSpeed), new DeathCause());
                Ship ship = theirArea.Owner as Ship;
                if (ship != null)
                {
                    ship.Weapon1Charge += AssaultWing.Instance.PhysicsEngine.ApplyChange(weapon1ChargeSpeed);
                    ship.Weapon2Charge += AssaultWing.Instance.PhysicsEngine.ApplyChange(weapon2ChargeSpeed);
                }
            }
        }
    }
}
