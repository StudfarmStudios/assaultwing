using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Game.Particles;

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
    /// Class Gob also provides methods required by certain interfaces such as
    /// ICollidable and IDamageable, although Gob itself doesn't inherit the interfaces.
    /// This serves to unify the collision and other code that would otherwise have to be
    /// implemented separately in each subclass that derives from the interfaces.
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
    public class Gob
    {
        // HACK
        VertexPositionColor[] boundVertexData = null;
        BasicEffect eff = null;

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
        TimeSpan birthTime;

        /// <summary>
        /// True iff the Die() has been called for this gob.
        /// </summary>
        [RuntimeState]
        bool dead;

        /// <summary>
        /// Choice of laws of physics to apply to the gob.
        /// </summary>
        /// Subclasses should set this according to their needs.
        protected PhysicsApplyMode physicsApplyMode;

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
        protected ParticleEngine[] exhaustEngines;

        #endregion Fields for gobs with thrusters

        #region Fields for ICollidable
        // The following fields are implemented here but are used only in
        // subclasses that implement ICollidable.

        /// <summary>
        /// Collision primitives, translated according to the gob's location.
        /// </summary>
        [TypeParameter]
        protected CollisionArea[] collisionAreas;

        /// <summary>
        /// True iff the gob's position at the beginning of the frame was not colliding.
        /// </summary>
        bool hadSafePosition;

        #endregion Fields for ICollidable

        #region Fields for IDamageable
        // The following fields are implemented here but are used only in
        // subclasses that implement IDamageable.

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

        #endregion Fields for IDamageable

        #region Gob properties

        /// <summary>
        /// Get the gob type name.
        /// </summary>
        public string TypeName { get { return typeName; } }

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
        public Vector2 Move { get { return move; } set { move = value; } }

        /// <summary>
        /// Mass of the gob, measured in kilograms.
        /// </summary>
        public float Mass { get { return mass; } }

        /// <summary>
        /// Get or set the gob's rotation around the Z-axis.
        /// </summary>
        public float Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value % MathHelper.TwoPi;
            }
        }

        /// <summary>
        /// Get the owner of the gob.
        /// </summary>
        public Player Owner { get { return owner; } set { owner = value; } }

        /// <summary>
        /// Returns the name of the 3D model of the gob.
        /// </summary>
        public string ModelName { get { return modelName; } set { modelName = value; } }

        /// <summary>
        /// Get and set the scaling factor of the 3D model.
        /// </summary>
        public float Scale { get { return scale; } set { scale = value; } }

        /// <summary>
        /// Returns the world matrix of the gob, i.e., the translation from
        /// game object coordinates to game world coordinates.
        /// </summary>
        public virtual Matrix WorldMatrix
        {
            get
            {
                return Matrix.CreateScale(scale)
                     * Matrix.CreateRotationZ(rotation)
                     * Matrix.CreateTranslation(new Vector3(pos, 0));
            }
        }

        /// <summary>
        /// Choice of laws of physics to apply to the gob.
        /// </summary>
        public PhysicsApplyMode PhysicsApplyMode { get { return physicsApplyMode; } }

        /// <summary>
        /// Is the gob cold.
        /// </summary>
        /// All gobs are born <b>cold</b>. If a gob is cold, it won't
        /// collide with other gobs that have the same owner. This works around
        /// the problem of bullets hitting the firing ship immediately at birth.
        public bool Cold
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
            this.owner = null;
            this.pos = Vector2.Zero;
            this.move = Vector2.Zero;
            this.rotation = 0;
            this.mass = 1;
            this.modelName = "dummymodel";
            this.scale = 1f;
            this.birthGobTypes = new string[] {
            };
            this.deathGobTypes = new string[] {
                "explosion",
            };
            this.collisionAreas = new CollisionArea[] {
                new CollisionArea("General", new Circle(Vector2.Zero, 10f), null),
                new CollisionArea("General", new Polygon(new Vector2[] {
                    new Vector2(10,0),
                    new Vector2(0,7), 
                    new Vector2(0,-7)
                }), null),
                new CollisionArea("General", new Everything(), null),
            };
            this.hadSafePosition = false;
            this.damage = 0;
            this.maxDamage = 100;
            this.birthTime = new TimeSpan(23, 59, 59);
            this.dead = false;
            this.physicsApplyMode = PhysicsApplyMode.All;
            this.physics = null;
            this.modelPartTransforms = null;
            this.exhaustEngineNames = new string[] { "dummyparticleengine", };
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
            this.rotation = 0;

            // If we are not ICollidable, we shouldn't have collision areas either.
            // If we are ICollidable, set us as the owner of each collision area.
            if (typeof(ICollidable).IsAssignableFrom(this.GetType()))
                for (int i = 0; i < collisionAreas.Length; ++i)
                    collisionAreas[i].Owner = (ICollidable)this;
            else
                collisionAreas = new CollisionArea[0];
            this.hadSafePosition = false; // we don't know, so do this to be sure
            this.physicsApplyMode = PhysicsApplyMode.All;
            this.physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));
            this.birthTime = this.physics.TimeStep.TotalGameTime;
            this.modelPartTransforms = null;
            this.exhaustEngines = new ParticleEngine[0];
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
                throw new ArgumentException("Runtime gob (class " + runtimeState.GetType().Name +
                    ") got type name " + runtimeState.typeName + " for class " + gob.GetType().Name);
            gob.SetRuntimeState(runtimeState);

            // Do special things with meshes named like collision areas.
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Model model = data.GetModel(gob.modelName);
            List<CollisionArea> meshAreas = new List<CollisionArea>();
            foreach (ModelMesh mesh in model.Meshes)
            {
                if (mesh.Name.StartsWith("mesh_Collision") && gob is ICollidable)
                {
                    string areaName = mesh.Name.Replace("mesh_Collision", "");
                    IGeomPrimitive area = Graphics3D.GetOutline(mesh);
                    meshAreas.Add(new CollisionArea(areaName, area, (ICollidable)gob));
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
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        /// DataEngine will call this method to make the gob do necessary 
        /// initialisations to make it fully functional on addition to 
        /// an ongoing play of the game.
        public virtual void Activate()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

            // Create birth gobs.
            foreach (string gobType in birthGobTypes)
            {
                Gob gob = CreateGob(gobType);
                gob.Pos = this.Pos;
                gob.Rotation = this.Rotation;
                if (gob is ParticleEngine)
                    ((ParticleEngine)gob).Leader = this;
                data.AddGob(gob);
            }
        }

        /// <summary>
        /// Updates the gob according to physical laws.
        /// </summary>
        /// Overriden Update methods must explicitly call this method in order to have 
        /// physical laws apply to the gob.
        public virtual void Update()
        {
            physics.Move(this);
        }

        /// <summary>
        /// Kills the gob, i.e. performs a death ritual and removes the gob from the game world.
        /// </summary>
        /// Call this method to make the gob die like it would during normal gameplay.
        /// Alternatively, if you want to just make the gob disappear, you can simply
        /// remove it from the game data. But this might be a bad idea later on if gobs
        /// refer to each other.
        /// Overriding methods should not do anything if the property <b>Dead</b> is true.
        public virtual void Die()
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
                data.AddGob(gob);
            }
        }

        /// <summary>
        /// Releases all resources allocated by the gob.
        /// </summary>
        public virtual void Dispose()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            foreach (ParticleEngine exhaustEngine in exhaustEngines)
                data.RemoveParticleEngine(exhaustEngine);
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
        /// Draws the gob.
        /// </summary>
        /// Assumes that the sprite batch has been Begun already and will be
        /// Ended later by someone else.
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="spriteBatch">The sprite batch to draw sprites with.</param>
        public virtual void Draw(Matrix view, Matrix projection, SpriteBatch spriteBatch)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Model model = data.GetModel(modelName);
            Matrix world = WorldMatrix;
            UpdateModelPartTransforms(model);
            Matrix meshSphereTransform = // mesh bounding spheres are by default in model coordinates
                Matrix.CreateScale(Scale) *
                Matrix.CreateTranslation(new Vector3(Pos, 0));
            foreach (ModelMesh mesh in model.Meshes)
            {
                if (mesh.Name.StartsWith("mesh_Collision"))
                    continue;
                if (!data.Viewport.Intersects(mesh.BoundingSphere.Transform(meshSphereTransform)))
                    return;
                foreach (BasicEffect be in mesh.Effects)
                {
                    data.PrepareEffect(be);
                    be.Projection = projection;
                    be.View = view;
                    be.World = modelPartTransforms[mesh.ParentBone.Index] * world;
                }
                mesh.Draw();
            }

