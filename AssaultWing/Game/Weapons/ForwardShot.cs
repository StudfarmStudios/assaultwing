using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Net;
using AW2.Sound;

namespace AW2.Game.Weapons
{
    /// <summary>
    /// What to do when firing a weapon.
    /// </summary>
    public enum FireAction
    {
        /// <summary>
        /// Always just produce new shots.
        /// </summary>
        Shoot,

        /// <summary>
        /// If old shots are alive, kill all of them.
        /// Otherwise produce new shots.
        /// </summary>
        KillAll,

        /// <summary>
        /// Always shoot and don't wait for reload. Uses charge per second.
        /// </summary>
        ShootContinuously,
    }

    /// <summary>
    /// A weapon that shoots gobs forward.
    /// Each firing can consist of a number of shots being fired.
    /// The shots are shot at even temporal intervals in a random
    /// angle. The shot angles distribute evenly in an angular fan
    /// whose center is directed at the direction of the weapon's owner.
    /// </summary>
    public class ForwardShot : Weapon
    {
        #region ForwardShot fields

        /// <summary>
        /// The ship gun barrels this weapon uses.
        /// </summary>
        [TypeParameter]
        private ShipBarrelTypes gunBarrels;

        /// <summary>
        /// Names of muzzle fire engine types.
        /// </summary>
        [TypeParameter]
        private CanonicalString[] muzzleFireEngineNames;

        /// <summary>
        /// How fast the shots leave the weapon barrel,
        /// in meters per second.
        /// </summary>
        [TypeParameter]
        private float shotSpeed;

        /// <summary>
        /// Difference of the maximum and the minimum of a shot's random angle
        /// relative to the general shot direction.
        /// </summary>
        [TypeParameter]
        private float shotAngleVariation;

        /// <summary>
        /// Difference of the maximum and the minimum of a shot's random speed
        /// relative to the general shot speed.
        /// </summary>
        [TypeParameter]
        private float shotSpeedVariation;

        /// <summary>
        /// What to do when firing the weapon.
        /// </summary>
        [TypeParameter]
        private FireAction fireAction;

        /// <summary>
        /// Shots produced by this weapon that are still alive.
        /// </summary>
        [RuntimeState]
        private List<Gob> liveShots;

        #endregion ForwardShot fields

        /// This constructor is only for serialisation.
        public ForwardShot()
        {
            gunBarrels = ShipBarrelTypes.Middle | ShipBarrelTypes.Left | ShipBarrelTypes.Right | ShipBarrelTypes.Rear;
            muzzleFireEngineNames = new CanonicalString[] { (CanonicalString)"dummypeng" };
            shotSpeed = 300f;
            shotAngleVariation = 0.3f;
            shotSpeedVariation = 20f;
            fireAction = FireAction.Shoot;
            liveShots = new List<Gob>();
        }

        public ForwardShot(CanonicalString typeName)
            : base(typeName)
        {
            liveShots = new List<Gob>();
        }

        public override void Activate()
        {
            switch (fireAction)
            {
                case FireAction.ShootContinuously:
                    FiringOperator = new FiringOperatorContinuous(this);
                    break;
                case FireAction.Shoot:
                case FireAction.KillAll:
                    FiringOperator = new FiringOperatorSingle(this);
                    break;
                default: throw new ApplicationException("Unknown FireAction " + fireAction);
            }
        }

        public override void Update()
        {
            base.Update();
            RemoveOldShots();
        }

        protected override bool PermissionToFire(bool canFire)
        {
            if (fireAction == FireAction.KillAll && liveShots.Count > 0)
            {
                foreach (var gob in liveShots) gob.Die(new DeathCause());
                return false;
            }
            return true;
        }

        protected override void ShootImpl()
        {
            ForEachShipBarrel(gunBarrels, CreateShot);
            ApplyRecoil();
        }

        protected override void CreateVisuals()
        {
            ForEachShipBarrel(gunBarrels, CreateMuzzleFire);
        }

        private void CreateShot(int boneIndex, float barrelRotation)
        {
            float direction = barrelRotation + owner.Rotation + shotAngleVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
            float kickSpeed = shotSpeed + shotSpeedVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
            Vector2 kick = kickSpeed * AWMathHelper.GetUnitVector2(direction);
            Gob.CreateGob<Gob>(shotTypeName, shot =>
            {
                shot.Owner = owner.Owner;
                shot.ResetPos(owner.GetNamedPosition(boneIndex), owner.Move + kick,
                    owner.Rotation);  // 'owner.Rotation' could also be 'direction'
                Arena.Gobs.Add(shot);
                liveShots.Add(shot);
            });
        }

        private void CreateMuzzleFire(int barrelBoneIndex, float barrelRotation)
        {
            foreach (var engineName in muzzleFireEngineNames)
            {
                Gob.CreateGob<Peng>(engineName, fireEngine =>
                {
                    fireEngine.Owner = owner.Owner;
                    fireEngine.Leader = owner;
                    fireEngine.LeaderBone = barrelBoneIndex;
                    Arena.Gobs.Add(fireEngine);
                });
            }
        }

        private void RemoveOldShots()
        {
            for (int i = liveShots.Count - 1; i >= 0; --i)
                if (liveShots[i].Dead)
                    liveShots.RemoveAt(i);
        }

        public override void Dispose()
        {
            // Kill existing shots if any exist and there is danger that 
            // they won't die soon by themselves.
            if (fireAction == FireAction.KillAll && liveShots.Count > 0)
            {
                foreach (Gob gob in liveShots)
                    gob.Die(new DeathCause());
            }
        }
    }
}
