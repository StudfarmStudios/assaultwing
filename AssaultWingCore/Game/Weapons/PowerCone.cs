using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Deals damage across a conic area and around the shooter.
    /// </summary>
    public class PowerCone : Weapon
    {
        /// <summary>
        /// Only for serialization.
        /// </summary>
        public PowerCone()
        {
        }

        public PowerCone(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            ForEachShipBarrel(ShipBarrelTypes.Middle, CreateShot);
        }

        private void CreateShot(int barrelBoneIndex, float barrelRotation)
        {
            var birthPos = Owner.GetNamedPosition(barrelBoneIndex);
            Gob.CreateGob<Triforce>(Owner.Game, _shotTypeName, shot =>
            {
                shot.ResetPos(birthPos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                shot.Owner = PlayerOwner;
                shot.Host = Owner;
                Arena.Gobs.Add(shot);
            });
        }
    }
}