#if false
            // Draw polygonal collision area outlines (for hardcore debugging)
            foreach (CollisionArea collArea in collisionAreas)
            {
                if (!(collArea.Area is Polygon)) continue;
                Graphics3D.GetWireframeModelData((Polygon)collArea.Area, 400f, Color.Azure, ref boundVertexData);
                Matrix meshTrans = modelPartTransforms[model.Meshes[0].ParentBone.Index] *
                    Matrix.CreateScale(Scale)
                    * Matrix.CreateTranslation(new Vector3(Pos, 0));
                GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice;
                gfx.VertexDeclaration = new VertexDeclaration(gfx, VertexPositionColor.VertexElements);
                if (eff == null)
                    eff = new BasicEffect(AssaultWing.Instance.GraphicsDevice, null);
                data.PrepareEffect(eff);
                eff.World = Matrix.Identity;
                eff.Projection = projection;
                eff.View = view;
                eff.TextureEnabled = false;
                eff.LightingEnabled = false;
                eff.VertexColorEnabled = true;
                eff.Begin();
                foreach (EffectPass pass in eff.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    gfx.DrawUserPrimitives<VertexPositionColor>(
                        PrimitiveType.LineStrip, boundVertexData, 0, boundVertexData.Length - 1);
                    pass.End();
                }
                eff.End();
            }
