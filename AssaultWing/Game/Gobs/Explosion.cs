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
    class Explosion : Gob, IGas
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
            particleEngineNames = new string[] { "dummyparticleengine", };
            sound = SoundOptions.Action.Explosion;
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
            base.physicsApplyMode = PhysicsApplyMode.Move;
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

            base.Activate();
        }

        /// <summary>
        /// Updates the explosion.
        /// </summary>
        public override void Update()
        {
            // Have our collisions checked.
            base.Update();

            // Remove damage-inflicting collision area for future frames.
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            data.CustomOperations += delegate(object obj)
            {
                physics.Unregister(this);
                collisionAreas = Array.FindAll(collisionAreas, delegate(CollisionArea collArea)
                {
                    return collArea.Name != "Hit"; 
                });
                physics.Register(this);
            };

            // When the flow ends, there's nothing more to do.
            if (AssaultWing.Instance.GameTime.TotalGameTime >= flowEndTime)
            {
                Die();
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

        #region ICollidable Members

        /// <summary>
        /// Performs collision operations with a gob whose general collision area
        /// has collided with one of our receptor areas.
        /// </summary>
        /// <param name="gob">The gob we collided with.</param>
        /// <param name="receptorName">The name of our colliding receptor area.</param>
        public override void Collide(ICollidable gob, string receptorName)
        {
            if (receptorName == "Hit")
            {
                IDamageable gobDamageable = gob as IDamageable;
                if (gobDamageable != null)
                {
                    float distance = gob.DistanceTo(this.Pos);
                    float damage = inflictDamage.Evaluate(distance);
                    gobDamageable.InflictDamage(damage);
                }
            }
            else if (receptorName == "Force")
            {
                Gob gobGob = gob as Gob;
                if (gobGob != null && (gobGob.PhysicsApplyMode & PhysicsApplyMode.Move) != 0)
                {
                    Vector2 difference = gobGob.Pos - this.Pos;
                    float differenceLength = difference.Length();
                    Vector2 flow = difference / differenceLength *
                        flowSpeed.Evaluate(differenceLength);
                    physics.ApplyDrag(gobGob, flow, 0.003f);
                }
            }
        }

        #endregion ICollidable Members
    }
}
