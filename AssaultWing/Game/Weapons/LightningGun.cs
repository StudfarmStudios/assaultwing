using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Game.Gobs;
using AW2.Helpers;
using Microsoft.Xna.Framework;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// A weapon that shoots auto-targeting lightnings.
    /// </summary>
    class LightningGun : Weapon
    {
        /// <summary>
        /// Maximum distance the lightning can carry, in meters.
        /// </summary>
        [TypeParameter]
        float range;

        /// This constructor is only for serialisation.
        public LightningGun()
            : base()
        {
            range = 500;
        }

        /// <param name="typeName">The type of the weapon.</param>
        /// <param name="owner">The ship that owns this weapon.</param>
        /// <param name="ownerHandle">A handle for identifying the weapon at the owner.
        /// Use <b>1</b> for primary weapons and <b>2</b> for secondary weapons.</param>
        /// <param name="boneIndices">Indices of the bones that define the weapon's
        /// barrels' locations on the owning ship.</param>
        public LightningGun(CanonicalString typeName, Ship owner, int ownerHandle, int[] boneIndices)
            : base(typeName, owner, ownerHandle, boneIndices)
        {
        }

        #region Weapon methods

        public override void Fire()
        {
            if (!CanFire) return;
            var target = Arena.Gobs.FirstOrDefault(gob =>
            {
                if (!gob.IsDamageable) return false;
                if (gob == Owner) return false;
                if (Vector2.DistanceSquared(gob.Pos, Owner.Pos) > range * range) return false;
                return true;
            });
            if (target == null) return; // nothing to shoot at

            StartFiring();

            // Every gun barrel shoots.
            for (int barrel = 0; barrel < boneIndices.Length; ++barrel)
            {
                int boneI = boneIndices[barrel];
                Gob.CreateGob(shotTypeName, shot =>
                {
                    shot.Owner = Owner.Owner;
                    shot.ResetPos(Owner.GetNamedPosition(boneI), Vector2.Zero, Owner.Rotation);
                    var lightning = shot as Lightning;
                    if (lightning != null)
                    {
                        lightning.Shooter = Owner;
                        lightning.ShooterBoneIndex = boneI;
                        lightning.Target = target;
                    }
                    Arena.Gobs.Add(shot);
                });
            }

            DoneFiring();
        }

        public override void Update()
        {
        }

        public override void Dispose()
        {
        }

        #endregion
    }
}
