using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using System.Reflection;

namespace AW2.Game.Particles
{
    /// <summary>
    /// Represents a group of particles with a specific behavior
    /// </summary>
    [LimitedSerialization]
    public class ParticleEngine : Gob
    {
        #region Fields

        //SYSTEM SETTINGS 
        private int id = -1;
        [RuntimeState]
        private bool isAlive = true;

        [TypeParameter]
        private bool loop = false; //If the particle system is infinite or not
        [TypeParameter]
        private int totalNumberParticles = 0; //Particles emitted in the total lifetime of the system
        [RuntimeState]
        private int createdParticles = 0; //Number of emitted particles so far
        [TypeParameter]
        private float birthRate = 5; //Particles emitted per second
        [RuntimeState]
        private TimeSpan nextBirth; // Time of next particle birth, in game time.
        [RuntimeState]
        private Vector2 oldPosition = new Vector2(Single.NaN); //Previous position of the system
        [RuntimeState]
        private Vector3 movement = new Vector3(0, 0, 0); //Movement of the system
        [TypeParameter]
        private float depthLayer = 0.5f; // Depth layer for sprite draw order; 0 is front; 1 is back
        [TypeParameter]
        private Emitter emitter = new DotEmitter(); //Emmiter of the system
        [TypeParameter]
        private float dragForce = 1; //Drag force for the particles off the system
        [TypeParameter]
        private Vector3 gravity = new Vector3(0, 0, 0); //Gravity for the particles of the system
        [TypeParameter]
        private string textureName = "dummytexture"; // Name of texture for particles

        Gob leader = null; // The leader who we follow, or null.

        //PARTICLES DATA
        [TypeParameter]
        private ExpectedValue particleAge = new ExpectedValue(2, 0); // Expected age of a particle

        [TypeParameter]
        private ExpectedValue particleInitialSize = new ExpectedValue(1, 0); //Initial size of a particle

        [TypeParameter]
        private ExpectedValue particleFinalSize = new ExpectedValue(2, 0); //Final size of a particle

        [TypeParameter]
        private ExpectedValue particleRotationSpeed = new ExpectedValue(); // Expected rotation speed of a particle

        [TypeParameter]
        private Color particleInitialColor = new Color(255, 255, 255, 255); //Initial color of the particle

        [TypeParameter]
        private Color particleFinalColor = new Color(0, 0, 0, 0); //Final color of the particle

        [TypeParameter]
        private ExpectedValue particleSpeed = new ExpectedValue(1, 0); //Expected speed of a particle

        [TypeParameter]
        private ExpectedValue particleAcceleration = new ExpectedValue(); //Expected acceleration of a particle

        [TypeParameter]
        private ExpectedValue particleMass = new ExpectedValue(); // Expected mass of a particle

        [RuntimeState]
        private List<Particle> particles = new List<Particle>();

        #endregion

        #region Properties

        /// <summary>
        /// Identification number.
        /// </summary>
        public int ID
        {
            get { return id; }
            internal set { id = value; }
        }

