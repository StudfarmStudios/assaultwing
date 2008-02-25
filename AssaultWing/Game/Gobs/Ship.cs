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
    public class Ship : Gob, ISolid, IDamageable
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
        Weapon[] weapons1;

        /// <summary>
        /// Secondary weapons of the ship.
        /// </summary>
        [RuntimeState]
        Weapon[] weapons2;

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

        #region Ship fields related to other things

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
        /// Name of the type of main weapon the ship is using.
        /// </summary>
        public string Weapon1Name
        {
            set
            {
                if (weapons1 != null)
                {
                    DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                    foreach (Weapon weapon in weapons1)
                        data.RemoveWeapon(weapon);
                }
                weapons1 = CreateWeapons(value);
            }
        }

        /// <summary>
        /// Name of the type of secondary weapon the ship is using.
        /// </summary>
        public string Weapon2Name
        {
            set
            {
                if (weapons2 != null)
                {
                    DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                    foreach (Weapon weapon in weapons2)
                        data.RemoveWeapon(weapon);
                }
                weapons2 = CreateWeapons(value);
            }
        }

        /// <summary>
        /// Is any of the primary weapons loaded.
        /// </summary>
        public bool Weapon1Loaded
        {
            get
            {
                foreach (Weapon weapon in weapons1)
                    if (weapon.Loaded) return true;
                return false;
            }
        }

        /// <summary>
        /// Is any of the secondary weapons loaded.
        /// </summary>
        public bool Weapon2Loaded
        {
            get
            {
                foreach (Weapon weapon in weapons2)
                    if (weapon.Loaded) return true;
                return false;
            }
        }

        /// <summary>
        /// Amount of charge for primary weapons,
        /// between <b>0</b> and <b>Weapon1ChargeMax</b>.
        /// </summary>
        public float Weapon1Charge { get { return weapon1Charge; } }

        /// <summary>
        /// Maximum amount of charge for primary weapons.
        /// </summary>
        public float Weapon1ChargeMax { get { return weapon1ChargeMax; } }

        /// <summary>
        /// Amount of charge for secondary weapons,
        /// between <b>0</b> and <b>Weapon2ChargeMax</b>.
        /// </summary>
        public float Weapon2Charge { get { return weapon2Charge; } }

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
        public Ship() : base() 
        {
            this.thrustForce = 100;
            this.turnSpeed = 3;
            this.maxSpeed = 200;
            this.weapons1 = null;
            this.weapons2 = null;
            this.rollAngle = 0;
            this.rollMax = (float)MathHelper.PiOver4;
            this.rollSpeed = (float)(MathHelper.TwoPi / 2.0);
            this.weapon1ChargeMax = 5000;
            this.weapon1ChargeSpeed = 500;
            this.weapon1Charge = this.weapon1ChargeMax;
            this.weapon2ChargeMax = 5000;
            this.weapon2ChargeSpeed = 500;
            this.weapon2Charge = this.weapon2ChargeMax;
        }

        /// <summary>
        /// Creates a new ship.
        /// </summary>
        /// <param name="typeName">Type of the ship.</param>
        public Ship(string typeName)
            : base(typeName)
        {
            this.weapons1 = null;
            this.weapons2 = null;
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
            this.weapons1 = CreateWeapons(weapon1Name);
            this.weapons2 = CreateWeapons(weapon2Name);
            this.weapon1Charge = this.weapon1ChargeMax;
            this.weapon2Charge = this.weapon2ChargeMax;
        }

        #endregion Ship constructors

        /// <summary>
        /// Creates new instances of a named weapon type, one instance for each
        /// gun barrel on the ship.
        /// </summary>
        /// <param name="weaponName">Name of the weapon type.</param>
        /// <returns>The list of created weapons.</returns>
        private Weapon[] CreateWeapons(string weaponName)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            KeyValuePair<string, int>[] boneIs = GetNamedPositions("Gun");
            if (boneIs.Length == 0)
                Log.Write("Warning: Ship found no gun barrels in its 3D model");
            Weapon[] weapons = new Weapon[boneIs.Length];
            for (int i = 0; i < boneIs.Length; ++i)
            {
                Weapon weapon = Weapon.CreateWeapon(weaponName, this, boneIs[i].Value);
                data.AddWeapon(weapon);
                weapons[i] = weapon;
            }
            return weapons;
        }

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            this.CreateExhaustEngines();
            this.exhaustAmountUpdated = false;
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
            // We do this after the ship's position has been updated by
            // base.Update() to get exhaust fumes in the right spot.
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Model model = data.GetModel(ModelName);
            UpdateModelPartTransforms(model);
            for (int i = 0; i < exhaustEngines.Length; ++i)
            {
                exhaustEngines[i].Position = new Vector3(GetNamedPosition(exhaustBoneIs[i]), 0);
                ((DotEmitter)exhaustEngines[i].Emitter).Direction = Rotation + MathHelper.Pi;
                if (!exhaustAmountUpdated)
                    exhaustEngines[i].IsAlive = false;
            }
            exhaustAmountUpdated = false;

            // Update weapon charges.
            weapon1Charge += physics.ApplyChange(weapon1ChargeSpeed);
            weapon1Charge = MathHelper.Clamp(weapon1Charge, 0, weapon1ChargeMax);
            weapon2Charge += physics.ApplyChange(weapon2ChargeSpeed);
            weapon2Charge = MathHelper.Clamp(weapon2Charge, 0, weapon2ChargeMax);
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
            foreach (Weapon weapon in weapons1)
                data.RemoveWeapon(weapon);
            foreach (Weapon weapon in weapons2)
                data.RemoveWeapon(weapon);
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
            CreateExhaustEngines();
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
            force = MathHelper.Clamp(force, 0f, 1f);
            Turn(force);
        }

        /// <summary>
        /// Turns the ship right.
        /// </summary>
        /// <param name="force">Force of turn; between 0 and 1.</param>
        public void TurnRight(float force)
        {
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
            if (Weapon1Loaded && weapon1Charge >= weapons1[0].FireCharge)
            {
                weapon1Charge -= weapons1[0].FireCharge;
                foreach (Weapon weapon in weapons1)
                    weapon.Fire();
            }
        }

        /// <summary>
        /// Fires the secondary weapon.
        /// </summary>
        public void Fire2()
        {
            if (Weapon2Loaded && weapon2Charge >= weapons2[0].FireCharge)
            {
                weapon2Charge -= weapons2[0].FireCharge;
                foreach (Weapon weapon in weapons2)
                    weapon.Fire();
            }
        }

        /// <summary>
        /// Performs an extra function which depends on the ship state.
        /// </summary>
        public void DoExtra()
        {
            // !!! not implemented
        }

        #endregion Ship public methods

        #region ICollidable Members
        // Some members are implemented in class Gob.

        #endregion

        #region IDamageable Members
        // Some members are implemented in class Gob.

        #endregion

    }
}
