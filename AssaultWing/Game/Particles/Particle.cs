using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Particles
{
    /// <summary>
    /// Represents a single 2d-textured particle. 
    /// Particle moves and changes in color, opacity and size during time.
    /// </summary>
    public class Particle
    {
        #region Fields

        private float age;
        private float totalAge;

        private float size;
        private float initialSize;
        private float finalSize;

        private float rotation;
        private float deltaRotation;

        private Color currentColor;
        private Color initialColor;
        private Color finalColor;

        private Vector3 position;

        private Vector3 velocity;
        private Vector3 acceleration;

        private float mass;

        private ParticleEngine parent;

        #endregion

        #region Properties

        /// <summary>
        /// This is the time (in seconds) that this particle has left before it's removed.
        /// When set, also sets the total lifespan of the particle.
        /// </summary>
        public float Age
        {
            get { return age; }
            set { age = value; totalAge = value; }
        }

        /// <summary>
        /// Length of the particle's whole life.
        /// </summary>
        public float TotalAge
        {
            get { return totalAge; }
        }

        /// <summary>
        /// Current size of the particle.
        /// </summary>
        public float Size
        {
            get { return size; }
            set { size = value; }
        }

        /// <summary>
        /// Size of the particle when it was created.
        /// </summary>
        public float InitialSize
        {
            get { return initialSize; }
            set { initialSize = value; }
        }

        /// <summary>
        /// Size of the particle just before it is removed.
        /// </summary>
        public float FinalSize
        {
            get { return finalSize; }
            set { finalSize = value; }
        }

        /// <summary>
        /// Current rotation of the particle.
        /// </summary>
        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        /// <summary>
        /// The amount that this particle changes in rotation per second.
        /// </summary>
        public float DeltaRotation
        {
            get { return deltaRotation; }
            set { deltaRotation = value; }
        }

        /// <summary>
        /// Current color of the particle. Used to colorize the texture of the particle.
        /// </summary>
        public Color CurrentColor
        {
            get { return currentColor; }
        }

        /// <summary>
        /// The color of the particle when it's created. (Also sets the current color to given value)
        /// </summary>
        public Color InitialColor
        {
            get { return initialColor; }
            set { currentColor = value; initialColor = value; }
        }

        /// <summary>
        /// The color of the particle just before it's removed.
        /// </summary>
        public Color FinalColor
        {
            get { return finalColor; }
            set { finalColor = value; }
        }

        /// <summary>
        /// Position of the particle. Currently Z is ignored.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        /// <summary>
        /// Speed of the particle. Currently Z is ignored.
        /// </summary>
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        /// <summary>
        /// Acceleration of the particle.
        /// </summary>
        public Vector3 Acceleration
        {
            get { return acceleration; }
            set { acceleration = value; }
        }

        /// <summary>
        /// Mass of the particle. TODO: Use with gravitation calculations
        /// </summary>
        public float Mass
        {
            get { return mass; }
            set { mass = value; }
        }

        /// <summary>
        /// Engine that created this particle.
        /// </summary>
        public ParticleEngine Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Class constructor
        /// </summary>
        public Particle() 
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the particle data (size, age, acceleration, position...)
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            float time;

            time = gameTime.ElapsedGameTime.Milliseconds * 0.001f;
            age -= time;

            position += velocity * time;
            acceleration += parent.Gravity * mass;

            velocity += acceleration * time;
            velocity *= (float) Math.Pow(parent.DragForce, time);

            float ageDone = totalAge - Math.Max(age, 0);
            float ageRatio = ageDone / totalAge;
            size = initialSize + (finalSize - initialSize) * ageRatio;

            if (this.parent.ID == 15)
            {
                Log.Write("totalAge: " + totalAge + " agedone: " + ageDone + " ageRatio: " + ageRatio + " size: " + size);
            }
            rotation += deltaRotation * time;
            currentColor = new Color(
                (byte)(initialColor.R + (finalColor.R - initialColor.R) * ageRatio),
                (byte)(initialColor.G + (finalColor.G - initialColor.G) * ageRatio),
                (byte)(initialColor.B + (finalColor.B - initialColor.B) * ageRatio),
                (byte)(initialColor.A + (finalColor.A - initialColor.A) * ageRatio));
        }

        #endregion
    }
}
