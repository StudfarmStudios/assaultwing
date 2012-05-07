﻿using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Deals damage across a conic area and across an area surrounding the shooter.
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
            Gob.CreateGob<Triforce>(Owner.Game, _shotTypeName, shot =>
            {
                shot.ResetPos(Owner.Pos, Vector2.Zero, Owner.Rotation);
                shot.Owner = PlayerOwner;
                shot.Host = Owner;
                Arena.Gobs.Add(shot);
            });
        }
    }
}