        /// <summary>
        /// IsAlive is set to false when no particles exist in this system.
        /// </summary>
        public bool IsAlive
        {
            get { return isAlive; }
            set
            {
                if (!isAlive && value)
                {
                    // Forget about creating particles whose creation was due 
                    // while we were dead.
                    PhysicsEngine physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));
                    if (nextBirth < physics.TimeStep.TotalGameTime)
                        nextBirth = physics.TimeStep.TotalGameTime;
                }
                isAlive = value;
            }
        }

        /// <summary>
        /// If set to loop, generator keeps generating infinite number of particles.
        /// </summary>
        public bool Loop
        {
            get { return loop; }
            set { loop = value; }
        }

        /// <summary>
        /// Maximum number of particles to generate.
        /// </summary>
        public int TotalNumberParticles
        {
            get { return totalNumberParticles; }
            set { totalNumberParticles = value; }
        }

        /// <summary>
        /// Speed of particle creation, in particles/second.
        /// </summary>
        public float BirthRate
        {
            get { return birthRate; }
            set { birthRate = value; }
        }

        /// <summary>
        /// Center of the particle system. This is where the emitter will be centered.
        /// </summary>
        public override Vector2 Pos
        {
            set
            {
                oldPosition = Single.IsNaN(oldPosition.X)
                    ? value
                    : pos;
                pos = value;

                if (emitter != null)
                    emitter.Position = new Vector3(value,0);
            }
        }

        /// <summary>
        /// Movement of the particle system.
        /// </summary>
        public Vector3 Movement
        {
            get { return movement; }
            set
            {
                movement = value;
            }
        }

        /// <summary>
        /// Which style of emitter to use when placing new particles.
        /// </summary>
        public Emitter Emitter
        {
            get { return emitter; }
            set
            {
                emitter = value;
                emitter.Position = new Vector3(pos,0);
            }
        }

        /// <summary>
        /// Currently not used. This should be fetched from physics engine.
        /// </summary>
        public float DragForce
        {
            get { return dragForce; }
            set { dragForce = value; }
        }

        /// <summary>
        /// Currently not used. This should be fetched from physics engine.
        /// </summary>
        public Vector3 Gravity
        {
            get { return gravity; }
            set { gravity = value; }
        }

        /// <summary>
        /// Name of the texture to draw each particle with.
        /// </summary>
        public string TextureName
        {
            get { return textureName; }
            set { textureName = value; }
        }

        /// <summary>
        /// Names of all textures that this gob type will ever use.
        /// </summary>
        public override List<string> TextureNames
        {
            get
            {
                List<string> textureNames = base.TextureNames;
                textureNames.Add(textureName);
                return textureNames;
            }
        }

        /// <summary>
        /// The leader who the particle system follows,
        /// or <b>null</b> if the particle system is not following anyone.
        /// </summary>
        public Gob Leader
        {
            get { return leader; }
            set { leader = value; }
        }

        /// <summary>
        /// Expected time in seconds that the created particles will live.
        /// </summary>
        public ExpectedValue ParticleAge
        {
            get { return particleAge; }
            set { particleAge = value; }
        }

        /// <summary>
        /// Size of the particle when it's created. This is used to scale the particle sprite.
        /// </summary>
        public ExpectedValue ParticleInitialSize
        {
            get { return particleInitialSize; }
            set { particleInitialSize = value; }
        }

        /// <summary>
        /// Size of the particle just before it dies. This is used to scale the particle sprite.
        /// </summary>
        public ExpectedValue ParticleFinalSize
        {
            get { return particleFinalSize; }
            set { particleFinalSize = value; }
        }

        /// <summary>
        /// Particles expected rotation speed.
        /// </summary>
        public ExpectedValue ParticleRotationSpeed
        {
            get { return particleRotationSpeed; }
            set { particleRotationSpeed = value; }
        }

        /// <summary>
        /// Color of the particle when it's created. This is used to colorize the particle sprite.
        /// </summary>
        public Color ParticleInitialColor
        {
            get { return particleInitialColor; }
            set { particleInitialColor = value; }
        }

        /// <summary>
        /// Color of the particle just before it dies. This is used to colorize the particle sprite.
        /// </summary>
        public Color ParticleFinalColor
        {
            get { return particleFinalColor; }
            set { particleFinalColor = value; }
        }

        /// <summary>
        /// Particles speed when it's created.
        /// </summary>
        public ExpectedValue ParticleSpeed
        {
            get { return particleSpeed; }
            set { particleSpeed = value; }
        }

        /// <summary>
        /// Particles acceleration when it's created.
        /// </summary>
        public ExpectedValue ParticleAcceleration
        {
            get { return particleAcceleration; }
            set { particleAcceleration = value; }
        }

        /// <summary>
        /// Particles mass when it's created.
        /// </summary>
        public ExpectedValue ParticleMass
        {
            get { return particleMass; }
            set { particleMass = value; }
        }

        /// <summary>
        /// Living particles created by this system.
        /// </summary>
        public List<Particle> Particles
        {
            get { return particles; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates particle source with default parameters. Should be given an emitter.
        /// </summary>
        public ParticleEngine() {
        }

        /// <summary>
        /// Creates a particle engine of the specified type.
        /// </summary>
        /// The particle engine's serialised fields are initialised according to the template 
        /// instance associated with the named type. This applies also to fields declared
        /// in subclasses, so a subclass constructor only has to initialise its runtime
        /// state fields, not the fields that define its type.
        /// <param name="typeName">The type of the particle engine.</param>
        public ParticleEngine(string typeName)
        {
            // Initialise fields from the gob type's template.
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            ParticleEngine template = (ParticleEngine)data.GetTypeTemplate(typeof(Gob), typeName);
            if (template.GetType() != this.GetType())
                throw new Exception("Silly programmer tries to create a particle engine (type " +
                    typeName + ") using a wrong class (" + this.GetType().Name + ")");
            foreach (FieldInfo field in Serialization.GetFields(this, typeof(TypeParameterAttribute)))
                field.SetValue(this, Serialization.DeepCopy(field.GetValue(template)));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        /// DataEngine will call this method to make the gob do necessary 
        /// initialisations to make it fully functional on addition to 
        /// an ongoing play of the game.
        public override void Activate()
        {
            PhysicsEngine physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));
            nextBirth = physics.TimeStep.TotalGameTime;

            base.Activate();
        }

        /// <summary>
        /// Updating a particle system consists in updating each particle's age, and then
        /// create and destroy particles
        /// </summary>
        public override void Update()
        {
            GameTime gameTime = AssaultWing.Instance.GameTime;

            // Update ourselves
            if (leader != null)
            {
                // Die by our dead leader.
                if (leader.Dead)
                {
                    Die();
                    leader = null;
                }
                else
                {
                    pos = leader.Pos;
                    if (emitter != null && emitter is DotEmitter)
                        ((DotEmitter)emitter).Direction = leader.Rotation;
                }
            }

            //Update the particles
            int j = 0;
            while (j < particles.Count)
            {
                if (particles[j].Age > 0.0f)
                {
                    particles[j].Update(gameTime);
                    j++;
                }
                else
                    particles.RemoveAt(j);
            }

            // Create new particles.
            if (isAlive && (loop || (!loop && (createdParticles < totalNumberParticles))))
            {
                // Count how many to create.
                int createCount = 0;
                while (nextBirth <= gameTime.TotalGameTime)
                {
                    ++createCount;
                    if (birthRate != 0)
                    {
                        long ticks = (long)(10 * 1000 * 1000 / birthRate);
                        nextBirth = nextBirth.Add(new TimeSpan(ticks));
                    }
                }

                if (!loop)
                    createCount = Math.Min(createCount, totalNumberParticles - createdParticles);

                // Create the particles at even spaces between oldPosition and pos.
                Vector2 startPos = this.oldPosition;
                Vector2 endPos = this.pos;
                for (int i = 0; i < createCount; ++i)
                {
                    float weight = (i + 1) / (float)createCount;
                    Vector2 iPos = Vector2.Lerp(startPos, endPos, weight);
                    pos = iPos;
                    emitter.Position = new Vector3(pos, 0);
                    CreateParticle();
                }
            }

            if (!loop && createdParticles >= totalNumberParticles)
                Die();

            // Remove the particle engine if it's created all its particles and
            // the particles have died.
            if (!loop && !isAlive && particles.Count == 0)
            {
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                data.RemoveParticleEngine(this);
            }
        }

        
        /// <summary>
        /// Kills the gob, i.e. performs a death ritual and removes the gob from the game world.
        /// </summary>
        public override void Die()
        {
            IsAlive = false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a new particle and sets it's properties
        /// </summary>
        private void CreateParticle()
        {
            Particle particle = new Particle();
            Vector3 position, direction;

            // Particle creation
            particle.Age = particleAge.GetRandomValue();

            particle.InitialSize = particleInitialSize.GetRandomValue();
            particle.FinalSize = particleFinalSize.GetRandomValue();
            
            // Log.Write("created particle with age: " + particle.Age + " init size: " + particle.InitialSize);

            particle.DeltaRotation = particleRotationSpeed.GetRandomValue();
            particle.Rotation = MathHelper.TwoPi * RandomHelper.GetRandomFloat();

            particle.InitialColor = particleInitialColor;
            particle.FinalColor = particleFinalColor;

            emitter.EmittPosition(out position, out direction);

            particle.Position = position + new Vector3(this.Pos, 0);
            particle.Velocity = movement + direction * particleSpeed.GetRandomValue();
            particle.Acceleration = direction * particleAcceleration.GetRandomValue();
            particle.Mass = particleMass.GetRandomValue();

            particle.Parent = this;

            // Add the particle to the system
            particles.Add(particle);
            createdParticles++;
        }

        #endregion

        /// <summary>
        /// Draws the particles managed by the particle engine.
        /// </summary>
        /// Assumes that the sprite batch has been Begun already and will be
        /// Ended later by someone else.
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="spriteBatch">The sprite batch to draw particles with.</param>
        public override void Draw(Matrix view, Matrix projection, SpriteBatch spriteBatch)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Texture2D tex = data.GetTexture(textureName);
            Viewport gfxViewport = AssaultWing.Instance.GraphicsDevice.Viewport;
            Vector3 viewportSize = new Vector3(gfxViewport.Width, gfxViewport.Height, gfxViewport.MaxDepth - gfxViewport.MinDepth);
            Matrix transform = view * projection
                * Matrix.CreateReflection(new Plane(Vector3.UnitY, 0))
                * Matrix.CreateTranslation(1, 1, 0)
                * Matrix.CreateScale(viewportSize / 2);
            
            foreach (Particle part in particles)
            {
                // Transform world coordinates to viewport coordinates that
                // range from (-1,-1,0) to (1,1,1).
                Vector3 posCenter = part.Position;
                Vector3 diagonal = new Vector3(tex.Width * part.Size, -tex.Height * part.Size, 0);
                Vector3 posTopLeft = posCenter - diagonal / 2;
                Vector3 posBotRight = posCenter + diagonal / 2;
                Vector3 screenCenter = Vector3.Transform(posCenter, transform);
                Vector3 screenTopLeft = Vector3.Transform(posTopLeft, transform);
                Vector3 screenBotRight = Vector3.Transform(posBotRight, transform);

                // Sprite depth will be our given depth layer slightly adjusted by
                // particle's position in its lifespan.
                float lifepos = MathHelper.Clamp(1 - (part.Age / part.TotalAge), 0, 1); // 0 = born; 1 = dead
                float layerDepth = MathHelper.Clamp(depthLayer * 0.99f + 0.0099f * lifepos, 0, 1);
                spriteBatch.Draw(tex, new Vector2(screenCenter.X, screenCenter.Y), null,
                    part.CurrentColor, part.Rotation,
                    new Vector2(tex.Width, tex.Height) / 2, part.Size, SpriteEffects.None, layerDepth);
            }
        }
    }
}
