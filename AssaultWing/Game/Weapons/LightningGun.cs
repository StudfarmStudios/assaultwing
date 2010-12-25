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
            FireAtTarget(TargetSelection.ChooseTarget(Arena.Gobs.GameplayLayer.Gobs, Owner, _range));
        }

        protected override void CreateVisualsImpl()
        {
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
