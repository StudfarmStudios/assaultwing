using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Events;
using AW2.Helpers;
using AW2.Sound;
using AW2.Game.Particles;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// An explosion; inflicts damage, makes a big flash, throws some stuff around.
    /// </summary>
    class Explosion : Gob
    {
        #region Explosion fields

        /// <summary>
        /// Amount of damage to inflict as a function of distance
        /// from the explosion.
        /// </summary>
        [TypeParameter]
        Curve inflictDamage;

        /// <summary>
        /// Speed of gas flow away from the explosion's center, measured in
        /// meters per second as a function of the distance from the explosion's
        /// center. Gas flow affects the movement of gobs.
        /// </summary>
        [TypeParameter]
        Curve flowSpeed;

        /// <summary>
        /// Time, in seconds of game time, of how long there is a gas flow away
        /// from the center of the explosion.
        /// </summary>
        [TypeParameter]
        float flowTime;

        /// <summary>
        /// Time of gas flow end, in game time.
        /// </summary>
        TimeSpan flowEndTime;

        /// <summary>
        /// The radius of the hole to make on impact to walls.
        /// </summary>
        [TypeParameter]
        float impactHoleRadius;

        /// <summary>
        /// Names of the particle engines to create.
        /// </summary>
        [TypeParameter]
        string[] particleEngineNames;

        /// <summary>
        /// Name of the sound effect to play on creation.
        /// </summary>
        [TypeParameter]
        SoundOptions.Action sound;

        ParticleEngine[] particleEngines;

        bool firstCollisionChecked;

        #endregion Explosion fields

        /// <summary>
        /// Creates an uninitialised explosion.
        /// </summary>
        /// This constructor is only for serialisation.
        public Explosion()
            : base()
        {
            inflictDamage = new Curve();
            inflictDamage.PreLoop = CurveLoopType.Constant;
            inflictDamage.PostLoop = CurveLoopType.Constant;
            inflictDamage.Keys.Add(new CurveKey(0, 200, 0, 0, CurveContinuity.Smooth));
            inflictDamage.Keys.Add(new CurveKey(300, 0, -3, -3, CurveContinuity.Smooth));
            flowSpeed = new Curve();
            flowSpeed.PreLoop = CurveLoopType.Constant;
            flowSpeed.PostLoop = CurveLoopType.Constant;
            flowSpeed.Keys.Add(new CurveKey(0, 6000, 0, 0, CurveContinuity.Smooth));
            flowSpeed.Keys.Add(new CurveKey(300, 0, -1.5f, -1.5f, CurveContinuity.Smooth));
            flowTime = 0.5f;
            impactHoleRadius = 100;
            particleEngineNames = new string[] { "dummyparticleengine", };
            sound = SoundOptions.Action.Explosion;
            firstCollisionChecked = false;
        }

        /// <summary>
        /// Creates an explosion.
        /// </summary>
        /// <param name="typeName">The type of the explosion.</param>
        public Explosion(string typeName)
            : base(typeName)
        {
            flowEndTime = new TimeSpan(1);
            particleEngines = null;
            firstCollisionChecked = false;
        }

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            EventEngine eventEngine = (EventEngine)AssaultWing.Instance.Services.GetService(typeof(EventEngine));

            // Play the sound.
            SoundEffectEvent soundEvent = new SoundEffectEvent();
            soundEvent.setAction(sound);
            eventEngine.SendEvent(soundEvent);

            // Create particle engines.
            particleEngines = new ParticleEngine[particleEngineNames.Length];
            for (int i = 0; i < particleEngineNames.Length; ++i)
            {
                particleEngines[i] = new ParticleEngine(particleEngineNames[i]);
                particleEngines[i].Pos = this.Pos;
                data.AddParticleEngine(particleEngines[i]);
            }

            // Count end time of gas flow.
            long ticks = (long)(10 * 1000 * 1000 * flowTime);
            flowEndTime = AssaultWing.Instance.GameTime.TotalGameTime + new TimeSpan(ticks);

            // Make a hole in the arena walls.
            physics.MakeHole(Pos, impactHoleRadius);

            base.Activate();
        }

        /// <summary>
        /// Updates the explosion.
        /// </summary>
        public override void Update()
        {
            // Have our collisions checked.
            base.Update();

            if (!firstCollisionChecked)
            {
                firstCollisionChecked = true;

                // Remove collision areas that only needed to collide once.
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                data.CustomOperations += delegate(object obj)
                {
                    physics.Unregister(this);
                    collisionAreas = Array.FindAll(collisionAreas, delegate(CollisionArea collArea)
                    {
                        return collArea.Name == "Force";
                    });
                    physics.Register(this);
                };
            }

            // When the flow ends, there's nothing more to do.
            if (AssaultWing.Instance.GameTime.TotalGameTime >= flowEndTime)
            {
                Die(new DeathCause());
            }
        }

        /// <summary>
        /// Draws the gob.
        /// </summary>
        /// Assumes that the sprite batch has been Begun already and will be
        /// Ended later by someone else.
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="spriteBatch">The sprite batch to draw sprites with.</param>
        public override void Draw(Matrix view, Matrix projection, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            // Our particle engines do the visual stuff.
        }

        /// <summary>
        /// Performs collision operations for the case when one of this gob's collision areas
        /// is overlapping one of another gob's collision areas.
        /// </summary>
        /// <param name="myArea">The collision area of this gob.</param>
        /// <param name="theirArea">The collision area of the other gob.</param>
        /// <param name="stuck">If <b>true</b> then the gob is stuck, i.e.
        /// <b>theirArea.Type</b> matches <b>myArea.CannotOverlap</b> and it's not possible
        /// to backtrack out of the overlap. It is then up to this gob and the other gob 
        /// to resolve the overlap.</param>
        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            // We assume we have only these collision areas, all receptors with specific names:
            // "Hit" is assumed to collide only against damageables;
            // "Force" is assumed to collide only against movables.
            if (myArea.Name == "Force")
            {
                Vector2 difference = theirArea.Owner.Pos - this.Pos;
                float differenceLength = difference.Length();
                Vector2 flow = difference / differenceLength *
                    flowSpeed.Evaluate(differenceLength);
                physics.ApplyDrag(theirArea.Owner, flow, 0.003f);
            }
            else if (myArea.Name == "Hit")
            {
                float distance = theirArea.Area.DistanceTo(this.Pos);
                float damage = inflictDamage.Evaluate(distance);
                theirArea.Owner.InflictDamage(damage, new DeathCause(DeathCauseType.Damage, this));
            }
        }
    }
}
