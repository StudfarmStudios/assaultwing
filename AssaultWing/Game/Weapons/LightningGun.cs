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
        private float range;

        /// This constructor is only for serialisation.
        public LightningGun()
        {
            range = 500;
        }

        public LightningGun(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            var targets =
                from gob in Arena.Gobs.GameplayLayer.Gobs
                where gob.IsDamageable && !gob.Disabled && gob != Owner
                let distanceSquared = Vector2.DistanceSquared(gob.Pos, Owner.Pos)
                where distanceSquared <= range * range
                orderby distanceSquared ascending
                select gob;
            FireAtTarget(targets.FirstOrDefault());
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