#endif
#if false
            // Draw first collision area bounding sphere (for hardcore debugging)
            Matrix meshTrans = modelPartTransforms[model.Meshes[0].ParentBone.Index]
                * Matrix.CreateScale(Scale)
                * Matrix.CreateTranslation(new Vector3(Pos, 0));
            BoundingSphere sph = model.Meshes[0].BoundingSphere.Transform(meshTrans);
            Graphics3D.GetWireframeModelData(sph, 400f, Color.Azure, out boundVertexData);
            GraphicsDevice gfx = AssaultWing.Instance.GraphicsDevice; 
            gfx.VertexDeclaration = new VertexDeclaration(gfx, VertexPositionColor.VertexElements);
            if (eff == null)
                eff = new BasicEffect(AssaultWing.Instance.GraphicsDevice, null);
            data.PrepareEffect(eff);
            eff.World = Matrix.Identity;
            eff.Projection = projection;
            eff.View = view;
            eff.TextureEnabled = false;
            eff.LightingEnabled = false;
            eff.VertexColorEnabled = true;
            eff.Begin();
            foreach (EffectPass pass in eff.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserPrimitives<VertexPositionColor>(
                    PrimitiveType.LineStrip, boundVertexData, 0, boundVertexData.Length - 1);
                pass.End();
            }
            eff.End();
#endif
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

        #endregion Gob public methods

        #region Gob protected methods

        /// <summary>
        /// Creates exhaust engines for the gob in all thrusters named in its 3D model.
        /// </summary>
        /// A subclass should call this method on <b>Activate</b> 
        /// if it wants to have visible thrusters,
        /// and then do final adjustments on the newly created <b>exhaustEngines</b>.
        /// The subclass should regularly <b>Update</b> its <b>exhaustEngines</b>.
        protected void CreateExhaustEngines()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            KeyValuePair<string, int>[] boneIs = GetNamedPositions("Thruster");
            if (boneIs.Length == 0)
                Log.Write("Warning: Gob (" + typeName + ") found no thrusters in its 3D model");

            // Create proper exhaust engines.
            int templates = exhaustEngineNames.Length;
            exhaustBoneIs = new int[boneIs.Length * templates];
            exhaustEngines = new ParticleEngine[boneIs.Length * templates];
            for (int thrustI = 0; thrustI < boneIs.Length; ++thrustI)
                for (int tempI = 0; tempI < templates; ++tempI)
                {
                    int i = thrustI * templates + tempI;
                    exhaustBoneIs[i] = boneIs[thrustI].Value;
                    exhaustEngines[i] = new ParticleEngine(exhaustEngineNames[tempI]);
                    exhaustEngines[i].Loop = true;
                    exhaustEngines[i].IsAlive = false;
                    DotEmitter exhaustEmitter = (DotEmitter)exhaustEngines[i].Emitter;
                    exhaustEmitter.Direction = Rotation + MathHelper.Pi;
                    data.AddParticleEngine(exhaustEngines[i]);
                }
        }

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

        #endregion Gob protected methods

        #region ICollidable Members // Implemented here for subclasses

        /// <summary>
        /// Returns the collision primitives of the gob.
        /// </summary>
        /// <returns>The collision primitives of the gob.</returns>
        public CollisionArea[] GetPrimitives()
        {
            return collisionAreas;
        }

        /// <summary>
        /// The index of the physical collision area of the collidable gob
        /// in <b>GetPrimitives()</b> or <b>-1</b> if it has none.
        /// </summary>
        public int PhysicalArea
        {
            get
            {
                for (int i = 0; i < collisionAreas.Length; ++i)
                    if (collisionAreas[i].Type == CollisionAreaType.Physical)
                        return i;
                return -1;
            }
        }
            
        /// <summary>
        /// Returns the distance from the edge of the physical collision area
        /// of the collidable gob to a point in the game world.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>The shortest distance between the point and the gob's
        /// physical collision area.</returns>
        public float DistanceTo(Vector2 point)
        {
            foreach (CollisionArea area in collisionAreas)
                if (area.Type == CollisionAreaType.Physical)
                    return area.Area.DistanceTo(point);

            // We're far away unless proven otherwise.
            return Single.MaxValue;
        }

        /// <summary>
        /// Returns true iff the gob's position at the beginning of the frame was not colliding.
        /// </summary>
        /// <returns>True iff the gob's position at the beginning of the frame was not colliding</returns>
        public bool HadSafePosition { get { return hadSafePosition; } set { hadSafePosition = value; } }

        /// <summary>
        /// Performs collision operations with a gob whose general collision area
        /// has collided with one of our receptor areas.
        /// </summary>
        /// <param name="gob">The gob we collided with.</param>
        /// <param name="receptorName">The name of our colliding receptor area.</param>
        public virtual void Collide(ICollidable gob, string receptorName)
        {
        }

        #endregion ICollidable Members

        #region IDamageable Members // Implemented here for subclasses

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
        public virtual void InflictDamage(float damageAmount)
        {
            damage += damageAmount;
            damage = MathHelper.Clamp(damage, 0, maxDamage);
            if (damage == maxDamage)
                Die();
        }

        #endregion IDamageable Members
    }
}
