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

        /// <summary>
        /// The sound to play when firing.
        /// </summary>
        [TypeParameter]
        string fireSound;

        /// This constructor is only for serialisation.
        public LightningGun()
            : base()
        {
            range = 500;
            fireSound = "dummysound";
        }

        public LightningGun(CanonicalString typeName)
            : base(typeName)
        {
        }

        #region Weapon methods

        protected override void FireImpl(AW2.UI.ControlState triggerState)
        {
            if (!triggerState.pulse) return;
            if (!CanFire) return;
            var targets =
                from gob in Arena.Gobs.GameplayLayer.Gobs
                where gob.IsDamageable && !gob.Disabled && gob != owner
                let distanceSquared = Vector2.DistanceSquared(gob.Pos, owner.Pos)
                where distanceSquared <= range * range
                orderby distanceSquared ascending
                select gob;
            StartFiring();
            FireAtTarget(targets.FirstOrDefault());
            DoneFiring();
        }

        private void FireAtTarget(Gob target)
        {
            AssaultWing.Instance.SoundEngine.PlaySound(fireSound);
            ForEachShipBarrel(ShipBarrelTypes.Middle, (index, rotation) => CreateShot(target, index));
        }

        public override void Activate()
        {
            FireMode = FireModeType.Single;
        }
        
        public override void Update()
        {
        }

        public override void Dispose()
        {
        }

        #endregion

        private void CreateShot(Gob target, int boneIndex)
        {
            Gob.CreateGob(shotTypeName, shot =>
            {
                shot.Owner = owner.Owner;
                shot.ResetPos(owner.GetNamedPosition(boneIndex), Vector2.Zero, owner.Rotation);
                var lightning = shot as Lightning;
                if (lightning != null)
                {
                    lightning.Shooter = new GobProxy(owner);
                    lightning.ShooterBoneIndex = boneIndex;
                    lightning.Target = new GobProxy(target);
                }
                Arena.Gobs.Add(shot);
            });
        }
    }
}
