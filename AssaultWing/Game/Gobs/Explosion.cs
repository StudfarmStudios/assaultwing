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
        /// Momentum delivered by the explosion's shockwave, measured
        /// in Newton seconds. The momentum is presented as 
        /// a function of distance from the explosion.
        /// </summary>
        [TypeParameter]
        Curve shockMomentum;

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
            shockMomentum = new Curve();
            shockMomentum.PreLoop = CurveLoopType.Constant;
            shockMomentum.PostLoop = CurveLoopType.Constant;
            shockMomentum.Keys.Add(new CurveKey(0, 6000, 0, 0, CurveContinuity.Smooth));
            shockMomentum.Keys.Add(new CurveKey(300, 0, -1.5f, -1.5f, CurveContinuity.Smooth));
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
                particleEngines[i].Position = new Vector3(this.Pos, 0);
                data.AddParticleEngine(particleEngines[i]);
            }
        }

        /// <summary>
        /// Updates the explosion.
        /// </summary>
        public override void Update()
        {
            // Have our collisions checked.
            base.Update();

            // There's nothing more to do.
            Die();
        }

        /// <summary>
        /// Draws the gob.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public override void Draw(Microsoft.Xna.Framework.Matrix view, Microsoft.Xna.Framework.Matrix projection)
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
                    Vector2 momentum = Vector2.Normalize(difference) *
                        shockMomentum.Evaluate(difference.Length());
                    physics.ApplyMomentum(gobGob, momentum);
                }
            }
        }

        #endregion ICollidable Members
    }
}
