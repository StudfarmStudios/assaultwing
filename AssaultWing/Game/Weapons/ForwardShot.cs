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
        string[] muzzleFireEngineNames;

        /// <summary>
        /// Currently active muzzle fire engines for each barrel.
        /// </summary>
        /// The array is indexed by barrels.
        List<ParticleEngine>[] muzzleFireEngines;

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

        #endregion ForwardShot fields

        /// <summary>
        /// Creates an uninitialised forward shooting weapon.
        /// </summary>
        /// This constructor is only for serialisation.
        public ForwardShot()
            : base()
        {
            this.fireSound = SoundOptions.Action.Pistol;
            this.muzzleFireEngineNames = new string[] { "dummyparticleengine", };
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
        public ForwardShot(string typeName, Ship owner, int ownerHandle, int[] boneIndices)
            : base(typeName, owner, ownerHandle, boneIndices)
        {
            this.shotsLeft = 0;
            this.nextShot = new TimeSpan(0);
            this.muzzleFireEngines = new List<ParticleEngine>[boneIndices.Length];
            for (int i = 0; i < boneIndices.Length; ++i)
                this.muzzleFireEngines[i] = new List<ParticleEngine>();
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
                    gob.Die();
            }
            else
            // Otherwise fire new shots if possible.
            if (CanFire)
            {
                // Start a new series.
                StartFiring();
                owner.UseCharge(ownerHandle, fireCharge);
                shotsLeft = shotCount;
                nextShot = physics.TimeStep.TotalGameTime;
            }
        }

        /// <summary>
        /// Updates the weapon's state and performs actions true to its nature.
        /// </summary>
        public override void Update()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            EventEngine eventEngine = (EventEngine)AssaultWing.Instance.Services.GetService(typeof(EventEngine));

            // Shoot if its time.
            bool muzzleFireCreated = false;
            while (shotsLeft > 0 && nextShot <= physics.TimeStep.TotalGameTime)
            {
                // Every gun barrel shoots.
                for (int barrel = 0; barrel < boneIndices.Length; ++barrel)
                {
                    int boneI = boneIndices[barrel];

                    // Create a shot.
                    float direction = owner.Rotation +
                        shotAngleVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
                    float kickSpeed = shotSpeed +
                        shotSpeedVariation * RandomHelper.GetRandomFloat(-0.5f, 0.5f);
                    Vector2 kick = new Vector2((float)Math.Cos(direction), (float)Math.Sin(direction))
                        * kickSpeed;
                    Gob shot = Gob.CreateGob(shotTypeName);
                    shot.Owner = owner.Owner;
                    shot.Pos = owner.GetNamedPosition(boneI);
                    shot.Move = owner.Move + kick;
                    shot.Rotation = owner.Rotation; // could also be 'direction'
                    data.AddGob(shot);
                    liveShots.Add(shot);

                    // Create muzzle fire engines, but only once in a frame.
                    if (!muzzleFireCreated)
                        foreach (string engineName in muzzleFireEngineNames)
                        {
                            ParticleEngine fireEngine = new ParticleEngine(engineName);
                            muzzleFireEngines[barrel].Add(fireEngine);
                            data.AddParticleEngine(fireEngine);
                        }
                }
                muzzleFireCreated = true;

                // Let our owner feel the consequences.
                ApplyRecoil();

                // Play a firing sound.
                SoundEffectEvent soundEvent = new SoundEffectEvent();
                soundEvent.setAction(fireSound);
                eventEngine.SendEvent(soundEvent);

                // Remember when to shoot again.
                --shotsLeft;
                TimeSpan shotSpacingSpan = new TimeSpan((long)(10 * 1000 * 1000 * shotSpacing));
                nextShot = nextShot.Add(shotSpacingSpan);

                if (shotsLeft == 0)
                    DoneFiring();
            }

            // Update muzzle fire engines.
            for (int barrel = 0; barrel < boneIndices.Length; ++barrel)
                foreach (ParticleEngine engine in muzzleFireEngines[barrel])
                {
                    int boneI = boneIndices[barrel];
                    DotEmitter emitter = (DotEmitter)engine.Emitter;
                    emitter.Direction = Owner.Rotation;
                    engine.Position = new Vector3(Owner.GetNamedPosition(boneI), 0);
                }

            // Forget about dead fire engines.
            for (int barrel = 0; barrel < boneIndices.Length; ++barrel)
                for (int i = 0; i < muzzleFireEngines[barrel].Count; )
                {
                    if (!muzzleFireEngines[barrel][i].IsAlive)
                        muzzleFireEngines[barrel].RemoveAt(i);
                    else
                        ++i;
                }

            // Forget about dead shots.
            for (int i = 0; i < liveShots.Count; )
            {
                if (liveShots[i].Dead)
                    liveShots.RemoveAt(i);
                else
                    ++i;
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
