#if !DEBUG
#define OPTIMIZED_CODE // replace some function calls with fast elementary operations
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Sound;
using AW2.UI;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A player's ship.
    /// </summary>
    public class Ship : Gob
    {
        private const string SHIP_BIRTH_SOUND = "NewCraft";
        private const string SHIP_THRUST_SOUND = "Engine";
        private static readonly ControlState[] g_defaultControlStates;

        #region Ship fields related to flying

        /// <summary>
        /// Maximum force of thrust of the ship, measured in Newtons.
        /// </summary>
        [TypeParameter]
        private float _thrustForce;

        /// <summary>
        /// Maximum turning speed of the ship, measured in radians per second.
        /// </summary>
        [TypeParameter]
        private float _turnSpeed;

        /// <summary>
        /// Ship's maximum speed reachable by thrust, measured in meters per second.
        /// </summary>
        [TypeParameter]
        private float _maxSpeed;

        private SoundInstance _thrusterSound;

        #endregion Ship fields related to flying

        #region Ship fields related to weapons

        /// <summary>
        /// Name of the type of primary weapon the ship type uses.
        /// </summary>
        [TypeParameter]
        private CanonicalString _weapon1TypeName;

        /// <summary>
        /// Maximum amount of charge for extra devices.
        /// </summary>
        [TypeParameter]
        private float _extraDeviceChargeMax;

        /// <summary>
        /// Maximum amount of charge for secondary weapons.
        /// </summary>
        [TypeParameter]
        private float _weapon2ChargeMax;

        /// <summary>
        /// Speed of charging for extra device charge,
        /// measured in charge units per second.
        /// </summary>
        [TypeParameter]
        private float _extraDeviceChargeSpeed;

        /// <summary>
        /// Speed of charging for secondary weapon charge,
        /// measured in charge units per second.
        /// </summary>
        [TypeParameter]
        private float _weapon2ChargeSpeed;

        private int[] _barrelBoneIndices;

        #endregion Ship fields related to weapons

        #region Ship fields related to rolling

        private InterpolatingValue _rollAngle;
        private bool _rollAngleGoalUpdated;

        /// <summary>
        /// Maximum angle of rotation of the ship around its tail-to-head axis
        /// Minimum roll angle will be the additive inverse.
        /// </summary>
        [TypeParameter]
        private float _rollMax;

        /// <summary>
        /// Roll angle change speed in radians per second.
        /// </summary>
        [TypeParameter]
        private float _rollSpeed;

        #endregion Ship fields related to rolling

        #region Ship fields related to coughing

        [TypeParameter, ShallowCopy]
        private CanonicalString[] _coughEngineNames;

        private List<Peng> _coughEngines;

        #endregion Ship fields related to coughing

        #region Ship fields related to other things

        /// <summary>
        /// Armour of the ship as a function that maps
        /// the amount of damage delivered to the ship
        /// to the amount of damage the ship actually receives.
        /// </summary>
        [TypeParameter, ShallowCopy]
        private Curve _armour;

        /// <summary>
        /// Alpha of the ship as a function that maps the age of the
        /// ship (in seconds) to the alpha value to draw the ship with.
        /// </summary>
        /// Use this to implement alpha flashing on ship birth.
        [TypeParameter, ShallowCopy]
        private Curve _birthAlpha;

        [TypeParameter]
        private ShipInfo _shipInfo;

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

        public override float DrawRotation
        {
            get
            {
                if (LocationPredicter == null) return Rotation;
                return LocationPredicter.GetShipLocation(Game.DataEngine.ArenaTotalTime).Rotation;
            }
        }

        public override void ResetPos(Vector2 pos, Vector2 move, float rotation)
        {
            base.ResetPos(pos, move, rotation);
            if (LocationPredicter != null) LocationPredicter.ForgetOldShipLocations();
        }

        public override Matrix WorldMatrix
        {
            get
            {
#if OPTIMIZED_CODE
                var drawPos = Pos + DrawPosOffset;
                float scale = Scale;
                float rotation = DrawRotation + DrawRotationOffset;
                float scaledCosRoll = scale * (float)Math.Cos(_rollAngle.Current);
                float scaledSinRoll = scale * (float)Math.Sin(_rollAngle.Current);
                float cosRota = (float)Math.Cos(rotation);
                float sinRota = (float)Math.Sin(rotation);
                return new Matrix(
                    scale * cosRota, scale * sinRota, 0, 0,
                    -scaledCosRoll * sinRota, scaledCosRoll * cosRota, scaledSinRoll, 0,
                    scaledSinRoll * sinRota, -scaledSinRoll * cosRota, scaledCosRoll, 0,
                    drawPos.X, drawPos.Y, 0, 1);
#else
                return Matrix.CreateScale(Scale)
                     * Matrix.CreateRotationX(_rollAngle.Current)
                     * Matrix.CreateRotationZ(DrawRotation + DrawRotationOffset)
                     * Matrix.CreateTranslation(new Vector3(Pos + DrawPosOffset, 0));
#endif
            }
        }

        public float TurnSpeed { get { return _turnSpeed; } }

        public float ThrustForce { get { return _thrustForce; } }

        /// <summary>
        /// Name of the type of main weapon the ship is using. Same as
        /// <c>Weapon1.TypeName</c> but works even when <see cref="Weapon1"/> is null.
        /// </summary>
        public CanonicalString Weapon1Name { get { return _weapon1TypeName; } }

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

        public ShipInfo ShipInfo { get { return _shipInfo; } set { _shipInfo = value; } }

        public int[] BarrelBoneIndices
        {
            get
            {
                if (_barrelBoneIndices == null)
                {
                    var boneIs = GetNamedPositions("Gun");
                    if (boneIs.Length != 4) throw new ApplicationException(string.Format("Unexpected number of gun barrels ({0}) in ship 3D model ({1})", boneIs.Length, ModelName));
                    _barrelBoneIndices = boneIs.OrderBy(index => index.Key).Select(index => index.Value).ToArray();
                }
                return _barrelBoneIndices;
            }
        }

        /// <summary>
        /// Names of all textures that this gob type will ever use.
        /// </summary>
        public override IEnumerable<CanonicalString> TextureNames
        {
            get { return base.TextureNames.Union(new CanonicalString[] { ShipInfo.IconEquipName }); }
        }

        #endregion Ship properties

        #region Ship constructors

        static Ship()
        {
            g_defaultControlStates = new ControlState[PlayerControls.CONTROL_COUNT];
            for (int i = 0; i < g_defaultControlStates.Length; i++)
                g_defaultControlStates[i] = new ControlState();
        }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public Ship()
        {
            _thrustForce = 100;
            _turnSpeed = 3;
            _maxSpeed = 200;
            _rollMax = (float)MathHelper.PiOver4;
            _rollSpeed = (float)(MathHelper.TwoPi / 2.0);
            _weapon1TypeName = (CanonicalString)"dummyweapon";
            _extraDeviceChargeMax = 5000;
            _extraDeviceChargeSpeed = 500;
            _weapon2ChargeMax = 5000;
            _weapon2ChargeSpeed = 500;
            _armour = new Curve();
            _armour.PreLoop = CurveLoopType.Linear;
            _armour.PostLoop = CurveLoopType.Linear;
            _armour.Keys.Add(new CurveKey(-500, -500, 1, 500 * 1, CurveContinuity.Smooth));
            _armour.Keys.Add(new CurveKey(0, 0, 500 * 1, 10 * 0.3f, CurveContinuity.Smooth));
            _armour.Keys.Add(new CurveKey(10, 7, 10 * 1, 40 * 1, CurveContinuity.Smooth));
            _armour.Keys.Add(new CurveKey(50, 50, 40 * 1, 450 * 1, CurveContinuity.Smooth));
            _armour.Keys.Add(new CurveKey(500, 500, 450 * 1, 1, CurveContinuity.Smooth));
            _birthAlpha = new Curve();
            _birthAlpha.PreLoop = CurveLoopType.Constant;
            _birthAlpha.PostLoop = CurveLoopType.Constant;
            for (float age = 0; age + 0.2f < 2; age += 0.4f)
            {
                _birthAlpha.Keys.Add(new CurveKey(age, 0.2f));
                _birthAlpha.Keys.Add(new CurveKey(age + 0.2f, 0.8f));
            }
            _birthAlpha.Keys.Add(new CurveKey(2, 1));
            _birthAlpha.ComputeTangents(CurveTangent.Flat);
            _coughEngineNames = new CanonicalString[] { (CanonicalString)"dummypeng" };
            _temporarilyDisabledGobs = new List<Gob>();
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

        #endregion Protected methods

        #region Private methods

        private void CreateCoughEngines()
        {
            _coughEngines = new List<Peng>();
            foreach (var name in _coughEngineNames)
            {
                Gob.CreateGob<Peng>(Game, name, gob =>
                {
                    gob.Paused = true;
                    gob.Leader = this;
                    Arena.Gobs.Add(gob);
                    _coughEngines.Add(gob);
                });
            }
        }

        private void CreateGlow()
        {
            Gob.CreateGob<Peng>(Game, (CanonicalString)"playerglow", gob =>
            {
                gob.Owner = Owner;
                gob.Leader = this;
                Game.DataEngine.Arena.Gobs.Add(gob);
            });
        }

        #endregion Private methods

        #region Methods related to gobs' functionality in the game world

        public override void Activate()
        {
            base.Activate();
            _thrusterSound = Game.SoundEngine.CreateSound(SHIP_THRUST_SOUND);
            SwitchEngineFlashAndBang(false);
            _exhaustAmountUpdated = false;
            CreateCoughEngines();
            CreateGlow();
            Disable(); // re-enabled in Update()
            _isBirthFlashing = true;
            Game.SoundEngine.PlaySound(SHIP_BIRTH_SOUND);
        }

        public override void Update()
        {
            UpdateRoll();
            base.Update();

            // Re-enable temporarily disabled gobs.
            foreach (Gob gob in _temporarilyDisabledGobs) gob.Enable();
            _temporarilyDisabledGobs.Clear();

            UpdateExhaustEngines();
            UpdateCoughEngines();
            UpdateCharges();
            UpdateFlashing();
            StoreCurrentShipLocation();
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
            Game.DataEngine.Devices.Remove(Weapon1);
            Game.DataEngine.Devices.Remove(Weapon2);
            Game.DataEngine.Devices.Remove(ExtraDevice);
            _thrusterSound.Dispose();
            base.Dispose();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        protected override void SetRuntimeState(Gob runtimeState)
        {
            base.SetRuntimeState(runtimeState);
            _exhaustAmountUpdated = false;
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                if (Weapon2 != null) writer.Write(Weapon2.TypeName);
                else writer.Write(CanonicalString.Null);
                if (ExtraDevice != null) writer.Write(ExtraDevice.TypeName);
                else writer.Write(CanonicalString.Null);
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((Half)_visualThrustForce);
                _visualThrustForce = 0;
            }
            Weapon1.Serialize(writer, mode);
            Weapon2.Serialize(writer, mode);
            ExtraDevice.Serialize(writer, mode);
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            var oldRotation = Rotation;
            var oldDrawRotationOffset = DrawRotationOffset;
            base.Deserialize(reader, mode, framesAgo);
            if (Owner != null && Owner.Ship != this) Owner.Ship = this;

            // HACK: superclass Gob deserializes old Pos and Move and calculates them forward;
            // class Ship must calculate Rotation from old value to current.
            if (LocationPredicter != null)
            {
                LocationPredicter.UpdateOldShipLocation(new ShipLocationEntry
                {
                    GameTime = Game.DataEngine.ArenaTotalTime - Game.TargetElapsedTime.Multiply(framesAgo),
                    Pos = Pos,
                    Move = Move,
                    Rotation = Rotation,
                    ControlStates = null,
                });
                DrawRotationOffset = AWMathHelper.GetAbsoluteMinimalEqualAngle(oldDrawRotationOffset + oldRotation - Rotation);
                if (float.IsNaN(DrawRotationOffset) || Math.Abs(DrawRotationOffset) > Gob.ROTATION_SMOOTHING_CUTOFF)
                    DrawRotationOffset = 0;
            }

            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                SetDeviceType(ShipDevice.OwnerHandleType.PrimaryWeapon, Weapon1Name);
                var weapon2TypeName = reader.ReadCanonicalString();
                if (!weapon2TypeName.IsNull) SetDeviceType(ShipDevice.OwnerHandleType.SecondaryWeapon, weapon2TypeName);
                var extraTypeName = reader.ReadCanonicalString();
                if (!extraTypeName.IsNull) SetDeviceType(ShipDevice.OwnerHandleType.ExtraDevice, extraTypeName);
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                float thrustForce = reader.ReadHalf();
                if (thrustForce > 0)
                    Thrust(thrustForce, Game.GameTime.ElapsedGameTime, Rotation);
            }
            var deviceMode = (mode & SerializationModeFlags.ConstantData) != 0
                ? mode & ~SerializationModeFlags.VaryingData // HACK to avoid ForwardShot using Ship.Model before it is initialized
                : mode;
            Weapon1.Deserialize(reader, deviceMode, framesAgo);
            Weapon2.Deserialize(reader, deviceMode, framesAgo);
            ExtraDevice.Deserialize(reader, deviceMode, framesAgo);
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
            Vector2 forceVector = AWMathHelper.GetUnitVector2(direction) * force * _thrustForce;
            Game.PhysicsEngine.ApplyLimitedForce(this, forceVector, _maxSpeed, duration);
            _visualThrustForce = force;
            Thrusting(force);
            SwitchEngineFlashAndBang(true);
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
            float deltaRotation = Game.PhysicsEngine.ApplyChange(force * _turnSpeed, duration);
            Rotation += deltaRotation;

            Vector2 headingNormal = Vector2.Transform(Vector2.UnitX, Matrix.CreateRotationZ(Rotation));
            float moveLength = Move.Length();
            float headingFactor = // fancy roll
                moveLength == 0 ? 0 :
                moveLength <= _maxSpeed ? Vector2.Dot(headingNormal, Move / _maxSpeed) :
                Vector2.Dot(headingNormal, Move / moveLength);
            //float headingFactor = 1.0f; // naive roll
            _rollAngle.Target = -_rollMax * force * headingFactor;
            _rollAngleGoalUpdated = true;
        }

        #endregion Ship public methods

        private SpriteFont playerNameFont;

        public override void Draw2D(Matrix gameToScreen, SpriteBatch spriteBatch, float scale)
        {
            var screenPos = Vector2.Transform(Pos + DrawPosOffset, gameToScreen);

            // Draw player name
            if (Owner != null && Owner.IsRemote)
            {
                var playerNameSize = playerNameFont.MeasureString(Owner.Name);
                var playerNamePos = new Vector2(screenPos.X - playerNameSize.X / 2, screenPos.Y + 35);
                spriteBatch.DrawString(playerNameFont, Owner.Name, playerNamePos, Color.Multiply(Owner.PlayerColor, 0.8f));
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();

            var content = Game.Content;
            playerNameFont = content.Load<SpriteFont>("ConsoleFont");

        }

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

        /// <summary>
        /// Time in milliseconds when the ship took the last damage
        /// </summary>
        public TimeSpan LastDamageTaken { get; set; }

        public override void InflictDamage(float damageAmount, DeathCause cause)
        {
            // If player takes damage (something substracted from health) mark the GameTime so
            // that a dock can check whether the player can actually dock.
            if (damageAmount > 0)
            {
                LastDamageTaken = Game.DataEngine.ArenaTotalTime;
            }

            float realDamage = _armour.Evaluate(damageAmount);
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
                    return new ChargeProvider(() => _weapon2ChargeMax, () => _weapon2ChargeSpeed);
                case ShipDevice.OwnerHandleType.ExtraDevice:
                    return new ChargeProvider(() => _extraDeviceChargeMax, () => _extraDeviceChargeSpeed);
                default: throw new ApplicationException("Unknown ship device type " + deviceType);
            }
        }

        public void SetDeviceType(ShipDevice.OwnerHandleType deviceType, CanonicalString typeName)
        {
            switch (deviceType)
            {
                case ShipDevice.OwnerHandleType.PrimaryWeapon:
                    if (typeName != _weapon1TypeName)
                        throw new InvalidOperationException("Cannot set Ship primary weapon " + typeName +
                            ", fixed primary weapon is " + _weapon1TypeName);
                    break;
                case ShipDevice.OwnerHandleType.SecondaryWeapon: Weapon2Name = typeName; break;
                case ShipDevice.OwnerHandleType.ExtraDevice: ExtraDeviceName = typeName; break;
                default: throw new ApplicationException("Unknown Weapon.OwnerHandleType " + deviceType);
            }
            ShipDevice oldDevice = null;
            switch (deviceType)
            {
                case ShipDevice.OwnerHandleType.PrimaryWeapon: oldDevice = Weapon1; break;
                case ShipDevice.OwnerHandleType.SecondaryWeapon: oldDevice = Weapon2; break;
                case ShipDevice.OwnerHandleType.ExtraDevice: oldDevice = ExtraDevice; break;
                default: throw new ApplicationException("Unknown Weapon.OwnerHandleType " + deviceType);
            }
            if (oldDevice != null) Game.DataEngine.Devices.Remove(oldDevice);
            var newDevice = (ShipDevice)Clonable.Instantiate(typeName);
            switch (deviceType)
            {
                case ShipDevice.OwnerHandleType.PrimaryWeapon: Weapon1 = (Weapon)newDevice; break;
                case ShipDevice.OwnerHandleType.SecondaryWeapon: Weapon2 = (Weapon)newDevice; break;
                case ShipDevice.OwnerHandleType.ExtraDevice: ExtraDevice = newDevice; break;
                default: throw new ApplicationException("Unknown Weapon.OwnerHandleType " + deviceType);
            }
            InitializeDevice(newDevice, deviceType);
        }

        public ControlState[] GetControlStates()
        {
            return Owner != null
                ? Owner.Controls.GetStates()
                : g_defaultControlStates;
        }

        protected override void SwitchEngineFlashAndBang(bool active)
        {
            base.SwitchEngineFlashAndBang(active);
            if (active)
                _thrusterSound.EnsureIsPlaying();
            else
                _thrusterSound.Stop();
        }

        private void InitializeDevice(ShipDevice device, ShipDevice.OwnerHandleType ownerHandle)
        {
            device.AttachTo(this, ownerHandle);
            device.Owner.Game.DataEngine.Devices.Add(device);
        }

        private void UpdateRoll()
        {
            _rollAngle.Step = Game.PhysicsEngine.ApplyChange(_rollSpeed, Game.GameTime.ElapsedGameTime);
            _rollAngle.Advance();
            if (!_rollAngleGoalUpdated)
                _rollAngle.Target = 0;
            _rollAngleGoalUpdated = false;
        }

        private void UpdateExhaustEngines()
        {
            if (!_exhaustAmountUpdated)
                SwitchEngineFlashAndBang(false);
            _exhaustAmountUpdated = false;
        }

        private void UpdateCoughEngines()
        {
            float coughArgument = (DamageLevel / MaxDamageLevel - 0.8f) / 0.2f;
            coughArgument = MathHelper.Clamp(coughArgument, 0, 1);
            foreach (var coughEngine in _coughEngines)
            {
                coughEngine.Input = coughArgument;
                coughEngine.Paused = coughArgument == 0;
            }
        }

        private void UpdateCharges()
        {
            float elapsedSeconds = (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
            ExtraDevice.Charge += _extraDeviceChargeSpeed * elapsedSeconds;
            Weapon2.Charge += _weapon2ChargeSpeed * elapsedSeconds;
        }

        private void UpdateFlashing()
        {
            if (_isBirthFlashing)
            {
                Alpha = _birthAlpha.Evaluate(AgeInGameSeconds);
                if (AgeInGameSeconds >= _birthAlpha.Keys[_birthAlpha.Keys.Count - 1].Position)
                {
                    Enable();
                    _isBirthFlashing = false;
                }
            }
        }

        private void StoreCurrentShipLocation()
        {
            if (LocationPredicter != null) LocationPredicter.StoreOldShipLocation(new ShipLocationEntry
            {
                GameTime = Game.DataEngine.ArenaTotalTime,
                Move = Move,
                Pos = Pos,
                Rotation = Rotation,
                ControlStates = GetControlStates(),
            });
        }
    }
}
