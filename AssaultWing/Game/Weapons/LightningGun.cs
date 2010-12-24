using System;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using System.Collections.Generic;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// A weapon that shoots auto-targeting lightnings.
    /// </summary>
    public class LightningGun : Weapon
    {
        /// <summary>
        /// Maximum distance the lightning can carry, in meters.
        /// </summary>
        [TypeParameter]
        private float _range;

        /// This constructor is only for serialisation.
        public LightningGun()
        {
            _range = 500;
        }

        public LightningGun(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            FireAtTarget(ChooseTarget(Arena.Gobs.GameplayLayer.Gobs));
        }

        protected override void CreateVisualsImpl()
        {
        }

        private Gob ChooseTarget(IEnumerable<Gob> candidates)
        {
            // Prefer targets that are
            // 1. enemies or at least not friends, and
            // 2. straight ahead, and
            // 3. close
            var targets =
                from gob in candidates
                where gob.IsDamageable && !gob.Disabled && gob != Owner
                let ownerWeight = gob.Owner == Owner.Owner ? 5f : gob.Owner == null ? 1f : 0.5f
                let relativePos = (gob.Pos - Owner.Pos).Rotate(-Owner.Rotation)
                let distanceSquared = relativePos.LengthSquared()
                where distanceSquared <= _range * _range && relativePos.X >= 0
                orderby ownerWeight * (relativePos.X + 5 * Math.Abs(relativePos.Y)) ascending
                select gob;
            return targets.FirstOrDefault();
        }

        private void FireAtTarget(Gob target)
        {
            ForEachShipBarrel(ShipBarrelTypes.Middle, (index, rotation) => CreateShot(target, index));
        }

        private void CreateShot(Gob target, int boneIndex)
        {
            Gob.CreateGob<Lightning>(Owner.Game, shotTypeName, shot =>
            {
                shot.Owner = Owner.Owner;
                shot.ResetPos(Owner.GetNamedPosition(boneIndex), Vector2.Zero, Owner.Rotation);
                shot.Shooter = new GobProxy(Owner);
                shot.ShooterBoneIndex = boneIndex;
                shot.Target = new GobProxy(target);
                Arena.Gobs.Add(shot);
            });
        }
    }
}
