#if !DEBUG
#define OPTIMIZED_CODE // replace some function calls with fast elementary operations
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A player's ship.
    /// </summary>
    public class Ship : Gob
    {
        private const string SHIP_BIRTH_SOUND = "ShipBirth";

        #region Ship fields related to flying

        /// <summary>
        /// Maximum force of thrust of the ship, measured in Newtons.
        /// </summary>
        [TypeParameter]
        private float thrustForce;

        /// <summary>
        /// Maximum turning speed of the ship, measured in radians per second.
        /// </summary>
        [TypeParameter]
        private float turnSpeed;

        /// <summary>
        /// Ship's maximum speed reachable by thrust, measured in meters per second.
        /// </summary>
        [TypeParameter]
        private float maxSpeed;

        #endregion Ship fields related to flying

        #region Ship fields related to weapons

        /// <summary>
        /// Name of the type of primary weapon the ship type uses.
        /// </summary>
        [TypeParameter]
        private CanonicalString weapon1TypeName;

        /// <summary>
        /// Maximum amount of charge for extra devices.
        /// </summary>
        [TypeParameter]
        private float extraDeviceChargeMax;

        /// <summary>
        /// Maximum amount of charge for secondary weapons.
        /// </summary>
        [TypeParameter]
        private float weapon2ChargeMax;

        /// <summary>
        /// Speed of charging for extra device charge,
        /// measured in charge units per second.
        /// </summary>
        [TypeParameter]
        private float extraDeviceChargeSpeed;

        /// <summary>
        /// Speed of charging for secondary weapon charge,
        /// measured in charge units per second.
        /// </summary>
        [TypeParameter]
        private float weapon2ChargeSpeed;

        private bool _isActivated;

        #endregion Ship fields related to weapons

        #region Ship fields related to rolling

        private InterpolatingValue _rollAngle;
        private bool _rollAngleGoalUpdated;

        /// <summary>
        /// Maximum angle of rotation of the ship around its tail-to-head axis
        /// Minimum roll angle will be the additive inverse.
        /// </summary>
        [TypeParameter]
        private float rollMax;

        /// <summary>
        /// Roll angle change speed in radians per second.
        /// </summary>
        [TypeParameter]
        private float rollSpeed;

        #endregion Ship fields related to rolling

        #region Ship fields related to coughing

        [TypeParameter, ShallowCopy]
        private CanonicalString[] coughEngineNames;

        private Gob[] _coughEngines;

        #endregion Ship fields related to coughing

        #region Ship fields related to other things

        /// <summary>
        /// Armour of the ship as a function that maps
        /// the amount of damage delivered to the ship
        /// to the amount of damage the ship actually receives.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private Curve armour;

        /// <summary>
        /// Alpha of the ship as a function that maps the age of the
        /// ship (in seconds) to the alpha value to draw the ship with.
        /// </summary>
        /// Use this to implement alpha flashing on ship birth.
        [TypeParameter, ShallowCopy]
        private Curve birthAlpha;

        /// <summary>
        /// Name of the ship's icon in the equip menu main display.
        /// </summary>
        [TypeParameter]
        private CanonicalString iconEquipName;

        /// <summary>
        /// True iff the amount of exhaust output has been set by ship thrusting this frame.
        /// </summary>
        private bool _exhaustAmountUpdated;

        /// <summary>
        /// Gobs that we have temporarily disabled while we move through them.
        /// </summary>
        private List<Gob> _temporarilyDisabledGobs;

        private bool _isBirthFlashing;

        #endregion Ship fields related to other things

        #region Ship fields for signalling visual things over the network

        private float _visualThrustForce;

        #endregion Ship fields for signalling visual things over the network

        #region Ship properties

        /// <summary>
        /// Sets <see cref="Pos"/>, <see cref="Move"/> and <see cref="Rotation"/>
        /// as if the gob appeared there instantaneously
        /// as opposed to moving there in a continuous fashion.
        /// </summary>
        public override void ResetPos(Vector2 pos, Vector2 move, float rotation)
        {
            base.ResetPos(pos, move, rotation);
            if (LocationPredicter != null) LocationPredicter.ForgetOldShipLocations();
        }

        /// <summary>
        /// Returns the world matrix of the gob, i.e., the translation from
        /// game object coordinates to game world coordinates.
        /// </summary>
        public override Matrix WorldMatrix
        {
            get
            {
#if OPTIMIZED_CODE
                float scale = Scale;
                float rotation = Rotation;
                float scaledCosRoll = scale * (float)Math.Cos(rollAngle.Current);
                float scaledSinRoll = scale * (float)Math.Sin(rollAngle.Current);
                float cosRota = (float)Math.Cos(rotation);
                float sinRota = (float)Math.Sin(rotation);
                return new Matrix(
                    scale*cosRota, scale*sinRota, 0, 0,
                    -scaledCosRoll*sinRota, scaledCosRoll*cosRota, scaledSinRoll, 0,
                    scaledSinRoll*sinRota, -scaledSinRoll*cosRota, scaledCosRoll, 0,
                    pos.X, pos.Y, 0, 1);
#else
                return Matrix.CreateScale(Scale)
                     * Matrix.CreateRotationX(_rollAngle.Current)
                     * Matrix.CreateRotationZ(Rotation)
                     * Matrix.CreateTranslation(new Vector3(Pos, 0));
#endif
            }
        }

        public float TurnSpeed { get { return turnSpeed; } }

        public float ThrustForce { get { return thrustForce; } }

        /// <summary>
        /// Name of the type of main weapon the ship is using. Same as
        /// <c>Weapon1.TypeName</c> but works even when <see cref="Weapon1"/> is null.
        /// </summary>
        public CanonicalString Weapon1Name { get { return weapon1TypeName; } }

        /// <summary>
        /// Name of the type of secondary weapon the ship is using. Same as
        /// <c>Weapon2.TypeName</c> but works even when <see cref="Weapon2"/> is null.
        /// </summary>
        public CanonicalString Weapon2Name { get; private set; }

        /// <summary>
        /// Name of the type of extra device the ship is using. Same as
        /// <c>ExtraDevice.TypeName</c> but works even when <see cref="ExtraDevice"/> is null.
        /// </summary>
        public CanonicalString ExtraDeviceName { get; set; }

        public Weapon Weapon1 { get; private set; }
        public Weapon Weapon2 { get; private set; }
        public ShipDevice ExtraDevice { get; private set; }

        /// <summary>
        /// Name of the ship's icon in the equip menu main display.
        /// </summary>
        public CanonicalString IconEquipName { get { return iconEquipName; } set { iconEquipName = value; } }

        /// <summary>
        /// Names of all textures that this gob type will ever use.
        /// </summary>
        public override IEnumerable<CanonicalString> TextureNames
        {
            get { return base.TextureNames.Union(new CanonicalString[] { iconEquipName }); }
        }

        #endregion Ship properties

        #region Ship constructors

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Ship()
        {
            thrustForce = 100;
            turnSpeed = 3;
            maxSpeed = 200;
            rollMax = (float)MathHelper.PiOver4;
            rollSpeed = (float)(MathHelper.TwoPi / 2.0);
            weapon1TypeName = (CanonicalString)"dummyweapon";
            extraDeviceChargeMax = 5000;
            extraDeviceChargeSpeed = 500;
            weapon2ChargeMax = 5000;
            weapon2ChargeSpeed = 500;
            armour = new Curve();
            armour.PreLoop = CurveLoopType.Linear;
            armour.PostLoop = CurveLoopType.Linear;
            armour.Keys.Add(new CurveKey(-500, -500, 1, 500 * 1, CurveContinuity.Smooth));
            armour.Keys.Add(new CurveKey(0, 0, 500 * 1, 10 * 0.3f, CurveContinuity.Smooth));
            armour.Keys.Add(new CurveKey(10, 7, 10 * 1, 40 * 1, CurveContinuity.Smooth));
            armour.Keys.Add(new CurveKey(50, 50, 40 * 1, 450 * 1, CurveContinuity.Smooth));
            armour.Keys.Add(new CurveKey(500, 500, 450 * 1, 1, CurveContinuity.Smooth));
            birthAlpha = new Curve();
            birthAlpha.PreLoop = CurveLoopType.Constant;
            birthAlpha.PostLoop = CurveLoopType.Constant;
            for (float age = 0; age + 0.2f < 2; age += 0.4f)
            {
                birthAlpha.Keys.Add(new CurveKey(age, 0.2f));
                birthAlpha.Keys.Add(new CurveKey(age + 0.2f, 0.8f));
            }
            birthAlpha.Keys.Add(new CurveKey(2, 1));
            birthAlpha.ComputeTangents(CurveTangent.Flat);
            coughEngineNames = new CanonicalString[] { (CanonicalString)"dummypeng" };
            _temporarilyDisabledGobs = new List<Gob>();
            iconEquipName = (CanonicalString)"dummytexture";
        }

        public Ship(CanonicalString typeName)
            : base(typeName)
        {
            _temporarilyDisabledGobs = new List<Gob>();
            LocationPredicter = new ShipLocationPredicter(this);
        }

        #endregion Ship constructors

        #region Protected methods

        /// <summary>
        /// Called when the ship is thrusting.
        /// </summary>
        protected virtual void Thrusting(float thrustForce) { }

        /// <summary>
        /// Called when the ship is turning.
        /// </summary>
        protected virtual void Turning(float turnAngle) { }

        #endregion Protected methods

        #region Private methods

        private void CreateCoughEngines()
        {
            var coughEngineList = new List<Gob>();
            for (int i = 0; i < coughEngineNames.Length; ++i)
            {
                Gob.CreateGob(coughEngineNames[i], gob =>
                {
                    if (gob is Peng)
                    {
                        Peng peng = (Peng)gob;
                        peng.Paused = true;
                        peng.Leader = this;
                    }
                    Arena.Gobs.Add(gob);
                    coughEngineList.Add(gob);
                });
            }
            _coughEngines = coughEngineList.ToArray();
        }

        private void CreateGlow()
        {
            if (Owner == null) return;
            Gob.CreateGob((CanonicalString)"playerglow", gob =>
            {
                if (gob is Peng)
                {
                    var peng = (Peng)gob;
                    peng.Owner = Owner;
                    peng.Leader = this;
                }
                AssaultWing.Instance.DataEngine.Arena.Gobs.Add(gob);
            });
        }

        #endregion Private methods

        #region Methods related to gobs' functionality in the game world

        public override void Activate()
        {
            base.Activate();
            _isActivated = true;

            // Deferred initialization of ship devices
            if (Weapon1 == null && Weapon1Name != CanonicalString.Null) SetDeviceType(ShipDevice.OwnerHandleType.PrimaryWeapon, Weapon1Name);
            if (Weapon2 == null && Weapon2Name != CanonicalString.Null) SetDeviceType(ShipDevice.OwnerHandleType.SecondaryWeapon, Weapon2Name);
            if (ExtraDevice == null && ExtraDeviceName != CanonicalString.Null) SetDeviceType(ShipDevice.OwnerHandleType.ExtraDevice, ExtraDeviceName);

            SwitchExhaustEngines(false);
            _exhaustAmountUpdated = false;
            CreateCoughEngines();
            CreateGlow();
            Disable(); // re-enabled in Update()
            _isBirthFlashing = true;
            AssaultWing.Instance.SoundEngine.PlaySound(SHIP_BIRTH_SOUND);
        }

        public override void Update()
        {
            var elapsedGameTime = AssaultWing.Instance.GameTime.ElapsedGameTime;

            // Manage turn-related rolling.
            _rollAngle.Step = AssaultWing.Instance.PhysicsEngine.ApplyChange(rollSpeed, elapsedGameTime);
            _rollAngle.Advance();
            if (!_rollAngleGoalUpdated)
                _rollAngle.Target = 0;
            _rollAngleGoalUpdated = false;

            LocationPredicter.StoreOldShipLocation(new ShipLocationEntry
            {
                GameTime = Arena.TotalTime - elapsedGameTime,
                Pos = Pos,
                Move = Move,
                Rotation = Rotation
            });
            base.Update();
            
            // Re-enable temporarily disabled gobs.
            foreach (Gob gob in _temporarilyDisabledGobs) gob.Enable();
            _temporarilyDisabledGobs.Clear();

            // Manage exhaust engines.
            if (!_exhaustAmountUpdated)
                SwitchExhaustEngines(false);
            _exhaustAmountUpdated = false;

            // Manage cough engines.
            float coughArgument = (DamageLevel / MaxDamageLevel - 0.8f) / 0.2f;
            coughArgument = MathHelper.Clamp(coughArgument, 0, 1);
            foreach (var coughEngine in _coughEngines)
            {
                var peng = coughEngine as Peng;
                if (peng != null)
                {
                    peng.Input = coughArgument;
                    peng.Paused = coughArgument == 0;
                }
            }

            ExtraDevice.Charge += extraDeviceChargeSpeed * (float)elapsedGameTime.TotalSeconds;
            Weapon2.Charge += weapon2ChargeSpeed * (float)elapsedGameTime.TotalSeconds;

            if (_isBirthFlashing)
            {
                float age = birthTime.SecondsAgoGameTime();
                Alpha = birthAlpha.Evaluate(age);
                if (age >= birthAlpha.Keys[birthAlpha.Keys.Count - 1].Position)
                {
                    Enable();
                    _isBirthFlashing = false;
                }
            }
        }

        public override void Die(DeathCause cause)
        {
            if (Dead) return;
            if (Owner != null)
                Owner.Die(cause);
            base.Die(cause);
        }

        public override void Dispose()
        {
            AssaultWing.Instance.DataEngine.Devices.Remove(Weapon1);
            AssaultWing.Instance.DataEngine.Devices.Remove(Weapon2);
            AssaultWing.Instance.DataEngine.Devices.Remove(ExtraDevice);
            base.Dispose();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);
            _exhaustAmountUpdated = false;
        }

        public override void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                if (Weapon2 != null) writer.Write((int)Weapon2.TypeName.Canonical);
                else writer.Write((int)CanonicalString.Null.Canonical);
                if (ExtraDevice != null) writer.Write((int)ExtraDevice.TypeName.Canonical);
                else writer.Write((int)CanonicalString.Null.Canonical);
            }
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((Half)_visualThrustForce);
                _visualThrustForce = 0;
            }
            Weapon1.Serialize(writer, mode);
            Weapon2.Serialize(writer, mode);
            ExtraDevice.Serialize(writer, mode);
        }

        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode, TimeSpan messageAge)
        {
            base.Deserialize(reader, mode, messageAge);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                var typeName = (CanonicalString)reader.ReadInt32();
                if (!typeName.IsNull) SetDeviceType(ShipDevice.OwnerHandleType.SecondaryWeapon, typeName);
                typeName = (CanonicalString)reader.ReadInt32();
                if (!typeName.IsNull) SetDeviceType(ShipDevice.OwnerHandleType.ExtraDevice, typeName);
            }
            if ((mode & AW2.Net.SerializationModeFlags.VaryingData) != 0)
            {
                float thrustForce = reader.ReadHalf();
                if (thrustForce > 0)
                    Thrust(thrustForce, AssaultWing.Instance.GameTime.ElapsedGameTime, Rotation);
            }
            if (Weapon1 != null) Weapon1.Deserialize(reader, mode, messageAge);
            if (Weapon2 != null) Weapon2.Deserialize(reader, mode, messageAge);
            if (ExtraDevice != null) ExtraDevice.Deserialize(reader, mode, messageAge);
        }

        #endregion Methods related to serialisation

        #region Ship public methods

        /// <summary>
        /// Thrusts the ship.
        /// </summary>
        /// <param name="force">Thrust force factor relative to ship's maximum thrust.</param>
        public void Thrust(float force, TimeSpan duration, float direction)
        {
            if (Disabled) return;
            Vector2 forceVector = AWMathHelper.GetUnitVector2(direction) * force * thrustForce;
            AssaultWing.Instance.PhysicsEngine.ApplyLimitedForce(this, forceVector, maxSpeed, duration);
            _visualThrustForce = force;
            Thrusting(force);

            // Manage exhaust engines.
            SwitchExhaustEngines(true);
            _exhaustAmountUpdated = true;
        }

        /// <param name="force">Force of turn; between 0 and 1.</param>
        public void TurnLeft(float force, TimeSpan duration)
        {
            if (Disabled) return;
            force = MathHelper.Clamp(force, 0f, 1f);
            Turn(force, duration);
        }

        /// <param name="force">Force of turn; between 0 and 1.</param>
        public void TurnRight(float force, TimeSpan duration)
        {
            if (Disabled) return;
            force = MathHelper.Clamp(force, 0f, 1f);
            Turn(-force, duration);
        }

        /// <param name="force">Force of turn; (0,1] for a left turn, or [-1,0) for a right turn.</param>
        private void Turn(float force, TimeSpan duration)
        {
            force = MathHelper.Clamp(force, -1f, 1f);
            float deltaRotation = AssaultWing.Instance.PhysicsEngine.ApplyChange(force * turnSpeed, duration);
            Rotation += deltaRotation;
            Turning(deltaRotation);

            Vector2 headingNormal = Vector2.Transform(Vector2.UnitX, Matrix.CreateRotationZ(Rotation));
            float moveLength = Move.Length();
            float headingFactor = // fancy roll
                moveLength == 0 ? 0 :
                moveLength <= maxSpeed ? Vector2.Dot(headingNormal, Move / maxSpeed) :
                Vector2.Dot(headingNormal, Move / moveLength);
            //float headingFactor = 1.0f; // naive roll
            _rollAngle.Target = -rollMax * force * headingFactor;
            _rollAngleGoalUpdated = true;
        }

        #endregion Ship public methods

        public override void Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck)
        {
            if (stuck)
            {
                // Set the other gob as disabled while we move, then enable it after we finish moving.
                // This works with the assumption that there are at least two moving iterations.
                theirArea.Owner.Disable(); // re-enabled in Update()
                _temporarilyDisabledGobs.Add(theirArea.Owner);
            }
        }

        public override void InflictDamage(float damageAmount, DeathCause cause)
        {
            float realDamage = armour.Evaluate(damageAmount);
            if (Owner != null)
                Owner.IncreaseShake(realDamage);
            base.InflictDamage(realDamage, cause);
        }

        public ShipLocationPredicter LocationPredicter { get; private set; }

        public ChargeProvider GetChargeProvider(ShipDevice.OwnerHandleType deviceType)
        {
            switch (deviceType)
            {
                case ShipDevice.OwnerHandleType.PrimaryWeapon:
                    return new ChargeProvider(() => int.MaxValue, () => 0);
                case ShipDevice.OwnerHandleType.SecondaryWeapon:
                    return new ChargeProvider(() => weapon2ChargeMax, () => weapon2ChargeSpeed);
                case ShipDevice.OwnerHandleType.ExtraDevice:
                    return new ChargeProvider(() => extraDeviceChargeMax, () => extraDeviceChargeSpeed);
                default: throw new ApplicationException("Unknown ship device type " + deviceType);
            }
        }

        public void SetDeviceLoadMultiplier(ShipDevice.OwnerHandleType g_deviceType, float g_multiplier)
        {
            var device = GetDevice(g_deviceType);
            device.LoadTimeMultiplier = g_multiplier;
        }
        
        public float GetDeviceLoadMultiplier(ShipDevice.OwnerHandleType g_deviceType)
        {
            var device = GetDevice(g_deviceType);
            return device.LoadTimeMultiplier;
        }

        private ShipDevice GetDevice(ShipDevice.OwnerHandleType deviceType)
        {
            ShipDevice device;
            switch (deviceType)
            {
                case ShipDevice.OwnerHandleType.PrimaryWeapon: device=Weapon1; break;
                case ShipDevice.OwnerHandleType.SecondaryWeapon: device = Weapon2; break;
                case ShipDevice.OwnerHandleType.ExtraDevice: device = ExtraDevice; break;
                default: throw new ApplicationException("Unknown Weapon.OwnerHandleType " + deviceType);
            }
            return device;
        }

        public void SetDeviceType(ShipDevice.OwnerHandleType deviceType, CanonicalString typeName)
        {
            switch (deviceType)
            {
                case ShipDevice.OwnerHandleType.PrimaryWeapon:
                    if (typeName != weapon1TypeName)
                        throw new InvalidOperationException("Cannot set Ship primary weapon " + typeName +
                            ", fixed primary weapon is " + weapon1TypeName);
                    break;
                case ShipDevice.OwnerHandleType.SecondaryWeapon: Weapon2Name = typeName; break;
                case ShipDevice.OwnerHandleType.ExtraDevice: ExtraDeviceName = typeName; break;
                default: throw new ApplicationException("Unknown Weapon.OwnerHandleType " + deviceType);
            }

            // If we are not activated, the next call to Activate() will create the device.
            // Creating weapons is deferred until the ship is activated because it
            // requires looking at the ship's 3D model which is initialized only when
            // the arena is played for sure.
            if (!_isActivated) return;

            ShipDevice oldDevice = null;
            switch (deviceType)
            {
                case ShipDevice.OwnerHandleType.PrimaryWeapon: oldDevice = Weapon1; break;
                case ShipDevice.OwnerHandleType.SecondaryWeapon: oldDevice = Weapon2; break;
                case ShipDevice.OwnerHandleType.ExtraDevice: oldDevice = ExtraDevice; break;
                default: throw new ApplicationException("Unknown Weapon.OwnerHandleType " + deviceType);
            }
            if (oldDevice != null) AssaultWing.Instance.DataEngine.Devices.Remove(oldDevice);
            var newDevice = ShipDevice.CreateDevice(typeName, deviceType, this);
            switch (deviceType)
            {
                case ShipDevice.OwnerHandleType.PrimaryWeapon: Weapon1 = (Weapon)newDevice; break;
                case ShipDevice.OwnerHandleType.SecondaryWeapon: Weapon2 = (Weapon)newDevice; break;
                case ShipDevice.OwnerHandleType.ExtraDevice: ExtraDevice = newDevice; break;
                default: throw new ApplicationException("Unknown Weapon.OwnerHandleType " + deviceType);
            }
        }
    }
}
