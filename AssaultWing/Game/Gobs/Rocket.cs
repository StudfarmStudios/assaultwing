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
    class Rocket : Gob
    {
        #region Rocket fields

        /// <summary>
        /// Amount of damage to inflict on impact with a damageable gob.
        /// </summary>
        [TypeParameter]
        float impactDamage;

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
            thrustEndTime = AssaultWing.Instance.GameTime.TotalGameTime 
                + TimeSpan.FromSeconds(thrustDuration);
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
            base.Activate();
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
            if (physics.TimeStep.TotalGameTime >= thrustEndTime)
                for (int i = 0; i < exhaustEngines.Length; ++i)
                    exhaustEngines[i].IsAlive = false;
        }

        #endregion Methods related to gobs' functionality in the game world

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
            if ((theirArea.Type & CollisionAreaType.PhysicalDamageable) != 0)
                theirArea.Owner.InflictDamage(impactDamage, new DeathCause(theirArea.Owner, DeathCauseType.Damage, this));
            Die(new DeathCause());
        }
    }
}
