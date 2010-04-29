using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
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
    public class ForwardShot : Weapon, IConsistencyCheckable
    {
        #region ForwardShot fields

        /// <summary>
        /// The ship gun barrels this weapon uses.
        /// </summary>
        [TypeParameter]
        ShipBarrelTypes gunBarrels;

        /// <summary>
        /// The sound to play when firing.
        /// </summary>
        [TypeParameter]
        string fireSound;

        /// <summary>
        /// Names of muzzle fire engine types.
        /// </summary>
        [TypeParameter]
        CanonicalString[] muzzleFireEngineNames;

        /// <summary>
        /// How fast the shots leave the weapon barrel,
        /// in meters per second.
        /// </summary>
        [TypeParameter]
        float shotSpeed;

        /// <summary>
        /// Number of shots to shoot in a series.
        /// </summary>
        [TypeParameter]
        int shotCount;

        /// <summary>
        /// Temporal spacing between successive shots in a series,
        /// in seconds.
        /// </summary>
        [TypeParameter]
        float shotSpacing;

        /// <summary>
        /// Difference of the maximum and the minimum of a shot's random angle
        /// relative to the general shot direction.
        /// </summary>
        [TypeParameter]
        float shotAngleVariation;

        /// <summary>
        /// Difference of the maximum and the minimum of a shot's random speed
        /// relative to the general shot speed.
        /// </summary>
        [TypeParameter]
        float shotSpeedVariation;

        /// <summary>
        /// What to do when firing the weapon.
        /// </summary>
        [TypeParameter]
        FireAction fireAction;

        /// <summary>
        /// How many shots left of the series.
        /// </summary>
        [RuntimeState]
        int shotsLeft;

        /// <summary>
        /// Time at which to shoot the next shot in the series, in game time.
        /// </summary>
        [RuntimeState]
        TimeSpan nextShot;

        /// <summary>
        /// Shots produced by this weapon that are still alive.
        /// </summary>
        [RuntimeState]
        List<Gob> liveShots;

        /// <summary>
        /// Have all shooting-related muzzle fire engines and sound effects been created this frame.
        /// </summary>
        bool _flashAndBangCreated;

        #endregion ForwardShot fields

        /// This constructor is only for serialisation.
        public ForwardShot()
            : base()
        {
            gunBarrels = ShipBarrelTypes.Middle | ShipBarrelTypes.Left | ShipBarrelTypes.Right | ShipBarrelTypes.Rear;
            fireSound = "dummysound";
            muzzleFireEngineNames = new CanonicalString[] { (CanonicalString)"dummyparticleengine" };
            shotSpeed = 300f;
            shotCount = 3;
            shotSpacing = 0.2f;
            shotAngleVariation = 0.3f;
            shotSpeedVariation = 20f;
            fireAction = FireAction.Shoot;
            shotsLeft = 2;
            nextShot = new TimeSpan(1, 2, 3);
            liveShots = new List<Gob>();
        }

        public ForwardShot(CanonicalString typeName)
            : base(typeName)
        {
            shotsLeft = 0;
            nextShot = new TimeSpan(0);
            liveShots = new List<Gob>();
        }

        /// <summary>
        /// Fires the weapon.
        /// </summary>
        protected override void FireImpl(AW2.UI.ControlState triggerState)
        {
            switch (fireAction)
            {
                case FireAction.KillAll:
                    if (triggerState.pulse)
                    {
                        if (liveShots.Count > 0)
                            foreach (Gob gob in liveShots)
                                gob.Die(new DeathCause());
                        else
                            TryShoot();
                    }
                    break;
                case FireAction.Shoot:
                    if (triggerState.pulse) TryShoot();
                    break;
                case FireAction.ShootContinuously:
                    if (triggerState.force > 0) TryShoot();
                    break;
                default: throw new ApplicationException("Unknown FireAction " + fireAction);
            }
        }

        private void TryShoot()
        {
            if (!CanFire) return;
            StartFiring();
            switch (FireMode)
            {
                case FireModeType.Single: shotsLeft = shotCount; break;
                case FireModeType.Continuous: shotsLeft = 1; break;
                default: throw new ApplicationException("Unknown FireMode " + FireMode);
            }
            if (nextShot < AssaultWing.Instance.GameTime.TotalArenaTime)
                nextShot = AssaultWing.Instance.GameTime.TotalArenaTime;
        }

        public override void Activate()
        {
            FireMode = fireAction == FireAction.ShootContinuously ? FireModeType.Continuous : FireModeType.Single;
        }

        /// <summary>
        /// Updates the weapon's state and performs actions true to its nature.
        /// </summary>
        public override void Update()
        {
            _flashAndBangCreated = false;
            while (IsItTimeToShoot())
            {
                CreateFlashAndBang();
                ForEachShipBarrel(gunBarrels, CreateShot);
                ApplyRecoil();
                nextShot += TimeSpan.FromSeconds(shotSpacing);
                switch (FireMode)
                {
                    case FireModeType.Single:
                        --shotsLeft;
                        if (shotsLeft == 0) DoneFiring();
                        break;
                    case FireModeType.Continuous:
                        shotsLeft = 1;
                        break;
                    default: throw new ApplicationException("Unknown FireMode " + FireMode);
                }
            }
            if (FireMode == FireModeType.Continuous)
            {
                shotsLeft = 0;
                DoneFiring();
            }
            RemoveOldShots();
        }

        private bool IsItTimeToShoot()
        {
            if (shotsLeft <= 0) return false;
            if (nextShot > AssaultWing.Instance.GameTime.TotalArenaTime) return false;
            return true;
        }

        private void CreateShot(int boneIndex, float barrelRotation)
        {
            float direction = barrelRotation + owner.Rotation + shotAngleVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
            float kickSpeed = shotSpeed + shotSpeedVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
            Vector2 kick = kickSpeed * AWMathHelper.GetUnitVector2(direction);
            Gob.CreateGob(shotTypeName, shot =>
            {
                shot.Owner = owner.Owner;
                shot.ResetPos(owner.GetNamedPosition(boneIndex), owner.Move + kick,
                    owner.Rotation);  // 'owner.Rotation' could also be 'direction'
                Arena.Gobs.Add(shot);
                liveShots.Add(shot);
            });
        }

        private void CreateFlashAndBang()
        {
            PlayFiringSound();
            ForEachShipBarrel(gunBarrels, CreateMuzzleFire);
            _flashAndBangCreated = true;
        }

        private void CreateMuzzleFire(int barrelBoneIndex, float barrelRotation)
        {
            if (_flashAndBangCreated) return;
            foreach (var engineName in muzzleFireEngineNames)
            {
                Gob.CreateGob(engineName, fireEngine =>
                {
                    if (fireEngine is Peng)
                    {
                        Peng peng = (Peng)fireEngine;
                        peng.Owner = owner.Owner;
                        peng.Leader = owner;
                        peng.LeaderBone = barrelBoneIndex;
                    }
                    Arena.Gobs.Add(fireEngine);
                });
            }
        }

        private void PlayFiringSound()
        {
            if (_flashAndBangCreated) return;
            AssaultWing.Instance.SoundEngine.PlaySound(fireSound);
        }

        private void RemoveOldShots()
        {
            for (int i = liveShots.Count - 1; i >= 0; --i)
                if (liveShots[i].Dead)
                    liveShots.RemoveAt(i);
        }

        /// <summary>
        /// Releases all resources allocated by the weapon.
        /// </summary>
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

        #region INetworkSerializable Members

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((bool)_flashAndBangCreated);
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, TimeSpan messageAge)
        {
            base.Deserialize(reader, mode, messageAge);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                bool mustCreateFlashAndBang = reader.ReadBoolean();
                if (mustCreateFlashAndBang) CreateFlashAndBang();
            }
        }

        #endregion

        #region IConsistencyCheckable Members

        public void MakeConsistent(Type limitationAttribute)
        {
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                shotCount = Math.Max(1, shotCount);
                shotSpacing = Math.Max(0, shotSpacing);
            }
        }

        #endregion
    }
}
