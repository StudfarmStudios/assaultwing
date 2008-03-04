using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Game.Particles;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A rocket that has its own means of propulsion.
    /// </summary>
    class Rocket : Gob, IProjectile
    {
        #region Rocket fields

        /// <summary>
        /// Amount of damage to inflict on impact with a damageable gob.
        /// </summary>
        [TypeParameter]
        float impactDamage;

        /// <summary>
        /// The hole to make on impact to walls.
        /// </summary>
        [TypeParameter]
        Polygon impactArea;

        /// <summary>
        /// Maximum force of thrust of the rocket, measured in Newtons.
        /// </summary>
        [TypeParameter]
        float thrustForce;

        /// <summary>
        /// Duration of thrust, measured in seconds.
        /// </summary>
        [TypeParameter]
        float thrustDuration;

        /// <summary>
        /// Maximum turning speed of the rocket, measured in radians per second.
        /// </summary>
        [TypeParameter]
        float turnSpeed;

        /// <summary>
        /// Time at which thursting ends, in game time.
        /// </summary>
        [RuntimeState]
        TimeSpan thrustEndTime;

        #endregion Rocket fields

        /// <summary>
        /// Creates an uninitialised rocket.
        /// </summary>
        /// This constructor is only for serialisation.
        public Rocket()
            : base()
        {
            this.impactDamage = 100;
            this.impactArea = new Polygon(new Vector2[] { 
                new Vector2(-5,-5),
                new Vector2(-5,5),
                new Vector2(7,0)});
            this.thrustForce = 100;
            this.thrustDuration = 2;
            this.turnSpeed = 5;
            this.thrustEndTime = new TimeSpan(0,0,0,9);
        }

        /// <summary>
        /// Creates a rocket.
        /// </summary>
        /// <param name="typeName">The type of the rocket.</param>
        public Rocket(string typeName)
            : base(typeName)
        {
            double endTicks = 10 * 1000 * 1000 *
                (physics.TimeStep.TotalGameTime.TotalSeconds + this.thrustDuration);
            this.thrustEndTime = new TimeSpan((long)endTicks);
            base.physicsApplyMode = PhysicsApplyMode.All | PhysicsApplyMode.ReceptorCollidesPhysically;
        }

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        /// DataEngine will call this method to make the gob do necessary 
        /// initialisations to make it fully functional on addition to 
        /// an ongoing play of the game.
        public override void Activate()
        {
            CreateExhaustEngines();
            foreach (ParticleEngine engine in exhaustEngines)
                engine.IsAlive = true;
        }

        /// <summary>
        /// Updates the gob according to its natural behaviour.
        /// </summary>
        public override void Update()
        {
            if (physics.TimeStep.TotalGameTime < thrustEndTime)
            {
                // Thrust.
                Vector2 forceVector = new Vector2((float)Math.Cos(Rotation), (float)Math.Sin(Rotation))
                    * thrustForce;
                physics.ApplyForce(this, forceVector);
            }
            else
            {
                // Fall nose first.
                float rotationGoal = (float)Math.Acos(Move.X / Move.Length());
                if (Move.Y < 0)
                    rotationGoal = MathHelper.TwoPi - rotationGoal;
                Rotation = AWMathHelper.InterpolateTowardsAngle(Rotation, rotationGoal, 
                    physics.ApplyChange(turnSpeed));
            }

            base.Update();

            // Manage exhaust engines.
            // We do this after our position has been updated by
            // base.Update() to get exhaust fumes in the right spot.
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Model model = data.GetModel(ModelName);
            UpdateModelPartTransforms(model);
            for (int i = 0; i < exhaustEngines.Length; ++i)
            {
                exhaustEngines[i].Position = new Vector3(GetNamedPosition(exhaustBoneIs[i]), 0);
                ((DotEmitter)exhaustEngines[i].Emitter).Direction = Rotation + MathHelper.Pi;
                if (physics.TimeStep.TotalGameTime >= thrustEndTime)
                    exhaustEngines[i].IsAlive = false;
            }
        }

        #endregion Methods related to gobs' functionality in the game world

        #region ICollidable Members
        // Some members are implemented in class Gob.

        /// <summary>
        /// Performs collision operations with a gob whose general collision area
        /// has collided with one of our receptor areas.
        /// </summary>
        /// <param name="gob">The gob we collided with.</param>
        /// <param name="receptorName">The name of our colliding receptor area.</param>
        public override void Collide(ICollidable gob, string receptorName)
        {
            IDamageable damaGob = gob as IDamageable;
            if (damaGob != null)
                damaGob.InflictDamage(impactDamage);

            Die();

            // Fake safe position to make physical collisions happen.
            // We can do this only because we know we're dead already.
            HadSafePosition = true;
        }

        #endregion
        
        #region IProjectile Members

        /// <summary>
        /// The area the projectile destroys from thick gobs on impact.
        /// </summary>
        /// The area is translated according to the gob's location.
        public Polygon ImpactArea
        {
            get
            {
                return (Polygon)impactArea.Transform(WorldMatrix);
            }
        }

        #endregion
    }
}
