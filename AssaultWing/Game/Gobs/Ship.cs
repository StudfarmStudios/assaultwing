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
    /// A player's ship.
    /// </summary>
    public class Ship : Gob
    {
        #region Ship fields related to flying

        /// <summary>
        /// Maximum force of thrust of the ship, measured in Newtons.
        /// </summary>
        [TypeParameter]
        float thrustForce;

        /// <summary>
        /// Maximum turning speed of the ship, measured in radians per second.
        /// </summary>
        [TypeParameter]
        float turnSpeed;

        /// <summary>
        /// Ship's maximum speed reachable by thrust, measured in meters per second.
        /// </summary>
        [TypeParameter]
        float maxSpeed;

        #endregion Ship fields related to flying

        #region Ship fields related to weapons

        /// <summary>
        /// Primary weapons of the ship.
        /// </summary>
        [RuntimeState]
        Weapon weapon1;

        /// <summary>
        /// Secondary weapons of the ship.
        /// </summary>
        [RuntimeState]
        Weapon weapon2;

        /// <summary>
        /// Maximum amount of charge for primary weapons.
        /// </summary>
        [TypeParameter]
        float weapon1ChargeMax;

        /// <summary>
        /// Maximum amount of charge for secondary weapons.
        /// </summary>
        [TypeParameter]
        float weapon2ChargeMax;

        /// <summary>
        /// Speed of charging for primary weapon charge,
        /// measured in charge units per second.
        /// </summary>
        [TypeParameter]
        float weapon1ChargeSpeed;

        /// <summary>
        /// Speed of charging for secondary weapon charge,
        /// measured in charge units per second.
        /// </summary>
        [TypeParameter]
        float weapon2ChargeSpeed;

        /// <summary>
        /// Amount of charge for primary weapons,
        /// between <b>0</b> and <b>weapon1ChargeMax</b>.
        /// </summary>
        [RuntimeState]
        float weapon1Charge;

        /// <summary>
        /// Amount of charge for secondary weapons,
        /// between <b>0</b> and <b>weapon2ChargeMax</b>.
        /// </summary>
        [RuntimeState]
        float weapon2Charge;

        #endregion Ship fields related to weapons

        #region Ship fields related to rolling

        /// <summary>
        /// Current rotation of the ship around its tail-to-head axis.
        /// </summary>
        /// This value will move towards <b>rollAngleGoal</b> at a constant speed.
        [RuntimeState]
        float rollAngle;

        /// <summary>
        /// The currently desired roll angle.
        /// </summary>
        /// This value is calculated separately each frame based
        /// on ship turning.
        float rollAngleGoal;

        /// <summary>
        /// True iff <b>rollAngleGoal</b> has been set by ship turning this frame.
        /// </summary>
        bool rollAngleGoalUpdated;

        /// <summary>
        /// Maximum roll angle.
        /// </summary>
        /// Minimum roll angle will be the additive inverse.
        [TypeParameter]
        float rollMax;

        /// <summary>
        /// Roll angle change speed in radians per second.
        /// </summary>
        [TypeParameter]
        float rollSpeed;

        #endregion Ship fields related to rolling

        #region Ship fields related to coughing

        /// <summary>
        /// Names of cough engine types.
        /// </summary>
        [TypeParameter]
        string[] coughEngineNames;

        /// <summary>
        /// Particle engines that manage coughing.
        /// </summary>
        ParticleEngine[] coughEngines;

        #endregion Ship fields related to coughing

        #region Ship fields related to other things

        /// <summary>
        /// Armour of the ship as a function that maps
        /// the amount of damage delivered to the ship
        /// to the amount of damage the ship actually receives.
        /// </summary>
        [TypeParameter]
        Curve armour;

        /// <summary>
        /// Alpha of the ship as a function that maps the age of the
        /// ship (in seconds) to the alpha value to draw the ship with.
        /// </summary>
        /// Use this to implement alpha flashing on ship birth.
        [TypeParameter]
        Curve birthAlpha;

        /// <summary>
        /// True iff the amount of exhaust output has been set by ship thrusting this frame.
        /// </summary>
        bool exhaustAmountUpdated;

        #endregion Ship fields related to other things

        #region Ship properties

        /// <summary>
        /// Returns the world matrix of the gob, i.e., the translation from
        /// game object coordinates to game world coordinates.
        /// </summary>
        public override Matrix WorldMatrix
        {
            get
            {
                return Matrix.CreateScale(Scale)
                     * Matrix.CreateRotationX(rollAngle)
                     * Matrix.CreateRotationZ(Rotation)
                     * Matrix.CreateTranslation(new Vector3(Pos, 0));
            }
        }

        /// <summary>
        /// The primary weapon of the ship.
        /// </summary>
        public Weapon Weapon1 { get { return weapon1; } }

        /// <summary>
        /// The secondary weapon of the ship.
        /// </summary>
        public Weapon Weapon2 { get { return weapon2; } }

        /// <summary>
        /// Name of the type of main weapon the ship is using.
        /// </summary>
        public string Weapon1Name
        {
            set
            {
                if (weapon1 != null)
                {
                    DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                    data.RemoveWeapon(weapon1);
                }
                weapon1 = CreateWeapons(value, 1);
            }
        }

        /// <summary>
        /// Name of the type of secondary weapon the ship is using.
        /// </summary>
        public string Weapon2Name
        {
            set
            {
                if (weapon2 != null)
                {
                    DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                        data.RemoveWeapon(weapon2);
                }
                weapon2 = CreateWeapons(value, 2);
            }
        }

        /// <summary>
        /// Is any of the primary weapons loaded.
        /// </summary>
        public bool Weapon1Loaded
        {
            get
            {
                return weapon1.Loaded;
            }
        }

        /// <summary>
        /// Is any of the secondary weapons loaded.
        /// </summary>
        public bool Weapon2Loaded
        {
            get
            {
                return weapon2.Loaded;
            }
        }

        /// <summary>
        /// Amount of charge for primary weapons,
        /// between <b>0</b> and <b>Weapon1ChargeMax</b>.
        /// </summary>
        public float Weapon1Charge { 
            get { return weapon1Charge; }
            set { weapon1Charge = MathHelper.Clamp(value, 0, weapon1ChargeMax); }
        }

        /// <summary>
        /// Maximum amount of charge for primary weapons.
        /// </summary>
        public float Weapon1ChargeMax { get { return weapon1ChargeMax; } }

        /// <summary>
        /// Amount of charge for secondary weapons,
        /// between <b>0</b> and <b>Weapon2ChargeMax</b>.
        /// </summary>
        public float Weapon2Charge { 
            get { return weapon2Charge; }
            set { weapon2Charge = MathHelper.Clamp(value, 0, weapon2ChargeMax); }
        }

        /// <summary>
        /// Maximum amount of charge for secondary weapons.
        /// </summary>
        public float Weapon2ChargeMax { get { return weapon2ChargeMax; } }

        #endregion Ship properties

        #region Ship constructors

        /// <summary>
        /// Creates an uninitialised ship.
        /// </summary>
        /// This constructor is only for serialisation.
        public Ship()
            : base()
        {
            this.thrustForce = 100;
            this.turnSpeed = 3;
            this.maxSpeed = 200;
            this.weapon1 = null;
            this.weapon2 = null;
            this.rollAngle = 0;
            this.rollMax = (float)MathHelper.PiOver4;
            this.rollSpeed = (float)(MathHelper.TwoPi / 2.0);
            this.weapon1ChargeMax = 5000;
            this.weapon1ChargeSpeed = 500;
            this.weapon1Charge = this.weapon1ChargeMax;
            this.weapon2ChargeMax = 5000;
            this.weapon2ChargeSpeed = 500;
            this.weapon2Charge = this.weapon2ChargeMax;
            this.armour = new Curve();
            this.armour.PreLoop = CurveLoopType.Linear;
            this.armour.PostLoop = CurveLoopType.Linear;
            this.armour.Keys.Add(new CurveKey(-500, -500, 1, 500 * 1, CurveContinuity.Smooth));
            this.armour.Keys.Add(new CurveKey(0, 0, 500 * 1, 10 * 0.3f, CurveContinuity.Smooth));
            this.armour.Keys.Add(new CurveKey(10, 7, 10 * 1, 40 * 1, CurveContinuity.Smooth));
            this.armour.Keys.Add(new CurveKey(50, 50, 40 * 1, 450 * 1, CurveContinuity.Smooth));
            this.armour.Keys.Add(new CurveKey(500, 500, 450 * 1, 1, CurveContinuity.Smooth));
            this.birthAlpha = new Curve();
            this.birthAlpha.PreLoop = CurveLoopType.Constant;
            this.birthAlpha.PostLoop = CurveLoopType.Constant;
            for (float age = 0; age + 0.2f < 2; age += 0.4f)
            {
                this.birthAlpha.Keys.Add(new CurveKey(age, 0.2f));
                this.birthAlpha.Keys.Add(new CurveKey(age + 0.2f, 0.8f));
            }
            this.birthAlpha.Keys.Add(new CurveKey(2, 1));
            this.birthAlpha.ComputeTangents(CurveTangent.Flat);
            this.coughEngineNames = new string[] { "dummyparticleengine", };
        }

        /// <summary>
        /// Creates a new ship.
        /// </summary>
        /// <param name="typeName">Type of the ship.</param>
        public Ship(string typeName)
            : base(typeName)
        {
            this.weapon1 = null;
            this.weapon2 = null;
            this.weapon1Charge = this.weapon1ChargeMax;
            this.weapon2Charge = this.weapon2ChargeMax;
        }

        /// <summary>
        /// Creates a new ship.
        /// </summary>
        /// <param name="typeName">Type of the ship.</param>
        /// <param name="owner">Owner of the ship.</param>
        /// <param name="pos">Initial position of the ship.</param>
        /// <param name="weapon1Name">Name of the primary weapon type.</param>
        /// <param name="weapon2Name">Name of the secondary weapon type.</param>
        public Ship(string typeName, Player owner, Vector2 pos, string weapon1Name, string weapon2Name)
            : base(typeName, owner, pos, Vector2.Zero, Gob.defaultRotation)
        {
            this.weapon1 = CreateWeapons(weapon1Name, 1);
            this.weapon2 = CreateWeapons(weapon2Name, 2);
            this.weapon1Charge = this.weapon1ChargeMax;
            this.weapon2Charge = this.weapon2ChargeMax;
            this.coughEngines = new ParticleEngine[0];
        }

        #endregion Ship constructors

        /// <summary>
        /// Creates a new instance of a named weapon type so that all
        /// gun barrels on the ship are covered.
        /// </summary>
        /// <param name="weaponName">Name of the weapon type.</param>
        /// <param name="ownerHandle">A handle for identifying the weapon at the owner.
        /// Use <b>1</b> for primary weapons and <b>2</b> for secondary weapons.</param>
        /// <returns>The created weapon.</returns>
        private Weapon CreateWeapons(string weaponName, int ownerHandle)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            KeyValuePair<string, int>[] boneIs = GetNamedPositions("Gun");
            if (boneIs.Length == 0)
                Log.Write("Warning: Ship found no gun barrels in its 3D model");
            int[] boneIndices = Array.ConvertAll<KeyValuePair<string, int>, int>(boneIs,
                delegate(KeyValuePair<string, int> pair) { return pair.Value; });
            Weapon weapon = Weapon.CreateWeapon(weaponName, this, ownerHandle, boneIndices);

            // Apply appropriate player bonuses.
            if (ownerHandle == 1 && (Owner.Bonuses & PlayerBonus.Weapon1LoadTime) != 0)
                weapon.LoadTime /= 2;
            if (ownerHandle == 2 && (Owner.Bonuses & PlayerBonus.Weapon2LoadTime) != 0)
                weapon.LoadTime /= 2;

            data.AddWeapon(weapon);
            return weapon;
        }

        /// <summary>
        /// Creates cough engines for the ship.
        /// </summary>
        private void CreateCoughEngines()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            coughEngines = new ParticleEngine[coughEngineNames.Length];
            for (int i = 0; i < coughEngineNames.Length; ++i)
            {
                coughEngines[i] = new ParticleEngine(coughEngineNames[i]);
                coughEngines[i].Loop = true;
                coughEngines[i].IsAlive = false;
                coughEngines[i].Leader = this;
                data.AddParticleEngine(coughEngines[i]);
            }
        }

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            foreach (ParticleEngine engine in exhaustEngines)
                engine.IsAlive = false;
            exhaustAmountUpdated = false;
            CreateCoughEngines();
        }

        /// <summary>
        /// Updates the ship's internal state.
        /// </summary>
        public override void Update()
        {
            // Manage turn-related rolling.
            rollAngle = AWMathHelper.InterpolateTowards(rollAngle, rollAngleGoal, 
                physics.ApplyChange(rollSpeed));
            if (!rollAngleGoalUpdated)
                rollAngleGoal = 0;
            rollAngleGoalUpdated = false;

            base.Update();
            
            // Manage exhaust engines.
            for (int i = 0; i < exhaustEngines.Length; ++i)
                if (!exhaustAmountUpdated)
                    exhaustEngines[i].IsAlive = false;
            exhaustAmountUpdated = false;

            // Manage cough engines.
            float coughArgument = (DamageLevel / MaxDamageLevel - 0.8f) / 0.2f;
            coughArgument = MathHelper.Clamp(coughArgument, 0, 1);
            foreach (ParticleEngine coughEngine in coughEngines)
            {
                coughEngine.Argument = coughArgument;
                coughEngine.IsAlive = coughArgument > 0;
            }

            // Update weapon charges.
            weapon1Charge += physics.ApplyChange(weapon1ChargeSpeed);
            weapon1Charge = MathHelper.Clamp(weapon1Charge, 0, weapon1ChargeMax);
            weapon2Charge += physics.ApplyChange(weapon2ChargeSpeed);
            weapon2Charge = MathHelper.Clamp(weapon2Charge, 0, weapon2ChargeMax);

            // Flash and be disabled if we're just born.
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Model model = data.GetModel(ModelName);
            float age = (float)(AssaultWing.Instance.GameTime.TotalGameTime - birthTime).TotalSeconds;
            float alpha = birthAlpha.Evaluate(age);
            foreach (ModelMesh mesh in model.Meshes)
                foreach (BasicEffect be in mesh.Effects)
                    be.Alpha = alpha;
            Disabled = age < birthAlpha.Keys[birthAlpha.Keys.Count - 1].Position;
        }

        /// <summary>
        /// Kills the gob, i.e. performs a death ritual and removes the gob from the game world.
        /// </summary>
        public override void Die()
        {
            if (Dead) return;
            if (Owner != null)
                Owner.Die();
            base.Die();
        }

        /// <summary>
        /// Releases all resources allocated by the gob.
        /// </summary>
        public override void Dispose()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            data.RemoveWeapon(weapon1);
            data.RemoveWeapon(weapon2);
            base.Dispose();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        /// <summary>
        /// Copies the gob's runtime state from another gob.
        /// </summary>
        /// <param name="runtimeState">The gob whose runtime state to imitate.</param>
        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);
            exhaustAmountUpdated = false;
        }

        #endregion Methods related to serialisation

        #region Ship public methods

        /// <summary>
        /// Thrusts the ship.
        /// </summary>
        /// <param name="force">Force of thrust; between 0 and 1.</param>
        public void Thrust(float force)
        {
            if (Disabled) return;
            force = MathHelper.Clamp(force, 0f, 1f);
            Vector2 forceVector = new Vector2((float)Math.Cos(Rotation), (float)Math.Sin(Rotation))
                * force * thrustForce;
            physics.ApplyLimitedForce(this, forceVector, maxSpeed);

            // Manage exhaust engine.
            foreach (ParticleEngine exhaustEngine in exhaustEngines)
                exhaustEngine.IsAlive = true;
            exhaustAmountUpdated = true;
        }

        /// <summary>
        /// Turns the ship left.
        /// </summary>
        /// <param name="force">Force of turn; between 0 and 1.</param>
        public void TurnLeft(float force)
        {
            if (Disabled) return;
            force = MathHelper.Clamp(force, 0f, 1f);
            Turn(force);
        }

        /// <summary>
        /// Turns the ship right.
        /// </summary>
        /// <param name="force">Force of turn; between 0 and 1.</param>
        public void TurnRight(float force)
        {
            if (Disabled) return;
            force = MathHelper.Clamp(force, 0f, 1f);
            Turn(-force);
        }

        /// <summary>
        /// Turns the ship right or left.
        /// </summary>
        /// <param name="force">Force of turn; (0,1] for a left turn, or [-1,0) for a right turn.</param>
        private void Turn(float force)
        {
            force = MathHelper.Clamp(force, -1f, 1f);
            Rotation += physics.ApplyChange(force * turnSpeed);

            Vector2 headingNormal = Vector2.Transform(Vector2.UnitX, Matrix.CreateRotationZ(Rotation));
            float moveLength = Move.Length();
            float headingFactor = // fancy roll
                moveLength == 0 ? 0 :
                moveLength <= maxSpeed ? Vector2.Dot(headingNormal, Move / maxSpeed) :
                Vector2.Dot(headingNormal, Move / moveLength);
            //float headingFactor = 1.0f; // naive roll
            rollAngleGoal = -rollMax * force * headingFactor;
            rollAngleGoalUpdated = true;
        }

        /// <summary>
        /// Fires the main weapon.
        /// </summary>
        public void Fire1()
        {
            if (Disabled) return;
            weapon1.Fire();
        }

        /// <summary>
        /// Fires the secondary weapon.
        /// </summary>
        public void Fire2()
        {
            if (Disabled) return;
            weapon2.Fire();
        }

        /// <summary>
        /// Performs an extra function which depends on the ship state.
        /// </summary>
        public void DoExtra()
        {
            if (Disabled) return;
            // !!! not implemented
        }

        /// <summary>
        /// Returns the amount of charge available for a weapon with
        /// a certain handle.
        /// </summary>
        /// <param name="ownerHandle">The owner handle of the weapon.</param>
        /// <returns>The amount of charge available for the weapon.</returns>
        public float GetCharge(int ownerHandle)
        {
            switch (ownerHandle)
            {
                case 1: return Weapon1Charge;
                case 2: return Weapon2Charge;
                default:
                    Log.Write("Warning: Someone inquired weapon charge for owner handle "
                        + ownerHandle);
                    return 0;
            }
        }
        
        /// <summary>
        /// Uses an amount of charge available for a weapon with
        /// a certain handle.
        /// </summary>
        /// <param name="ownerHandle">The owner handle of the weapon.</param>
        /// <param name="amount">The amount of charge to use.</param>
        public void UseCharge(int ownerHandle, float amount)
        {
            switch (ownerHandle)
            {
                case 1:
                    weapon1Charge = MathHelper.Clamp(weapon1Charge - amount, 0, weapon1ChargeMax);
                    break;
                case 2:
                    weapon2Charge = MathHelper.Clamp(weapon2Charge - amount, 0, weapon2ChargeMax);
                    break;
                default:
                    Log.Write("Warning: Someone tried to use weapon charge for owner handle "
                        + ownerHandle);
                    break;
            }
        }

        #endregion Ship public methods

        /// <summary>
        /// Inflicts damage on the entity.
        /// </summary>
        /// <param name="damageAmount">If positive, amount of damage;
        /// if negative, amount of repair.</param>
        public override void InflictDamage(float damageAmount)
        {
            float realDamage = armour.Evaluate(damageAmount);
            if (Owner != null)
                Owner.IncreaseShake(realDamage);
            base.InflictDamage(realDamage);
        }
    }
}
