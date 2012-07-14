using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// Deals damage across a conic area and across an area surrounding the shooter.
    /// </summary>
    public class PowerCone : Weapon
    {
        /// <summary>
        /// The ship gun barrels this weapon uses.
        /// </summary>
        [TypeParameter]
        private ShipBarrelTypes _gunBarrels;

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public PowerCone()
        {
            _gunBarrels = ShipBarrelTypes.Middle;
        }

        public PowerCone(CanonicalString typeName)
            : base(typeName)
        {
        }

        protected override void ShootImpl()
        {
            ForEachShipBarrel(_gunBarrels, CreateShot);
        }

        private void CreateShot(int boneIndex, float barrelRotation)
        {
            Gob.CreateGob<Triforce>(Owner.Game, _shotTypeName, shot =>
            {
                shot.ResetPos(Owner.Pos, Vector2.Zero, Owner.Rotation);
                shot.Owner = PlayerOwner;
                shot.Host = Owner;
                shot.HostBoneIndex = boneIndex;
                Arena.Gobs.Add(shot);
            });
        }
    }
}
