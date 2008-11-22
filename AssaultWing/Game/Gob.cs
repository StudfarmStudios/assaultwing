#if !DEBUG
#define OPTIMIZED_CODE // replace some function calls with fast elementary operations
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Particles;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Graphics;

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
    [System.Diagnostics.DebuggerDisplay("typeName:{typeName} pos:{pos} move:{move}")]
    public class Gob : IConsistencyCheckable
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
        public static readonly float defaultRotation = MathHelper.PiOver2;

        /// <summary>
        /// Time, in seconds, for a gob to stop being cold.
        /// </summary>
        /// <see cref="Gob.Cold"/>
        public static readonly float warmUpTime = 0.2f;

        /// <summary>
        /// Gob type name.
        /// </summary>
        [TypeParameter, RuntimeState]
        string typeName;

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
        /// Arena layer index of the gob. Set by DataEngine.
        /// </summary>
        int layer;

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
        string modelName;

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
        [TypeParameter]
        string[] birthGobTypes;

        /// <summary>
        /// Types of gobs to create on death.
        /// </summary>
        /// You might want to put some gob types of subclass Explosion here.
        [TypeParameter]
        string[] deathGobTypes;

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
        /// The physics engine of the game instance this gob belongs to.
        /// </summary>
        protected PhysicsEngine physics;

        /// <summary>
        /// Table for holding 3D model part transform matrices.
        /// </summary>
        protected Matrix[] modelPartTransforms;

        #endregion Fields for all gobs

        #region Fields for gobs with thrusters

        /// <summary>
        /// Names of exhaust engine types.
        /// </summary>
        [TypeParameter]
        string[] exhaustEngineNames;

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
        /// Elasticity factor of the gob. Zero means the no bouncing off 
        /// at a collision for either gob. One means fully elastic collision.
        /// </summary>
        /// The elasticity factors of both colliding gobs affect the final elasticity
        /// of the collision. Avoid using zero; instead, use a very small number.
        /// Use a number above one to regain fully elastic collisions even
        /// when countered by inelastic gobs.
        [TypeParameter]
        float elasticity;

        /// <summary>
        /// Friction factor of the gob. Zero means that movement along the
        /// collision surface is not slowed by friction.
        /// </summary>
        /// The friction factors of both colliding gobs affect the final friction
        /// of the collision. It's a good idea to use values that are closer to
        /// zero than one.
        [TypeParameter]
        float friction;

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
        /// Get the gob type name.
        /// </summary>
        public string TypeName { get { return typeName; } }

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
        /// Names of all 3D models that this gob type will ever use.
        /// </summary>
        public virtual List<string> ModelNames
        {
            get
            {
                List<string> names = new List<string>();
                names.Add(modelName);
                return names;
            }
        }

        /// <summary>
        /// Names of all textures that this gob type will ever use.
        /// </summary>
        public virtual List<string> TextureNames
        {
            get
            {
                List<string> names = new List<string>();
                return names;
            }
        }

        /// <summary>
        /// Get or set the gob position in the game world.
        /// </summary>
        public virtual Vector2 Pos
        {
            get { return pos; }
            set
            {
                pos = value;
            }
        }

        /// <summary>
        /// Get or set the gob's movement vector.
        /// </summary>
        public virtual Vector2 Move { get { return move; } set { move = value; } }

        /// <summary>
        /// Mass of the gob, measured in kilograms.
        /// </summary>
        public float Mass { get { return mass; } }

        /// <summary>
        /// Elasticity factor of the gob. Zero means the no bouncing off 
        /// at a collision for either gob. One means fully elastic collision.
        /// </summary>
        /// The elasticity factors of both colliding gobs affect the final elasticity
        /// of the collision. Avoid using zero; instead, use a very small number.
        /// Use a number above one to regain fully elastic collisions even
        /// when countered by inelastic gobs.
        public float Elasticity { get { return elasticity; } }

        /// <summary>
        /// Friction factor of the gob. Zero means that movement along the
        /// collision surface is not slowed by friction.
        /// </summary>
        /// The friction factors of both colliding gobs affect the final friction
        /// of the collision. It's a good idea to use values that are closer to
        /// zero than one. 
        public float Friction { get { return friction; } }

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
        /// Arena layer index of the gob. Set by <c>DataEngine</c>.
        /// </summary>
        /// Note that if somebody who is not DataEngine sets this value,
        /// it leads to confusion. This field only reflects the real knowledge
        /// that DataEngine possesses.
        public int Layer { get { return layer; } set { layer = value; } }

        /// <summary>
        /// Returns the name of the 3D model of the gob.
        /// </summary>
        public string ModelName { get { return modelName; } set { modelName = value; } }

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
                        long bleachDurationTicks = (long)(TimeSpan.TicksPerSecond * 0.055f);
                        bleachResetTime = AssaultWing.Instance.GameTime.TotalGameTime + new TimeSpan(bleachDurationTicks);
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
        public virtual Matrix WorldMatrix
        {
            get
            {
#if OPTIMIZED_CODE
                float scaledCos = scale * (float)Math.Cos(rotation);
                float scaledSin = scale * (float)Math.Sin(rotation);
                return new Matrix(
                    scaledCos, scaledSin, 0, 0,
                    -scaledSin, scaledCos, 0, 0,
                    0, 0, scale, 0,
                    pos.X, pos.Y, 0, 1);
#else
                return Matrix.CreateScale(scale)
                     * Matrix.CreateRotationZ(rotation)
                     * Matrix.CreateTranslation(new Vector3(pos, 0));
#endif
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
        public virtual bool Cold
        {
            get
            {
                return physics.TimeStep.TotalGameTime.Subtract(birthTime).TotalSeconds < warmUpTime;
            }
        }

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

        #endregion Gob Properties

        #region Gob static and instance constructors, and static constructor-like methods

        static Gob()
        {
            // Check that important constructors have been declared
            Helpers.Log.Write("Checking gob constructors");
            foreach (Type type in Array.FindAll<Type>(System.Reflection.Assembly.GetExecutingAssembly().GetTypes(),
                delegate(Type t) { return typeof(Gob).IsAssignableFrom(t); }))
            {
                if (null == type.GetConstructor(Type.EmptyTypes))
                {
                    string message = "Missing constructor " + type.Name + "()";
                    Log.Write(message);
                    throw new Exception(message);
                }
                if (null == type.GetConstructor(new Type[] { typeof(string) }))
                {
                    string message = "Missing constructor " + type.Name + "(string)";
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
            this.typeName = "unknown gob type";
            depthLayer2D = 0.5f;
            drawMode2D = new DrawMode2D(DrawModeType2D.None);
            layerPreference = LayerPreferenceType.Front;
            this.owner = null;
            this.pos = Vector2.Zero;
            this.move = Vector2.Zero;
            this.rotation = 0;
            this.mass = 1;
            this.elasticity = 0.7f;
            this.friction = 0.7f;
            this.modelName = "dummymodel";
            this.scale = 1f;
            this.birthGobTypes = new string[0];
            this.deathGobTypes = new string[0];
            this.collisionAreas = new CollisionArea[] {
                /*
                new CollisionArea("General", new Circle(Vector2.Zero, 10f), null,
                CollisionAreaType.None, CollisionAreaType.None, CollisionAreaType.None),
                new CollisionArea("General", new Polygon(new Vector2[] {
                    new Vector2(10,0),
                    new Vector2(0,7), 
                    new Vector2(0,-7)
                }), null,
                CollisionAreaType.None, CollisionAreaType.None, CollisionAreaType.None),
                new CollisionArea("General", new Everything(), null,
                CollisionAreaType.None, CollisionAreaType.None, CollisionAreaType.None),
                */
            };
            this.damage = 0;
            this.maxDamage = 100;
            bleachDamage = 0;
            this.birthTime = new TimeSpan(23, 59, 59);
            this.dead = false;
            this.disabled = false;
            this.movable = true;
            this.physics = null;
            this.modelPartTransforms = null;
            this.exhaustEngineNames = new string[0];
        }

        /// <summary>
        /// Creates a gob of the specified gob type.
        /// </summary>
        /// The gob's serialised fields are initialised according to the gob template 
        /// instance associated with the gob type. This applies also to fields declared
        /// in subclasses, so a subclass constructor only has to initialise its runtime
        /// state fields, not the fields that define its gob type.
        /// <param name="typeName">The type of the gob.</param>
        public Gob(string typeName)
        {
            // Initialise fields from the gob type's template.
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Gob template = (Gob)data.GetTypeTemplate(typeof(Gob), typeName);
            if (template.GetType() != this.GetType())
                throw new Exception("Silly programmer tries to create a gob (type " +
                    typeName + ") using a wrong Gob subclass (class " + this.GetType().Name + ")");
            foreach (FieldInfo field in Serialization.GetFields(this, typeof(TypeParameterAttribute)))
                field.SetValue(this, Serialization.DeepCopy(field.GetValue(template)));

            this.owner = null;
            this.Pos = Vector2.Zero; // also translates collPrimitives
            this.move = Vector2.Zero;
            this.rotation = Gob.defaultRotation;

            // Set us as the owner of each collision area.
            if (collisionAreas == null)
                collisionAreas = new CollisionArea[0];
            for (int i = 0; i < collisionAreas.Length; ++i)
                collisionAreas[i].Owner = this;

            this.movable = true;
            this.physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));
            this.birthTime = this.physics.TimeStep.TotalGameTime;
            this.modelPartTransforms = null;
            exhaustEngines = new Gob[0];
            this.alpha = 1;
            bleachDamage = 0;
            bleach = -1;
            bleachResetTime = new TimeSpan(0);
        }

        /// <summary>
        /// Creates a gob of the specified gob type.
        /// </summary>
        /// The gob's serialised fields are initialised according to the gob template 
        /// instance associated with the gob type. This applies also to fields declared
        /// in subclasses, so a subclass constructor only has to initialise its runtime
        /// state fields, not the fields that define its gob type.
        /// <param name="typeName">The type of the gob.</param>
        /// <param name="owner">The player who owns the gob.</param>
        /// <param name="pos">The position of the gob in the game world.</param>
        /// <param name="move">The movement vector of the gob.</param>
        /// <param name="rotation">The rotation of the gob around the Z-axis.</param>
        public Gob(string typeName, Player owner, Vector2 pos, Vector2 move, float rotation)
            : this(typeName)
        {
            this.owner = owner;
            this.Pos = pos; // also translates collPrimitives
            this.move = move;
            this.rotation = rotation;
        }

        /// <summary>
        /// Creates a gob of the given type.
        /// </summary>
        /// Note that you cannot call new Gob(typeName) because then the created object
        /// won't have the fields of the subclass that 'typeName' requires. This static method
        /// takes care of finding the correct subclass.
        /// <param name="typeName">The type of the gob.</param>
        /// <param name="args">Any other arguments to pass to the subclass' constructor.</param>
        /// <returns>The newly created gob.</returns>
        public static Gob CreateGob(string typeName, params object[] args)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Gob template = (Gob)data.GetTypeTemplate(typeof(Gob), typeName);
            Type type = template.GetType();
            object[] newArgs = new object[args.Length + 1];
            newArgs[0] = typeName;
            Array.Copy(args, 0, newArgs, 1, args.Length);
            return (Gob)Activator.CreateInstance(type, newArgs);
        }

        /// <summary>
        /// Creates a new gob from the given runtime state.
        /// </summary>
        /// Use this method to revive gobs whose runtime state you have deserialised.
        /// This method will create the gob properly, initialising all fields and then
        /// copying the runtime state fields to the new instance.
        /// <param name="runtimeState">The runtime state from where to initialise the new gob.</param>
        /// <returns>The newly created gob.</returns>
        public static Gob CreateGob(Gob runtimeState)
        {
            Gob gob = CreateGob(runtimeState.TypeName);
            if (runtimeState.GetType() != gob.GetType())
                throw new ArgumentException("Runtime gob of class " + runtimeState.GetType().Name +
                    " has type name \"" + runtimeState.typeName + "\" which is for class " + gob.GetType().Name);
            gob.SetRuntimeState(runtimeState);

            // Do special things with meshes named like collision areas.
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Model model = data.GetModel(gob.modelName);
            List<CollisionArea> meshAreas = new List<CollisionArea>();
            foreach (ModelMesh mesh in model.Meshes)
            {
                // A specially named mesh can replace the geometric area of a named collision area.
                if (mesh.Name.StartsWith("mesh_Collision"))
                {
                    string areaName = mesh.Name.Replace("mesh_Collision", "");
                    CollisionArea changeArea = null;
                    foreach (CollisionArea area in gob.collisionAreas)
                        if (area.Name == areaName)
                        {
                            changeArea = area;
                            break;
                        }
                    if (changeArea == null)
                        Log.Write("Warning: Gob found collision mesh \"" + areaName +
                            "\" that didn't match any collision area name.");
                    else
                    {
                        IGeomPrimitive geomArea = Graphics3D.GetOutline(mesh);
                        if (!gob.Movable)
                            geomArea = geomArea.Transform(gob.WorldMatrix);
                        changeArea.AreaGob = geomArea;
                    }
                }
            }
            if (meshAreas.Count > 0)
            {
                CollisionArea[] newAreas = new CollisionArea[gob.collisionAreas.Length + meshAreas.Count];
                Array.Copy(gob.collisionAreas, newAreas, gob.collisionAreas.Length);
                Array.Copy(meshAreas.ToArray(), newAreas, meshAreas.Count);
                gob.collisionAreas = newAreas;
            }

            return gob;
        }

        #endregion Gob constructors and static constructor-like methods

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public virtual void LoadContent()
        {
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
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            LoadContent();

            // Create birth gobs.
            foreach (string gobType in birthGobTypes)
            {
                Gob gob = CreateGob(gobType);
                gob.Pos = this.Pos;
                gob.Rotation = this.Rotation;
                gob.owner = this.owner;
                if (gob is ParticleEngine)
                    ((ParticleEngine)gob).Leader = this;
                if (gob is Gobs.Peng)
                {
                    ((Gobs.Peng)gob).Leader = this;
                    // TODO: User named bones for birth gob placement //((Gobs.Peng)gob).LeaderBone = GetNamedPositions("SomeGobPart");
                }
                data.AddGob(gob);
            }

            CreateExhaustEngines();
        }

        /// <summary>
        /// Updates the gob according to physical laws.
        /// </summary>
        /// Overriden Update methods should explicitly call this method in order to have 
        /// physical laws apply to the gob and the gob's exhaust engines updated.
        public virtual void Update()
        {
            physics.Move(this);
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
            if (Dead) return;
            dead = true;

            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            data.RemoveGob(this);

            // Create death gobs.
            foreach (string gobType in deathGobTypes)
            {
                Gob gob = CreateGob(gobType);
                gob.Pos = this.Pos;
                gob.Rotation = this.Rotation;
                gob.owner = this.owner;
                data.AddGob(gob);
            }
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
        /// A rectangular area in the X-Y-plane that contains the gob 
        /// as it is seen in its current location in the game world.
        /// </summary>
        /// Subclasses who override the <b>Draw</b> method should also 
        /// override this property.
        [Obsolete("Overridden Draw methods should do their own bounding volume clipping")]
        public virtual BoundingBox DrawBoundingBox
        {
            get
            {
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                Model model = data.GetModel(modelName);
                UpdateModelPartTransforms(model);
                BoundingSphere modelSphere = new BoundingSphere();
                bool firstDone = false;
                foreach (ModelMesh mesh in model.Meshes)
                {
                    Matrix meshTransform = modelPartTransforms[mesh.ParentBone.Index];
                    BoundingSphere meshSphere = mesh.BoundingSphere.Transform(meshTransform);
                    if (!firstDone)
                    {
                        firstDone = true;
                        modelSphere = meshSphere;
                    }
                    else
                        modelSphere = BoundingSphere.CreateMerged(modelSphere, meshSphere);
                }
                modelSphere = modelSphere.Transform(Matrix.CreateTranslation(new Vector3(Pos, 0)));
                BoundingBox modelBox = BoundingBox.CreateFromSphere(modelSphere);
                return modelBox;
            }
        }

        /// <summary>
        /// Draws the gob's 3D graphics.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public virtual void Draw(Matrix view, Matrix projection)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            BoundingFrustum viewVolume = new BoundingFrustum(view * projection);
            Model model = data.GetModel(modelName);
            Matrix world = WorldMatrix;
            UpdateModelPartTransforms(model);
            Matrix meshSphereTransform = // mesh bounding spheres are by default in model coordinates
                Matrix.CreateScale(Scale) *
                Matrix.CreateTranslation(new Vector3(Pos, 0));

            // Draw each mesh in the 3D model.
            foreach (ModelMesh mesh in model.Meshes)
            {
                if (mesh.Name.StartsWith("mesh_Collision"))
                    continue;
                if (!viewVolume.Intersects(mesh.BoundingSphere.Transform(meshSphereTransform)))
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
                    data.PrepareEffect(be);
                    be.Projection = projection;
                    be.View = view;
                    be.World = modelPartTransforms[mesh.ParentBone.Index] * world;
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
            // Note: We assume that UpdateModelPartTransforms() has been called recently.
            if (modelPartTransforms == null) // HACK to avoid crash when shooting on the very first frame
            {
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                UpdateModelPartTransforms(data.GetModel(modelName));
            }
            return Vector2.Transform(Vector2.Zero, modelPartTransforms[boneIndex] * WorldMatrix);
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
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            List<KeyValuePair<string, int>> boneIs = new List<KeyValuePair<string, int>>();
            Model model = data.GetModel(ModelName);
            foreach (ModelBone bone in model.Bones)
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
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            KeyValuePair<string, int>[] boneIs = GetNamedPositions("Thruster");

            // Create proper exhaust engines.
            int templates = exhaustEngineNames.Length;
            exhaustBoneIs = new int[boneIs.Length * templates];
            exhaustEngines = new Gob[boneIs.Length * templates];
            for (int thrustI = 0; thrustI < boneIs.Length; ++thrustI)
                for (int tempI = 0; tempI < templates; ++tempI)
                {
                    int i = thrustI * templates + tempI;
                    exhaustBoneIs[i] = boneIs[thrustI].Value;
                    exhaustEngines[i] = Gob.CreateGob(exhaustEngineNames[tempI]);
                    if (exhaustEngines[i] is ParticleEngine)
                    {
                        ParticleEngine peng = (ParticleEngine)exhaustEngines[i];
                        peng.Loop = true;
                        peng.IsAlive = true;
                        DotEmitter exhaustEmitter = peng.Emitter as DotEmitter;
                        if (exhaustEmitter != null)
                            exhaustEmitter.Direction = Rotation + MathHelper.Pi;
                    }
                    else if (exhaustEngines[i] is Gobs.Peng)
                    {
                        Gobs.Peng peng = (Gobs.Peng)exhaustEngines[i];
                        peng.Leader = this;
                        peng.LeaderBone = exhaustBoneIs[i];
                    }
                    data.AddGob(exhaustEngines[i]);
                }
        }

        /// <summary>
        /// Updates the gob's exhaust engines.
        /// </summary>
        /// This method should be called after the gob's position and direction
        /// have been updated so that the exhaust engines have an up-to-date location.
        private void UpdateExhaustEngines()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Model model = data.GetModel(ModelName);
            UpdateModelPartTransforms(model);
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
        /// Updates <b>modelPartTransforms</b> to contain the absolute transforms
        /// of each mesh of the gob's 3D model relative to the root bone of the 3D model.
        /// </summary>
        /// <param name="model">The model of the gob.</param>
        /// You should only pass as argument the model you get from
        /// <b>DataEngine.GetModel(this.modelName)</b>.
        protected void UpdateModelPartTransforms(Model model)
        {
            if (modelPartTransforms == null || modelPartTransforms.Length != model.Bones.Count)
                modelPartTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(modelPartTransforms);
        }

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

        #endregion Gob miscellaneous protected methods

        #region Collision methods

        /// <summary>
        /// The physical collision area of the gob or <b>null</b> if it doesn't have one.
        /// </summary>
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
        public float DamageLevel { get { return damage; } }

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
            damage += damageAmount;
            damage = MathHelper.Clamp(damage, 0, maxDamage);
            if (damageAmount > 0)
                bleachDamage += damageAmount;
            if (damage == maxDamage)
                Die(cause);
        }

        #endregion Damage methods

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public virtual void MakeConsistent(Type limitationAttribute)
        {
            // Rearrange our collision areas to have a physical area be first, 
            // if there is such.
            if (collisionAreas.Length == 0) return;
            if ((collisionAreas[0].Type & CollisionAreaType.Physical) != 0) return;
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
            elasticity = Math.Max(0, elasticity);
            friction = Math.Max(0, friction);
        }

        #endregion
    }
}
