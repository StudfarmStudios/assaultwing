using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Sound;
using AW2.Game.Particles;

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
        /// Currently active muzzle fire engines for each barrel.
        /// </summary>
        /// The array is indexed by barrels.
        List<Gob>[] muzzleFireEngines;

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
        bool flashAndBangCreated;

        #endregion ForwardShot fields

        /// <summary>
        /// Creates an uninitialised forward shooting weapon.
        /// </summary>
        /// This constructor is only for serialisation.
        public ForwardShot()
            : base()
        {
            fireSound = "Pistol";
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
            muzzleFireEngines = new List<Gob>[0];
            liveShots = new List<Gob>();
        }

        /// <summary>
        /// Fires the weapon.
        /// </summary>
        public override void Fire(AW2.UI.ControlState triggerState)
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
            nextShot = AssaultWing.Instance.GameTime.TotalGameTime;
        }

        public override void Activate()
        {
            FireMode = fireAction == FireAction.ShootContinuously ? FireModeType.Continuous : FireModeType.Single;
            muzzleFireEngines = new List<Gob>[boneIndices.Length];
            for (int i = 0; i < boneIndices.Length; ++i)
                muzzleFireEngines[i] = new List<Gob>();
        }

        /// <summary>
        /// Updates the weapon's state and performs actions true to its nature.
        /// </summary>
        public override void Update()
        {
            flashAndBangCreated = false;

            while (IsItTimeToShoot())
            {
                // Every gun barrel shoots.
                for (int barrel = 0; barrel < boneIndices.Length; ++barrel)
                {
                    int boneI = boneIndices[barrel];
                    CreateShot(boneI);
                    CreateMuzzleFire(barrel, boneI);
                }

                ApplyRecoil();
                PlayFiringSound();
                flashAndBangCreated = true;
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

            UpdateMuzzleFire();
            RemoveOldMuzzleFire();
            RemoveOldShots();
        }

        private bool IsItTimeToShoot()
        {
            if (shotsLeft <= 0) return false;
            if (nextShot > AssaultWing.Instance.GameTime.TotalGameTime) return false;
            return true;
        }

        private void CreateShot(int boneI)
        {
            float direction = owner.Rotation +
                shotAngleVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
            float kickSpeed = shotSpeed +
                shotSpeedVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
            Vector2 kick = kickSpeed * AWMathHelper.GetUnitVector2(direction);
            Gob.CreateGob(shotTypeName, shot =>
            {
                shot.Owner = owner.Owner;
                shot.ResetPos(owner.GetNamedPosition(boneI), owner.Move + kick,
                    owner.Rotation);  // 'owner.Rotation' could also be 'direction'
                Arena.Gobs.Add(shot);
                liveShots.Add(shot);
            });
        }

        private void CreateMuzzleFire(int barrel, int boneI)
        {
            if (flashAndBangCreated) return;
            foreach (var engineName in muzzleFireEngineNames)
            {
                Gob.CreateGob(engineName, fireEngine =>
                {
                    if (fireEngine is Peng)
                    {
                        Peng peng = (Peng)fireEngine;
                        peng.Owner = owner.Owner;
                        peng.Leader = owner;
                        peng.LeaderBone = boneI;
                    }
                    muzzleFireEngines[barrel].Add(fireEngine);
                    Arena.Gobs.Add(fireEngine);
                });
            }
        }

        private void PlayFiringSound()
        {
            if (flashAndBangCreated) return;
            AssaultWing.Instance.SoundEngine.PlaySound(fireSound.ToString());
        }

        private void UpdateMuzzleFire()
        {
            for (int barrel = 0; barrel < boneIndices.Length; ++barrel)
                foreach (Gob engine in muzzleFireEngines[barrel])
                    if (engine is ParticleEngine)
                    {
                        ParticleEngine peng = (ParticleEngine)engine;
                        int boneI = boneIndices[barrel];
                        DotEmitter emitter = (DotEmitter)peng.Emitter;
                        emitter.Direction = owner.Rotation;
                        peng.Pos = owner.GetNamedPosition(boneI);
                        peng.Move = owner.Move;
                    }
        }

        private void RemoveOldMuzzleFire()
        {
            for (int barrel = 0; barrel < boneIndices.Length; ++barrel)
                for (int i = muzzleFireEngines[barrel].Count - 1; i >= 0; --i)
                    if (muzzleFireEngines[barrel][i] is ParticleEngine)
                    {
                        if (!((ParticleEngine)muzzleFireEngines[barrel][i]).IsAlive)
                            muzzleFireEngines[barrel].RemoveAt(i);
                    }
                    else
                        if (muzzleFireEngines[barrel][i].Dead)
                            muzzleFireEngines[barrel].RemoveAt(i);
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

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
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
