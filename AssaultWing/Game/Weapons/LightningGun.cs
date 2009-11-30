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

        public LightningGun(CanonicalString typeName)
            : base(typeName)
        {
        }

        #region Weapon methods

        public override void Fire()
        {
            if (!CanFire) return;
            var target = Arena.Gobs.FirstOrDefault(gob =>
            {
                if (!gob.IsDamageable) return false;
                if (gob == owner) return false;
                if (Vector2.DistanceSquared(gob.Pos, owner.Pos) > range * range) return false;
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
                    shot.Owner = owner.Owner;
                    shot.ResetPos(owner.GetNamedPosition(boneI), Vector2.Zero, owner.Rotation);
                    var lightning = shot as Lightning;
                    if (lightning != null)
                    {
                        lightning.Shooter = owner;
                        lightning.ShooterBoneIndex = boneI;
                        lightning.Target = target;
                    }
                    Arena.Gobs.Add(shot);
                });
            }

            DoneFiring();
        }

        public override void Activate()
        {
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
