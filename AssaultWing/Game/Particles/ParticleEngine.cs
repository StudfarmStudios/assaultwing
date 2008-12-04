using System;
using System.Collections.Generic;
using System.Reflection;
using AW2.Graphics;
using AW2.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game.Particles
{
    /// <summary>
    /// Represents a group of particles with a specific behavior
    /// </summary>
    [LimitedSerialization]
    public class ParticleEngine : Gob, IConsistencyCheckable
    {
        #region Fields

        //SYSTEM SETTINGS 
        [RuntimeState]
        private bool isAlive = true;

        [TypeParameter]
        private bool loop = false; //If the particle system is infinite or not
        [TypeParameter]
        private int totalNumberParticles = 0; //Particles emitted in the total lifetime of the system
        private int createdParticles = 0; //Number of emitted particles so far
        [TypeParameter]
        private FloatFactory birthRate = new ExpectedValue(5,0); //Particles emitted per second
        private TimeSpan nextBirth; // Time of next particle birth, in game time.
        private Vector2 oldPosition = new Vector2(Single.NaN); //Previous position of the system
        [TypeParameter]
        private Emitter emitter = new DotEmitter(); //Emitter of the system
        [TypeParameter]
        private string textureName = "dummytexture"; // Name of texture for particles

        Gob leader = null; // The leader who we follow, or null.
        float argument = 0; // Argument for FloatFactory instances that need it.

        //PARTICLES DATA
        [TypeParameter]
        private FloatFactory particleAge = new ExpectedValue(2, 0); // Expected age of a particle

        [TypeParameter]
        private FloatFactory particleInitialSize = new ExpectedValue(1, 0); //Initial size of a particle

        [TypeParameter]
        private FloatFactory particleFinalSize = new ExpectedValue(2, 0); //Final size of a particle

        [TypeParameter]
        private FloatFactory particleInitialRotation = new ExpectedValue(0, MathHelper.Pi); // Expected initial rotation of a particle

        [TypeParameter]
        private FloatFactory particleRotationSpeed = new ExpectedValue(); // Expected rotation speed of a particle

        [TypeParameter]
        private Color particleInitialColor = new Color(255, 255, 255, 255); //Initial color of the particle

        [TypeParameter]
        private Color particleFinalColor = new Color(0, 0, 0, 0); //Final color of the particle

        [TypeParameter]
        private FloatFactory particleSpeed = new ExpectedValue(1, 0); //Expected speed of a particle

        [TypeParameter]
        private FloatFactory particleAcceleration = new ExpectedValue(); //Expected acceleration of a particle

        private List<Particle> particles = new List<Particle>();

        #endregion

        #region Properties

        /// <summary>
        /// IsAlive is <c>true</c> if and only if the particle engine is producing new particles.
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
        public FloatFactory BirthRate
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
                    emitter.Position = value;
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
                emitter.Position = pos;
            }
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
        /// Argument for particle parameters that need it.
        /// </summary>
        public float Argument { get { return argument; } set { argument = value; } }

        /// <summary>
        /// Expected time in seconds that the created particles will live.
        /// </summary>
        public FloatFactory ParticleAge
        {
            get { return particleAge; }
            set { particleAge = value; }
        }

        /// <summary>
        /// Size of the particle when it's created. This is used to scale the particle sprite.
        /// </summary>
        public FloatFactory ParticleInitialSize
        {
            get { return particleInitialSize; }
            set { particleInitialSize = value; }
        }

        /// <summary>
        /// Size of the particle just before it dies. This is used to scale the particle sprite.
        /// </summary>
        public FloatFactory ParticleFinalSize
        {
            get { return particleFinalSize; }
            set { particleFinalSize = value; }
        }

        /// <summary>
        /// Particles expected rotation speed.
        /// </summary>
        public FloatFactory ParticleRotationSpeed
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
        public FloatFactory ParticleSpeed
        {
            get { return particleSpeed; }
            set { particleSpeed = value; }
        }

        /// <summary>
        /// Particles acceleration when it's created.
        /// </summary>
        public FloatFactory ParticleAcceleration
        {
            get { return particleAcceleration; }
            set { particleAcceleration = value; }
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
        /// Creates an uninitialised particle engine.
        /// </summary>
        /// This constructor is only for serialisation.
        public ParticleEngine()
        {
            // Set a better default than class Gob does.
            DrawMode2D = new DrawMode2D(DrawModeType2D.Transparent);

            // Remove default collision areas set by class Gob so that we don't need to explicitly state
            // in each particle engine's XML definition that there are no collision areas.
            collisionAreas = new CollisionArea[0];
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
                    Die(new DeathCause());
                    leader = null;
                }
                else
                {
                    this.Pos = leader.Pos;
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
                    float birthRateNow = GetFloatFromFactory(ref birthRate);
                    if (birthRateNow != 0)
                    {
                        long ticks = (long)(TimeSpan.TicksPerSecond / birthRateNow);
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
                    emitter.Position = pos;
                    CreateParticle();
                }
            }

            // Remove the particle engine if it's created all its particles and
            // the particles have died.
            if (!loop && createdParticles >= totalNumberParticles && particles.Count == 0)
            {
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                Die(new DeathCause());
            }
        }

        
        /// <summary>
        /// Kills the gob, i.e. performs a death ritual and removes the gob from the game world.
        /// </summary>
        /// <param name="cause">The cause of death.</param>
        public override void Die(DeathCause cause)
        {
            base.Die(cause);
            IsAlive = false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a new particle and sets its properties.
        /// </summary>
        private void CreateParticle()
        {
            Particle particle = new Particle();
            Vector2 position, direction;
            float directionAngle;

            emitter.EmittPosition(out position, out direction, out directionAngle);

            // Particle creation
            particle.Age = GetFloatFromFactory(ref particleAge);

            particle.InitialSize = GetFloatFromFactory(ref particleInitialSize);
            particle.FinalSize = GetFloatFromFactory(ref particleFinalSize);

            particle.DeltaRotation = GetFloatFromFactory(ref particleRotationSpeed);
            particle.Rotation = directionAngle + GetFloatFromFactory(ref particleInitialRotation);

            particle.InitialColor = particleInitialColor;
            particle.FinalColor = particleFinalColor;

            particle.Position = position + this.pos;
            particle.Velocity = this.move + direction * GetFloatFromFactory(ref particleSpeed);
            particle.Acceleration = direction * GetFloatFromFactory(ref particleAcceleration);

            particle.Parent = this;

            // Add the particle to the system
            particles.Add(particle);
            createdParticles++;
        }

        private float GetFloatFromFactory(ref FloatFactory floatFactory)
        {
            switch (floatFactory.ArgumentCount)
            {
                case 0: return floatFactory.GetValue();
                case 1: return floatFactory.GetValue(argument);
                default: throw new ArgumentException("Float factory requires " + floatFactory.ArgumentCount + " arguments");
            }
        }

        #endregion

        /// <summary>
        /// Draws the gob's 3D graphics.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public override void Draw(Matrix view, Matrix projection)
        {
            // Peng has no 3D graphics.
        }

        /// <summary>
        /// Draws the gob's 2D graphics.
        /// </summary>
        /// Assumes that the sprite batch has been Begun already and will be
        /// Ended later by someone else.
        /// <param name="gameToScreen">Transformation from game coordinates 
        /// to screen coordinates (pixels).</param>
        /// <param name="spriteBatch">The sprite batch to draw sprites with.</param>
        /// <param name="scale">Scale of graphics.</param>
        public override void Draw2D(Matrix gameToScreen, SpriteBatch spriteBatch, float scale)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Texture2D tex = data.GetTexture(textureName);
            Matrix transform = gameToScreen;
            
            foreach (Particle part in particles)
            {
                // Transform world coordinates to viewport coordinates that
                // range from (-1,-1,0) to (1,1,1).
                Vector3 posCenter = new Vector3(part.Position, 0);
                Vector3 screenCenter = Vector3.Transform(posCenter, transform);

                // Sprite depth will be our given depth layer slightly adjusted by
                // particle's position in its lifespan.
                float lifepos = MathHelper.Clamp(1 - (part.Age / part.TotalAge), 0, 1); // 0 = born; 1 = dead
                float layerDepth = MathHelper.Clamp(DepthLayer2D * 0.99f + 0.0099f * lifepos, 0, 1);
                spriteBatch.Draw(tex, new Vector2(screenCenter.X, screenCenter.Y), null,
                    part.CurrentColor, part.Rotation,
                    new Vector2(tex.Width, tex.Height) / 2, part.Size * scale, SpriteEffects.None, layerDepth);
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
        public override void MakeConsistent(Type limitationAttribute)
        {
            base.MakeConsistent(limitationAttribute);

            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                // Make sure there's no null references.
                if (birthRate == null)
                    throw new Exception("Serialization error: ParticleEngine birthRate not defined in " + TypeName);
                if (emitter == null)
                    throw new Exception("Serialization error: ParticleEngine emitter not defined in " + TypeName);
                textureName = textureName ?? "dummytexture";
                if (particleAge == null)
                    throw new Exception("Serialization error: ParticleEngine particleAge not defined in " + TypeName);
                if (particleInitialSize == null)
                    throw new Exception("Serialization error: ParticleEngine particleInitialSize not defined in " + TypeName);
                if (particleFinalSize == null)
                    throw new Exception("Serialization error: ParticleEngine particleFinalSize not defined in " + TypeName);
                if (particleInitialRotation == null)
                    throw new Exception("Serialization error: ParticleEngine particleInitialRotation not defined in " + TypeName);
                if (particleRotationSpeed == null)
                    throw new Exception("Serialization error: ParticleEngine particleRotationSpeed not defined in " + TypeName);
                if (particleSpeed == null)
                    throw new Exception("Serialization error: ParticleEngine particleSpeed not defined in " + TypeName);
                if (particleAcceleration == null)
                    throw new Exception("Serialization error: ParticleEngine particleAcceleration not defined in " + TypeName);
            }
            if (limitationAttribute == typeof(RuntimeStateAttribute))
            {
                // Make sure there's no null references.
                particles = particles ?? new List<Particle>();

                // Initialise oldPos
                Pos = Pos;
            }
        }

        #endregion IConsistencyCheckable Members
    }
}
