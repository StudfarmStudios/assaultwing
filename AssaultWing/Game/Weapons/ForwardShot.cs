using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Events;
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
        SoundOptions.Action fireSound;

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
            this.fireSound = SoundOptions.Action.Pistol;
            this.muzzleFireEngineNames = new CanonicalString[] { (CanonicalString)"dummyparticleengine", };
            this.shotSpeed = 300f;
            this.shotCount = 3;
            this.shotSpacing = 0.2f;
            this.shotAngleVariation = 0.3f;
            this.shotSpeedVariation = 20f;
            this.fireAction = FireAction.Shoot;
            this.shotsLeft = 2;
            this.nextShot = new TimeSpan(1, 2, 3);
            this.liveShots = new List<Gob>();
        }

        /// <summary>
        /// Creates a new forward shooting weapon.
        /// </summary>
        /// <param name="typeName">The type of the weapon.</param>
        /// <param name="owner">The ship that owns this weapon.</param>
        /// <param name="ownerHandle">A handle for identifying the weapon at the owner.
        /// Use <b>1</b> for primary weapons and <b>2</b> for secondary weapons.</param>
        /// <param name="boneIndices">Indices of the bones that define the weapon's
        /// barrels' locations on the owning ship.</param>
        public ForwardShot(CanonicalString typeName, Ship owner, int ownerHandle, int[] boneIndices)
            : base(typeName, owner, ownerHandle, boneIndices)
        {
            this.shotsLeft = 0;
            this.nextShot = new TimeSpan(0);
            muzzleFireEngines = new List<Gob>[boneIndices.Length];
            for (int i = 0; i < boneIndices.Length; ++i)
                muzzleFireEngines[i] = new List<Gob>();
            this.liveShots = new List<Gob>();
        }

        /// <summary>
        /// Fires the weapon.
        /// </summary>
        public override void Fire()
        {
            // Do something with existing shots if any exist and
            // if there is anything to be done with them.
            if (fireAction == FireAction.KillAll && liveShots.Count > 0)
            {
                foreach (Gob gob in liveShots)
                    gob.Die(new DeathCause());
            }
            else
            // Otherwise fire new shots if possible.
            if (CanFire)
            {
                // Start a new series.
                StartFiring();
                owner.UseCharge(ownerHandle, fireCharge);
                shotsLeft = shotCount;
                nextShot = AssaultWing.Instance.GameTime.TotalGameTime;
            }
        }

        /// <summary>
        /// Updates the weapon's state and performs actions true to its nature.
        /// </summary>
        public override void Update()
        {
            flashAndBangCreated = false;

            // Shoot if its time.
            while (shotsLeft > 0 && nextShot <= AssaultWing.Instance.GameTime.TotalGameTime)
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

                // Remember when to shoot again.
                --shotsLeft;
                nextShot += TimeSpan.FromSeconds(shotSpacing);

                if (shotsLeft == 0)
                    DoneFiring();
            }

            UpdateMuzzleFire();
            RemoveOldMuzzleFire();
            RemoveOldShots();
        }

        private void CreateShot(int boneI)
        {
            float direction = owner.Rotation +
                shotAngleVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
            float kickSpeed = shotSpeed +
                shotSpeedVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
            Vector2 kick = new Vector2((float)Math.Cos(direction), (float)Math.Sin(direction))
                * kickSpeed;
            Gob.CreateGob(shotTypeName, shot =>
            {
                shot.Owner = owner.Owner;
                shot.Pos = owner.GetNamedPosition(boneI);
                shot.Move = owner.Move + kick;
                shot.Rotation = owner.Rotation; // could also be 'direction'
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
            var eventer = (EventEngine)AssaultWing.Instance.Services.GetService(typeof(EventEngine));
            var soundEvent = new SoundEffectEvent();
            soundEvent.setAction(fireSound);
            eventer.SendEvent(soundEvent);
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
                        emitter.Direction = Owner.Rotation;
                        peng.Pos = Owner.GetNamedPosition(boneI);
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
