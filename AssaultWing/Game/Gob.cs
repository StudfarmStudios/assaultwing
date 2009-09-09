using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Particles;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Graphics;
using AW2.Net;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace AW2.Game
{
    /// <summary>
    /// Game object, i.e., a gob.
    /// </summary>
    /// By default, a gob is a single material object in the game world
    /// that follows the laws of physics and displays itself as a 3D model.
    /// A gob's additional functionality is implemented in a subclass.
    ///
    /// There can be several special instances of each subclass of Gob. Each of
    /// these 'template instances' defines a gob type by specifying values for the
    /// type parameters of that Gob subclass. Newly created gob instances automatically
    /// initialise their type parameter fields by copying them from a template instance.
    /// Template instances are referred to by human-readable names such as "rocket pod".
    /// 
    /// Class Gob manages exhaust engines, i.e. particle engines that produce
    /// engine exhaust fumes or similar. Exhaust engines are created automatically,
    /// provided that the gob's 3D model has bones whose name begins with Thruster
    /// and <b>exhaustEngineNames</b> contains at least one valid particle engine name.
    /// All exhaust engines are set on loop and their position and direction
    /// are set each frame. The subclass should manage other parameters of the engines.
    /// 
    /// Class Gob also provides methods required by certain Gob subclasses 
    /// such as those that can be damaged. This serves to keep general code
    /// in one place only.
    /// 
    /// Class Gob and its subclasses use limited (de)serialisation for
    /// for saving and loading gob types. Therefore those fields
    /// that describe the gob type should be marked as 'type parameters' by 
    /// <b>TypeParameterAttribute</b>, and those fields that describe the gob's 
    /// state during gameplay should be marked by <b>RuntimeStateAttribute</b>.
    /// The remaining fields, precalculated data and references to objects that
    /// are not part of the game state should not be marked with either attribute.
    /// 
    /// Each Gob subclass must provide a parameterless constructor that initialises all
    /// of its type parameters to descriptive and exemplary default values.
    /// <see cref="AW2.Helpers.TypeParameterAttribute"/>
    /// <see cref="AW2.Helpers.RuntimeStateAttribute"/>
    [LimitedSerialization]
    [System.Diagnostics.DebuggerDisplay("Id:{Id} typeName:{typeName} pos:{pos} move:{move}")]
    public class Gob : Clonable, IConsistencyCheckable, Net.INetworkSerializable
    {
        /// <summary>
        /// Type of a gob's preferred placement to arena layers.
        /// </summary>
        public enum LayerPreferenceType
        {
            /// <summary>
            /// Place gob to the gameplay layer.
            /// </summary>
            Front,

            /// <summary>
            /// Place gob to the gameplay backlayer.
            /// </summary>
            Back,
        }

        #region Fields for all gobs

        /// <summary>
        /// Default rotation of gobs. Points up in the game world.
        /// </summary>
        protected const float defaultRotation = MathHelper.PiOver2;

        /// <summary>
        /// Time, in seconds, for a gob to stop being cold.
        /// </summary>
        /// <see cref="Gob.Cold"/>
        const float warmUpTime = 0.2f;

        /// <summary>
        /// Least int that is known not to have been used as a gob identifier
        /// on this game instance.
        /// </summary>
        /// This field is used in obtaining identifiers for gobs that can be
        /// shared among all participating game instances (this means
        /// relevant gobs but this field may be used for irrelevant gobs, too).
        /// Such identifiers are positive.
        /// <seealso cref="Gob.Id"/>
        /// <seealso cref="Gob.leastUnusedIrrelevantId"/>
        static int leastUnusedId = 1;

        /// <summary>
        /// Greatest int that is known not to have been used as a gob identifier
        /// on this game instance.
        /// </summary>
        /// This field is used in obtaining identifiers for gobs that are 
        /// local to one game instance (irrelevant gobs). Such identifiers
        /// are negative.
        /// <seealso cref="Gob.Id"/>
        /// <seealso cref="Gob.leastUnusedId"/>
        static int leastUnusedIrrelevantId = -1;

        /// <summary>
        /// Gob type name.
        /// </summary>
        [TypeParameter, RuntimeState]
        CanonicalString typeName;

        /// <summary>
        /// The player who owns the gob. Can be null for impartial gobs.
        /// </summary>
        Player owner;

        /// <summary>
        /// Drawing depth of 2D graphics of the gob, between 0 and 1.
        /// 0 is front, 1 is back.
        /// </summary>
        [TypeParameter]
        float depthLayer2D;

        /// <summary>
        /// Drawing mode of 2D graphics of the gob.
        /// </summary>
        [TypeParameter]
        DrawMode2D drawMode2D;

        /// <summary>
        /// Preferred placement of gob to arena layers.
        /// </summary>
        [TypeParameter]
        LayerPreferenceType layerPreference;

        /// <summary>
        /// Position of the gob in the game world.
        /// </summary>
        [RuntimeState]
        protected Vector2 pos;

        /// <summary>
        /// Movement vector of the gob.
        /// </summary>
        [RuntimeState]
        protected Vector2 move;

        /// <summary>
        /// Gob rotation around the Z-axis in radians.
        /// </summary>
        [RuntimeState]
        float rotation;

        /// <summary>
        /// Mass of the gob, measured in kilograms.
        /// </summary>
        /// Larger mass needs more force to be put in motion. 
        [TypeParameter]
        float mass;

        /// <summary>
        /// Name of the 3D model of the gob. The name indexes the model database in GraphicsEngine.
        /// </summary>
        [TypeParameter]
        CanonicalString modelName;

        /// <summary>
        /// Scaling factor of the 3D model.
        /// </summary>
        [TypeParameter]
        float scale;

        /// <summary>
        /// Amount of alpha to use when drawing the gob's 3D model, between 0 and 1.
        /// </summary>
        float alpha;

        /// <summary>
        /// Types of gobs to create on birth.
        /// </summary>
        [TypeParameter, ShallowCopy]
        CanonicalString[] birthGobTypes;

        /// <summary>
        /// Types of gobs to create on death.
        /// </summary>
        /// You might want to put some gob types of subclass Explosion here.
        [TypeParameter, ShallowCopy]
        CanonicalString[] deathGobTypes;

        /// <summary>
        /// Time of birth of the gob, in game time.
        /// </summary>
        [RuntimeState]
        protected TimeSpan birthTime;

        /// <summary>
        /// True iff the Die() has been called for this gob.
        /// </summary>
        [RuntimeState]
        bool dead;

        /// <summary>
        /// True iff the gob is not regarded in movement and collisions.
        /// </summary>
        [RuntimeState]
        bool disabled;

        /// <summary>
        /// True iff the gob moves around by the laws of physics.
        /// </summary>
        /// Subclasses should set this according to their needs.
        [TypeParameter]
        protected bool movable;

        /// <summary>
        /// True iff the gob's movement is affected by gravity and other forces.
        /// </summary>
        /// Subclasses should set this according to their needs.
        protected bool gravitating;

        /// <summary>
        /// Preferred maximum time between the gob's state updates
        /// from the game server to game clients, in real time.
        /// </summary>
        [TypeParameter]
        TimeSpan networkUpdatePeriod;

        /// <summary>
        /// Access only through <see cref="ModelPartTransforms"/>.
        /// </summary>
        Matrix[] modelPartTransforms;

        /// <summary>
        /// Last time of update of <see cref="modelPartTransforms"/>, in game time.
        /// </summary>
        TimeSpan modelPartTransformsUpdated;

        /// <summary>
        /// Bounding volume of the visuals of the gob, in gob coordinates.
        /// </summary>
        protected BoundingSphere drawBounds;

        #endregion Fields for all gobs

        #region Fields for gobs with thrusters

        /// <summary>
        /// Names of exhaust engine types.
        /// </summary>
        [TypeParameter, ShallowCopy]
        CanonicalString[] exhaustEngineNames;

        /// <summary>
        /// Indices of bones that indicate exhaust engine locations
        /// in the gob's 3D model.
        /// </summary>
        protected int[] exhaustBoneIs;

        /// <summary>
        /// Particle engines that manage exhaust fumes.
        /// </summary>
        protected Gob[] exhaustEngines;

        #endregion Fields for gobs with thrusters

        #region Fields for collisions

        /// <summary>
        /// Collision primitives, translated according to the gob's location.
        /// </summary>
        [TypeParameter]
        protected CollisionArea[] collisionAreas;

        #endregion Fields for collisions

        #region Fields for damage

        /// <summary>
        /// The amount of damage; 0 means perfect condition;
        /// <b>maxDamage</b> means totally destroyed.
        /// </summary>
        [RuntimeState]
        float damage;

        /// <summary>
        /// Maximum amount of sustainable damage.
        /// </summary>
        [TypeParameter]
        float maxDamage;

        #endregion Fields for damage

        #region Fields for bleach

        /// <summary>
        /// Amount of accumulated damage that determines the amount of bleach.
        /// </summary>
        float bleachDamage;

        /// <summary>
        /// Function that maps bleach damage to bleach, i.e. degree of whiteness.
        /// </summary>
        static Curve bleachCurve;

        /// <summary>
        /// Current level of bleach between 0 and 1. Access this field through the property <c>Bleach</c>.
        /// </summary>
        float bleach;

        /// <summary>
        /// Time when bleach will be reset, in game time.
        /// </summary>
        /// When bleach is set to nonzero, this time is set to denote how
        /// long the bleach is supposed to stay on.
        TimeSpan bleachResetTime;

        #endregion Fields for bleach

        #region Gob properties

        /// <summary>
        /// The gob's unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Get the gob type name.
        /// </summary>
        public CanonicalString TypeName { get { return typeName; } }

        /// <summary>
        /// The arena in which the gob lives.
        /// </summary>
        public Arena Arena { get; set; }

        /// <summary>
        /// Is the gob relevant to gameplay. Irrelevant gobs won't receive state updates
        /// from the server when playing over network and they can therefore be created
        /// independently on a client.
        /// </summary>
        public virtual bool IsRelevant { get { return true; } }

        /// <summary>
        /// Drawing depth of 2D graphics of the gob, between 0 and 1.
        /// 0 is front, 1 is back.
        /// </summary>
        public float DepthLayer2D { get { return depthLayer2D; } set { depthLayer2D = value; } }

        /// <summary>
        /// Drawing mode of 2D graphics of the gob.
        /// </summary>
        public DrawMode2D DrawMode2D { get { return drawMode2D; } set { drawMode2D = value; } }

        /// <summary>
        /// Preferred placement of gob to arena layers.
        /// </summary>
        public LayerPreferenceType LayerPreference { get { return layerPreference; } }

        /// <summary>
        /// Bounding volume of the visuals of the gob, in world coordinates.
        /// </summary>
        public virtual BoundingSphere DrawBounds
        {
            get
            {
                return new BoundingSphere(drawBounds.Center.RotateZ(Rotation) + new Vector3(Pos, 0), drawBounds.Radius);
            }
        }

        /// <summary>
        /// The 3D model of the gob.
        /// </summary>
        protected Model Model { get; private set; }

        /// <summary>
        /// Names of all 3D models that this gob type will ever use.
        /// </summary>
        public virtual IEnumerable<CanonicalString> ModelNames
        {
            get { return new List<CanonicalString> { modelName }; }
        }

        /// <summary>
        /// Names of all textures that this gob type will ever use.
        /// </summary>
        public virtual IEnumerable<CanonicalString> TextureNames
        {
            get { return new List<CanonicalString>(); }
        }

        /// <summary>
        /// Get or set the gob position in the game world.
        /// </summary>
        public virtual Vector2 Pos { get { return pos; } set { pos = value; } }

        /// <summary>
        /// Get or set the gob's movement vector.
        /// </summary>
        public virtual Vector2 Move { get { return move; } set { move = value; } }

        /// <summary>
        /// Mass of the gob, measured in kilograms.
        /// </summary>
        public float Mass { get { return mass; } }

        /// <summary>
        /// Get or set the gob's rotation around the Z-axis.
        /// </summary>
        public virtual float Rotation
        {
            get { return rotation; }
            set { rotation = value % MathHelper.TwoPi; }
        }

        /// <summary>
        /// Get the owner of the gob.
        /// </summary>
        public Player Owner { get { return owner; } set { owner = value; } }

        /// <summary>
        /// Arena layer of the gob, or <c>null</c> if uninitialised. Set by <see cref="Arena"/>.
        /// </summary>
        /// Note that if somebody who is not <see cref="Arena"/> sets this value,
        /// it leads to confusion.
        public ArenaLayer Layer { get; set; }

        /// <summary>
        /// Returns the name of the 3D model of the gob.
        /// </summary>
        public CanonicalString ModelName { get { return modelName; } set { modelName = value; } }

        /// <summary>
        /// Get and set the scaling factor of the 3D model.
        /// </summary>
        public float Scale { get { return scale; } set { scale = value; } }

        /// <summary>
        /// Amount of bleach to use when drawing the gob's 3D model, between 0 and 1.
        /// </summary>
        /// A bleach of 0 means the 3D model looks normal. 
        /// A bleach of 1 means the 3D model is drawn totally white.
        /// Anything in between states the amount of blend from the
        /// unbleached 3D model towards the totally white 3D model.
        public float Bleach
        {
            get
            {
                // Reset bleach if it's getting old.
                if (AssaultWing.Instance.GameTime.TotalGameTime >= bleachResetTime)
                    bleach = 0;

                // Set new bleach based on accumulated damage during this frame.
                if (bleachDamage > 0)
                {
                    float newBleach = bleachCurve.Evaluate(bleachDamage);
                    if (newBleach > bleach)
                    {
                        bleach = newBleach;
                        bleachResetTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(0.055);
                    }
                    bleachDamage = 0;
                }

                return bleach;
            }
        }

        /// <summary>
        /// Amount of alpha to use when drawing the gob's 3D model, between 0 and 1.
        /// </summary>
        public float Alpha { get { return alpha; } set { alpha = value; } }

        /// <summary>
        /// Returns the world matrix of the gob, i.e., the translation from
        /// game object coordinates to game world coordinates.
        /// </summary>
        public virtual Matrix WorldMatrix { get { return AWMathHelper.CreateWorldMatrix(scale, rotation, pos); } }

        /// <summary>
        /// The transform matrices of the gob's 3D model parts.
        /// </summary>
        Matrix[] ModelPartTransforms
        {
            get
            {
                if (modelPartTransforms == null || modelPartTransforms.Length != Model.Bones.Count)
                {
                    modelPartTransforms = new Matrix[Model.Bones.Count];
                    modelPartTransformsUpdated = new TimeSpan(-1);
                }
                TimeSpan now = AssaultWing.Instance.GameTime.TotalGameTime;
                if (modelPartTransformsUpdated < now)
                {
                    modelPartTransformsUpdated = now;
                    CopyAbsoluteBoneTransformsTo(Model, modelPartTransforms);
                }
                return modelPartTransforms;
            }
        }

        /// <summary>
        /// The collision areas of the gob.
        /// </summary>
        public CollisionArea[] CollisionAreas { get { return collisionAreas; } }

        /// <summary>
        /// Is the gob cold.
        /// </summary>
        /// All gobs are born <b>cold</b>. If a gob is cold, it won't
        /// collide with other gobs that have the same owner. This works around
        /// the problem of bullets hitting the firing ship immediately at birth.
        public virtual bool Cold { get { return birthTime.SecondsAgo() < warmUpTime; } }

        /// <summary>
        /// Is the gob dead, i.e. has Die been called for this gob.
        /// </summary>
        /// If you hold references to gobs, do check every now and then if the gobs
        /// are dead and remove the references if they are.
        public bool Dead { get { return dead; } }

        /// <summary>
        /// Is the gob disabled. A disabled gob is not regarded in movement and collisions.
        /// </summary>
        public bool Disabled { get { return disabled; } set { disabled = value; } }

        /// <summary>
        /// True iff the gob moves around by the laws of physics.
        /// </summary>
        public bool Movable { get { return movable; } }

        /// <summary>
        /// True iff the gob's movement is affected by gravity and other forces.
        /// </summary>
        public bool Gravitating { get { return gravitating; } }

        #endregion Gob Properties

        #region Network properties

        /// <summary>
        /// Preferred maximum time between the gob's state updates
        /// from the game server to game clients, in real time.
        /// </summary>
        public TimeSpan NetworkUpdatePeriod { get { return networkUpdatePeriod; } set { networkUpdatePeriod = value; } }

        /// <summary>
        /// Time of last network update, in real time. Used only on the game server.
        /// </summary>
        public TimeSpan LastNetworkUpdate { get; set; }

        #endregion Network properties

        #region Gob static and instance constructors, and static constructor-like methods

        static Gob()
        {
            // Check that important constructors have been declared
            Helpers.Log.Write("Checking gob constructors");
            foreach (Type type in Array.FindAll<Type>(System.Reflection.Assembly.GetExecutingAssembly().GetTypes(),
                t => typeof(Gob).IsAssignableFrom(t) && !t.IsAbstract))
            {
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
                if (null == type.GetConstructor(flags, null, Type.EmptyTypes, null))
                {
                    string message = "Missing constructor " + type.Name + "()";
                    Log.Write(message);
                    throw new Exception(message);
                }
                if (null == type.GetConstructor(flags, null, new Type[] { typeof(CanonicalString) }, null))
                {
                    string message = "Missing constructor " + type.Name + "(CanonicalString)";
                    Log.Write(message);
                    throw new Exception(message);
                }
            }

            // Initialise bleach curve.
            bleachCurve = new Curve();
            bleachCurve.PreLoop = CurveLoopType.Constant;
            bleachCurve.PostLoop = CurveLoopType.Constant;
            bleachCurve.Keys.Add(new CurveKey(0, 0));
            bleachCurve.Keys.Add(new CurveKey(5, 0.1f));
            bleachCurve.Keys.Add(new CurveKey(30, 0.3f));
            bleachCurve.Keys.Add(new CurveKey(100, 0.5f));
            bleachCurve.Keys.Add(new CurveKey(200, 0.65f));
            bleachCurve.Keys.Add(new CurveKey(500, 0.8f));
            bleachCurve.Keys.Add(new CurveKey(1000, 0.9f));
            bleachCurve.Keys.Add(new CurveKey(5000, 1));
            bleachCurve.ComputeTangents(CurveTangent.Linear);
        }

        /// <summary>
        /// Creates an uninitialised gob.
        /// </summary>
        /// This constructor is only for serialisation.
        /// In their parameterless constructors, subclasses should initialise
        /// all their fields marked with any limitation attribute, setting their
        /// values to representative defaults for XML templates.
        public Gob()
        {
            // We initialise the values so that they work as good examples in the XML template.
            Id = -1;
            typeName = (CanonicalString)"unknown gob type";
            depthLayer2D = 0.5f;
            drawMode2D = new DrawMode2D(DrawModeType2D.None);
            layerPreference = LayerPreferenceType.Front;
            owner = null;
            pos = Vector2.Zero;
            move = Vector2.Zero;
            rotation = 0;
            mass = 1;
            modelName = (CanonicalString)"dummymodel";
            scale = 1f;
            birthGobTypes = new CanonicalString[0];
            deathGobTypes = new CanonicalString[0];
            collisionAreas = new CollisionArea[0];
            damage = 0;
            maxDamage = 100;
            bleachDamage = 0;
            birthTime = new TimeSpan(23, 59, 59);
            dead = false;
            disabled = false;
            movable = true;
            modelPartTransforms = null;
            exhaustEngineNames = new CanonicalString[0];
        }

        /// <summary>
        /// Creates a gob of the specified gob type.
        /// </summary>
        /// The gob's serialised fields are initialised according to the gob template 
        /// instance associated with the gob type. This applies also to fields declared
        /// in subclasses, so a subclass constructor only has to initialise its runtime
        /// state fields, not the fields that define its gob type.
        /// <param name="typeName">The type of the gob.</param>
        protected Gob(CanonicalString typeName)
        {
            this.typeName = typeName;
            gravitating = true;
            SetId();
            owner = null;
            Pos = Vector2.Zero; // also translates collPrimitives
            move = Vector2.Zero;
            rotation = Gob.defaultRotation;
            birthTime = AssaultWing.Instance.GameTime.TotalGameTime;
            modelPartTransforms = null;
            exhaustEngines = new Gob[0];
            alpha = 1;
            bleachDamage = 0;
            bleach = -1;
            bleachResetTime = new TimeSpan(0);
            LastNetworkUpdate = AssaultWing.Instance.GameTime.TotalGameTime;
        }

        /// <summary>
        /// Creates unconditionally a gob of a given type. Don't call this method from 
        /// common game logic. Call <c>CreateGob(string, Action&lt;Gob&gt;)</c> instead.
        /// </summary>
        /// Note that you cannot call new Gob(typeName) because then the created object
        /// won't have the fields of the subclass that 'typeName' requires. This static method
        /// takes care of finding the correct subclass.
        /// <param name="typeName">The type of the gob.</param>
        /// <returns>The newly created gob.</returns>
        /// <seealso cref="CreateGob(CanonicalString, Action&lt;Gob&gt;)"/>
        public static Gob CreateGob(CanonicalString typeName)
        {
            AssaultWing.Instance.GobsCreatedPerFrameAvgPerSecondCounter.Increment();
            Gob template = (Gob)AssaultWing.Instance.DataEngine.GetTypeTemplate(TypeTemplateType.Gob, typeName);
            return (Gob)template.Clone();
        }

        /// <summary>
        /// Creates a gob of a given type and performs a given initialisation on it.
        /// This method is for game logic; gob init is skipped appropriately on clients.
        /// </summary>
        /// Note that you cannot call new Gob(typeName) because then the created object
        /// won't have the fields of the subclass that 'typeName' requires. This static method
        /// takes care of finding the correct subclass.
        /// 
        /// In order for a call to this method to be meaningful, <c>init</c>
        /// should contain a call to <c>DataEngine.AddGob</c> or similar method.
        /// <param name="typeName">The type of the gob.</param>
        /// <param name="init">Initialisation to perform on the gob.</param>
        /// <seealso cref="CreateGob(CanonicalString)"/>
        public static void CreateGob(CanonicalString typeName, Action<Gob> init)
        {
            Gob gob = CreateGob(typeName);
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Client || !gob.IsRelevant)
                init(gob);
        }

        /// <summary>
        /// Creates unconditionally a new gob from the given runtime state.
        /// </summary>
        /// Use this method to revive gobs whose runtime state you have deserialised.
        /// This method will create the gob properly, initialising all fields and then
        /// copying the runtime state fields to the new instance.
        /// <param name="runtimeState">The runtime state from where to initialise the new gob.</param>
        /// <returns>The newly created gob.</returns>
        /// <seealso cref="CreateGob(Gob, Action&lt;Gob&gt;)"/>
        private static Gob CreateGob(Gob runtimeState)
        {
            Gob gob = CreateGob(runtimeState.TypeName);
            if (runtimeState.GetType() != gob.GetType())
                throw new ArgumentException("Runtime gob of class " + runtimeState.GetType().Name +
                    " has type name \"" + runtimeState.typeName + "\" which is for class " + gob.GetType().Name);
            gob.SetRuntimeState(runtimeState);
            return gob;
        }

        /// <summary>
        /// Creates a new gob from the given runtime state and performs a given initialisation on it.
        /// This method is for game logic; gob init is skipped appropriately on clients.
        /// </summary>
        /// Use this method to revive gobs whose runtime state you have deserialised.
        /// This method will create the gob properly, initialising all fields and then
        /// copying the runtime state fields to the new instance.
        /// 
        /// In order for a call to this method to be meaningful, <c>init</c>
        /// should contain a call to <c>DataEngine.AddGob</c> or similar method.
        /// <param name="runtimeState">The runtime state from where to initialise the new gob.</param>
        /// <returns>The newly created gob.</returns>
        /// <param name="init">Initialisation to perform on the gob.</param>
        /// <seealso cref="CreateGob(Gob)"/>
        public static void CreateGob(Gob runtimeState, Action<Gob> init)
        {
            Gob gob = CreateGob(runtimeState);
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Client || !gob.IsRelevant)
                init(gob);
        }

        #endregion Gob constructors and static constructor-like methods

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public virtual void LoadContent()
        {
            Model = AssaultWing.Instance.Content.Load<Model>(ModelName);
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public virtual void UnloadContent()
        {
        }

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        /// DataEngine will call this method to make the gob do necessary 
        /// initialisations to make it fully functional on addition to 
        /// an ongoing play of the game.
        public virtual void Activate()
        {
            LoadContent();
            if (Arena.IsForPlaying)
            {
                InitializeModelCollisionAreas();
                TransformUnmovableCollisionAreas();
                CreateBirthGobs();
                CreateModelBirthGobs();
                CreateExhaustEngines();
            }

            // Create draw bounding volume
            VertexPositionNormalTexture[] vertexData;
            short[] indexData;
            Graphics3D.GetModelData(Model, out vertexData, out indexData);
            drawBounds = BoundingSphere.CreateFromPoints(vertexData.Select(v => v.Position * scale));
        }

        /// <summary>
        /// Updates the gob according to physical laws.
        /// </summary>
        /// Overriden Update methods should explicitly call this method in order to have 
        /// physical laws apply to the gob and the gob's exhaust engines updated.
        public virtual void Update()
        {
            Arena.Move(this);
            UpdateExhaustEngines();
        }

        /// <summary>
        /// Kills the gob, i.e. performs a death ritual and removes the gob from the game world.
        /// </summary>
        /// Call this method to make the gob die like it would during normal gameplay.
        /// Alternatively, if you want to just make the gob disappear, you can simply
        /// remove it from the game data. But this might be a bad idea later on if gobs
        /// refer to each other.
        /// Overriding methods should not do anything if the property <b>Dead</b> is true.
        /// <param name="cause">The cause of death.</param>
        public virtual void Die(DeathCause cause)
        {
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client && IsRelevant) return;
            DieImpl(cause, false);
        }

        /// <summary>
        /// Kills the gob, i.e. performs a death ritual and removes the gob from the game world.
        /// Compared to <see cref="Die(DeathCause)"/>, this method forces death on game clients.
        /// Therefore this method is to be called only when interpreting the game server's kill
        /// messages.
        /// </summary>
        /// <seealso cref="Die(DeathCause)"/>
        public void DieOnClient()
        {
            DieImpl(new DeathCause(), true);
        }

        /// <summary>
        /// Releases all resources allocated by the gob.
        /// </summary>
        public virtual void Dispose()
        {
            // Remove exhaust engines that are not Pengs.
            // Pengs will die automatically because we are their Leader.
            foreach (Gob exhaustEngine in exhaustEngines)
                if (!(exhaustEngine is Gobs.Peng))
                    exhaustEngine.Die(new DeathCause());
            UnloadContent();
        }

        /// <summary>
        /// Draws the gob's 3D graphics.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public virtual void Draw(Matrix view, Matrix projection)
        {
            Matrix world = WorldMatrix;

            // Draw each mesh in the 3D model.
            foreach (ModelMesh mesh in Model.Meshes)
            {
                if (mesh.Name.StartsWith("mesh_Collision"))
                    continue;

                // Apply alpha.
                float oldAlpha = 1;
                if (alpha < 1)
                {
                    // For now we assume only one ModelMeshPart. (Laziness.)
                    if (mesh.Effects.Count > 1)
                        throw new Exception("Error: Several effects on a gob with alpha effect. Programmer must use arrays for saving BasicEffect state.");
                    BasicEffect be = (BasicEffect)mesh.Effects[0];
                    oldAlpha = be.Alpha;
                    be.Alpha = alpha;

                    // Modify render state.
                    AssaultWing.Instance.GraphicsDevice.RenderState.AlphaBlendEnable = true;
                }

                foreach (BasicEffect be in mesh.Effects)
                {
                    AssaultWing.Instance.DataEngine.PrepareEffect(be);
                    be.Projection = projection;
                    be.View = view;
                    be.World = ModelPartTransforms[mesh.ParentBone.Index] * world;
                }
                mesh.Draw();

                // Undo alpha application.
                if (alpha < 1)
                {
                    // For now we assume only one ModelMeshPart. (Laziness.)
                    BasicEffect be = (BasicEffect)mesh.Effects[0];
                    be.Alpha = oldAlpha;

                    // Restore render state.
                    AssaultWing.Instance.GraphicsDevice.RenderState.AlphaBlendEnable = false;
                }

                // Blend towards white if required.
                if (Bleach > 0)
                {
                    // For now we assume only one ModelMeshPart. (Laziness.)
                    if (mesh.Effects.Count > 1)
                        throw new Exception("Error: Several effects on a flashing gob. Programmer must use arrays for saving BasicEffect state.");
                    BasicEffect be = (BasicEffect)mesh.Effects[0];

                    // Modify render state.
                    RenderState renderState = AssaultWing.Instance.GraphicsDevice.RenderState;
                    renderState.AlphaBlendEnable = true;
                    renderState.DepthBufferEnable = false;

                    // Save effect state.
                    bool oldLightingEnabled = be.LightingEnabled;
                    bool oldTextureEnabled = be.TextureEnabled;
                    bool oldVertexColorEnabled = be.VertexColorEnabled;
                    Vector3 oldDiffuseColor = be.DiffuseColor;
                    oldAlpha = be.Alpha;

                    // Set effect state to bleach.
                    be.LightingEnabled = false;
                    be.TextureEnabled = false;
                    be.VertexColorEnabled = false;
                    be.DiffuseColor = Vector3.One;
                    be.Alpha = Bleach;

                    mesh.Draw();

                    // Restore original effect state.
                    be.LightingEnabled = oldLightingEnabled;
                    be.TextureEnabled = oldTextureEnabled;
                    be.VertexColorEnabled = oldVertexColorEnabled;
                    be.DiffuseColor = oldDiffuseColor;
                    be.Alpha = oldAlpha;

                    // Restore render state.
                    renderState.AlphaBlendEnable = false;
                    renderState.DepthBufferEnable = true;
                }
            }
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
        public virtual void Draw2D(Matrix gameToScreen, SpriteBatch spriteBatch, float scale)
        {
            // No 2D graphics by default.
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        /// <summary>
        /// Copies the gob's runtime state from another gob.
        /// </summary>
        /// All value type fields (also those declared in Gob subclasses) that are 
        /// marked with RuntimeStateAttribute are automatically copied.
        /// Subclasses overriding this method must also call their base class' method.
        /// <param name="runtimeState">The gob whose runtime state to imitate.</param>
        protected virtual void SetRuntimeState(Gob runtimeState)
        {
            FieldInfo[] fields = Serialization.GetFields(this, typeof(RuntimeStateAttribute));
            foreach (FieldInfo field in fields)
            {
                // TODO: Watch out for shallow copies sharing references.
                object value = field.GetValue(runtimeState);
                ICloneable cloneableValue = value as ICloneable;
                if (cloneableValue != null)
                    field.SetValue(this, cloneableValue.Clone());
                else
                    field.SetValue(this, value);
            }
        }

        /// <summary>
        /// Serialises the gob to a binary writer.
        /// </summary>
        /// Subclasses should call the base implementation
        /// before performing their own serialisation.
        /// <param name="writer">The writer where to write the serialised data.</param>
        /// <param name="mode">Which parts of the gob to serialise.</param>
        public virtual void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)Id);
                if (owner != null)
                    writer.Write((int)owner.Id);
                else
                    writer.Write((int)-1);
            }
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((float)pos.X);
                writer.Write((float)pos.Y);
                writer.Write((Half)move.X);
                writer.Write((Half)move.Y);
                writer.Write((Half)rotation);
            }
        }

        /// <summary>
        /// Deserialises the gob from a binary writer.
        /// </summary>
        /// Subclasses should call the base implementation
        /// before performing their own deserialisation.
        /// <param name="reader">The reader where to read the serialised data.</param>
        /// <param name="mode">Which parts of the gob to deserialise.</param>
        public virtual void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode)
        {
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                Id = reader.ReadInt32();
                int ownerId = reader.ReadInt32();
                owner = AssaultWing.Instance.DataEngine.Players.FirstOrDefault(player => player.Id == ownerId);
            }
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                Vector2 newPos = new Vector2 { X = reader.ReadSingle(), Y = reader.ReadSingle() };
                pos = newPos;
                Vector2 newMove = new Vector2 { X = reader.ReadHalf(), Y = reader.ReadHalf() };
                move = newMove;
                rotation = reader.ReadHalf();
            }
        }

        #endregion Methods related to serialisation

        #region Gob public methods

        /// <summary>
        /// Returns the game world location of a named position on the gob's 3D model.
        /// </summary>
        /// Named positions are defined in the gob's 3D model by specially named
        /// ModelBones (a.k.a. Frames in the X file). An external object that is
        /// positioned at a named place on the ship -- such as a weapon -- is 
        /// given a bone index at its creation, and passing that index to this method
        /// the external object can find out where in the game world it is located.
        /// <see cref="GetNamedPositions(string)"/>
        /// <param name="boneIndex">The bone index of the named position.</param>
        /// <returns>The game world location of the named position.</returns>
        public Vector2 GetNamedPosition(int boneIndex)
        {
            return Vector2.Transform(Vector2.Zero, ModelPartTransforms[boneIndex] * WorldMatrix);
        }

        /// <summary>
        /// Returns a list of named positions in the gob's 3D model with bone indices
        /// for later calls to <b>GetNamedPosition(int)</b>.
        /// </summary>
        /// <see cref="GetNamedPosition(int)"/>
        /// <param name="namePrefix">Prefix for names of positions to return.</param>
        /// <returns>A list of position names and bone indices in the gob's 3D model.</returns>
        public KeyValuePair<string, int>[] GetNamedPositions(string namePrefix)
        {
            List<KeyValuePair<string, int>> boneIs = new List<KeyValuePair<string, int>>();
            foreach (ModelBone bone in Model.Bones)
                if (bone.Name != null && bone.Name.StartsWith(namePrefix))
                    boneIs.Add(new KeyValuePair<string, int>(bone.Name, bone.Index));
            return boneIs.ToArray();
        }

        /// <summary>
        /// Makes the gob forget its collision areas. Doesn't unregister the collision
        /// areas from <c>PhysicsEngine</c>.
        /// </summary>
        /// This method is to be called only before the gob has been registered
        /// to <c>PhysicsEngine</c>. This method is only a hack to allow putting
        /// any gob in a background arena layer.
        public void ClearCollisionAreas()
        {
            collisionAreas = new CollisionArea[0];
        }

        #endregion Gob public methods

        #region Gob methods related to thrusters

        /// <summary>
        /// Creates exhaust engines for the gob in all thrusters named in its 3D model.
        /// </summary>
        /// A subclass should call this method on <b>Activate</b> 
        /// if it wants to have visible thrusters,
        /// and then do final adjustments on the newly created <b>exhaustEngines</b>.
        /// The subclass should regularly <b>Update</b> its <b>exhaustEngines</b>.
        private void CreateExhaustEngines()
        {
            KeyValuePair<string, int>[] boneIs = GetNamedPositions("Thruster");

            // Create proper exhaust engines.
            int templates = exhaustEngineNames.Length;
            List<int> exhaustBoneIList = new List<int>();
            List<Gob> exhaustEngineList = new List<Gob>();
            for (int thrustI = 0; thrustI < boneIs.Length; ++thrustI)
                for (int tempI = 0; tempI < templates; ++tempI)
                {
                    Gob.CreateGob(exhaustEngineNames[tempI], gob =>
                    {
                        if (gob is ParticleEngine)
                        {
                            ParticleEngine peng = (ParticleEngine)gob;
                            peng.Loop = true;
                            peng.IsAlive = true;
                            DotEmitter exhaustEmitter = peng.Emitter as DotEmitter;
                            if (exhaustEmitter != null)
                                exhaustEmitter.Direction = Rotation + MathHelper.Pi;
                        }
                        else if (gob is Gobs.Peng)
                        {
                            Gobs.Peng peng = (Gobs.Peng)gob;
                            peng.Leader = this;
                            peng.LeaderBone = boneIs[thrustI].Value;
                        }
                        Arena.Gobs.Add(gob);
                        exhaustBoneIList.Add(boneIs[thrustI].Value);
                        exhaustEngineList.Add(gob);
                    });
                }
            exhaustBoneIs = exhaustBoneIList.ToArray();
            exhaustEngines = exhaustEngineList.ToArray();
        }

        /// <summary>
        /// Updates the gob's exhaust engines.
        /// </summary>
        /// This method should be called after the gob's position and direction
        /// have been updated so that the exhaust engines have an up-to-date location.
        private void UpdateExhaustEngines()
        {
            for (int i = 0; i < exhaustEngines.Length; ++i)
                if (exhaustEngines[i] is ParticleEngine)
                {
                    exhaustEngines[i].Pos = GetNamedPosition(exhaustBoneIs[i]);
                    DotEmitter dotEmitter = ((ParticleEngine)exhaustEngines[i]).Emitter as DotEmitter;
                    if (dotEmitter != null)
                        dotEmitter.Direction = Rotation + MathHelper.Pi;
                }
        }

        #endregion Gob methods related to thrusters

        #region Gob miscellaneous protected methods

        /// <summary>
        /// Switches exhaust engines on or off.
        /// </summary>
        /// <param name="active">If <c>true</c>, switches exhaust engines on, otherwise off.</param>
        protected void SwitchExhaustEngines(bool active)
        {
            foreach (Gob exhaustEngine in exhaustEngines)
            {
                if (exhaustEngine is ParticleEngine)
                    ((ParticleEngine)exhaustEngine).IsAlive = active;
                else if (exhaustEngine is Gobs.Peng)
                    ((Gobs.Peng)exhaustEngine).Paused = !active;
            }
        }

        /// <summary>
        /// Copies a transform of each bone in a model relative to all parent bones of the bone into a given array.
        /// </summary>
        protected virtual void CopyAbsoluteBoneTransformsTo(Model model, Matrix[] transforms)
        {
            model.CopyAbsoluteBoneTransformsTo(transforms);
        }

        #endregion Gob miscellaneous protected methods

        #region Collision methods

        /// <summary>
        /// The primary physical collision area of the gob or <b>null</b> if it doesn't have one.
        /// </summary>
        /// The primary physical collision area is used mostly for movable gobs as an
        /// optimisation to avoid looping through all collision areas in order to find
        /// physical ones.
        public CollisionArea PhysicalArea
        {
            get
            {
                if (collisionAreas.Length == 0) return null;
                if ((collisionAreas[0].Type & CollisionAreaType.Physical) == 0) return null;
                return collisionAreas[0];
            }
        }

        /// <summary>
        /// Performs collision operations for the case when one of this gob's collision areas
        /// is overlapping one of another gob's collision areas.
        /// </summary>
        /// Called only when <b>theirArea.Type</b> matches either <b>myArea.CollidesAgainst</b> or
        /// <b>myArea.CannotOverlap</b>.
        /// <param name="myArea">The collision area of this gob.</param>
        /// <param name="theirArea">The collision area of the other gob.</param>
        /// <param name="stuck">If <b>true</b> then the gob is stuck, i.e.
        /// <b>theirArea.Type</b> matches <b>myArea.CannotOverlap</b> and it's not possible
        /// to backtrack out of the overlap. It is then up to this gob and the other gob 
        /// to resolve the overlap.</param>
        public virtual void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
        }

        #endregion Collision methods

        #region Damage methods

        /// <summary>
        /// The amount of damage, between 0 and <b>MaxDamageLevel</b>.
        /// 0 means the entity is in perfect condition;
        /// <b>MaxDamageLevel</b> means the entity is totally destroyed.
        /// </summary>
        public float DamageLevel { get { return damage; } set { damage = value; } }

        /// <summary>
        /// The maximum amount of damage the entity can sustain.
        /// </summary>
        public float MaxDamageLevel { get { return maxDamage; } }

        /// <summary>
        /// Inflicts damage on the entity.
        /// </summary>
        /// <param name="damageAmount">If positive, amount of damage;
        /// if negative, amount of repair.</param>
        /// <param name="cause">Cause of death if the damage results in death.</param>
        public virtual void InflictDamage(float damageAmount, DeathCause cause)
        {
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Client) return;

            damage += damageAmount;
            damage = MathHelper.Clamp(damage, 0, maxDamage);

            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
            {
                var message = new AW2.Net.Messages.GobDamageMessage();
                message.GobId = this.Id;
                message.DamageLevel = damage;
                AssaultWing.Instance.NetworkEngine.GameClientConnections.Send(message);
            }

            if (damageAmount > 0)
                bleachDamage += damageAmount;
            if (damage == maxDamage)
                Die(cause);
        }

        #endregion Damage methods

        #region Private methods

        /// <summary>
        /// Creates birth gobs for the gob.
        /// </summary>
        void CreateBirthGobs()
        {
            foreach (var gobType in birthGobTypes)
            {
                CreateGob(gobType, gob =>
                {
                    gob.Pos = this.Pos;
                    gob.Rotation = this.Rotation;
                    gob.owner = this.owner;
                    if (gob is ParticleEngine)
                        ((ParticleEngine)gob).Leader = this;
                    if (gob is Gobs.Peng)
                        ((Gobs.Peng)gob).Leader = this;
                    Arena.Gobs.Add(gob);
                });
            }
        }

        /// <summary>
        /// Creates birth gobs for the gob from specially named meshes in the gob's 3D model.
        /// </summary>
        void CreateModelBirthGobs()
        {
            KeyValuePair<string, int>[] poses = GetNamedPositions("Peng_");
            foreach (KeyValuePair<string, int> pos in poses)
            {
                // We expect 3D model bones named like "Peng_blinker_1", where
                // "Peng" is a special marker,
                // "blinker" is the typename of the Peng to create,
                // "1" is an optional number used only to make such bone names unique.
                string[] tokens = pos.Key.Split('_');
                if (tokens.Length < 2 || tokens.Length > 3)
                {
                    Log.Write("Warning: Invalid birth gob definition " + pos.Key + " in 3D model " + modelName);
                    continue;
                }
                Gob.CreateGob((CanonicalString)tokens[1], gob =>
                {
                    Gobs.Peng peng = gob as Gobs.Peng;
                    if (peng != null)
                    {
                        peng.Leader = this;
                        peng.LeaderBone = pos.Value;
                    }
                    ParticleEngine particleEngine = gob as ParticleEngine;
                    if (particleEngine != null)
                        particleEngine.Leader = this;
                    Arena.Gobs.Add(gob);
                });
            }
        }

        /// <summary>
        /// Creates collision areas from specially named 3D model meshes.
        /// </summary>
        void InitializeModelCollisionAreas()
        {
            foreach (ModelMesh mesh in Model.Meshes)
            {
                // A specially named mesh can replace the geometric area of a named collision area.
                if (mesh.Name.StartsWith("mesh_Collision"))
                {
                    string areaName = mesh.Name.Replace("mesh_Collision", "");
                    var changeArea = collisionAreas.FirstOrDefault(area => area.Name == areaName);
                    if (changeArea == null)
                        Log.Write("Warning: Gob found collision mesh \"" + areaName +
                            "\" that didn't match any collision area name.");
                    else
                        changeArea.AreaGob = Graphics3D.GetOutline(mesh);
                }
            }
        }

        /// <summary>
        /// Pretransforms the gob's collision areas if the gob is unmovable.
        /// </summary>
        void TransformUnmovableCollisionAreas()
        {
            if (Movable) return;
            foreach (var area in collisionAreas)
                area.AreaGob = area.AreaGob.Transform(WorldMatrix);
        }

        /// <summary>
        /// The core implementation of the public methods <see cref="Die(DeathCause)"/>
        /// and <see cref="DieOnClient()"/>.
        /// </summary>
        /// <param name="cause">The cause of death.</param>
        /// <param name="forceRemove">Force removal of the dead gob. Useful for clients.</param>
        void DieImpl(DeathCause cause, bool forceRemove)
        {
            if (Dead) return;
            dead = true;

            Arena.Gobs.Remove(this, forceRemove);

            // Create death gobs.
            foreach (var gobType in deathGobTypes)
            {
                CreateGob(gobType, gob =>
                {
                    gob.Pos = this.Pos;
                    gob.Rotation = this.Rotation;
                    gob.owner = this.owner;
                    Arena.Gobs.Add(gob);
                });
            }
        }

        void SetId()
        {
            Id = AssaultWing.Instance.NetworkMode == NetworkMode.Client
                ? leastUnusedIrrelevantId--
                : leastUnusedId++;
        }

        #endregion

        #region IConsistencyCheckable and Clonable Members

        /// <summary>
        /// Called on a cloned object after the cloning.
        /// </summary>
        public override void Cloned()
        {
            foreach (var area in collisionAreas) area.Owner = this;
        }

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public virtual void MakeConsistent(Type limitationAttribute)
        {
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                // Rearrange our collision areas to have a physical area be first, 
                // if there is such.
                for (int i = 0; i < collisionAreas.Length; ++i)
                    if ((collisionAreas[i].Type & CollisionAreaType.Physical) != 0)
                    {
                        CollisionArea swap = collisionAreas[i];
                        collisionAreas[i] = collisionAreas[0];
                        collisionAreas[0] = swap;
                        break;
                    }

                // Make physical attributes sensible.
                mass = Math.Max(0.001f, mass); // strictly positive mass
            }
            if (limitationAttribute == typeof(RuntimeStateAttribute))
            {
                SetId();
            }
        }

        #endregion
    }
}
