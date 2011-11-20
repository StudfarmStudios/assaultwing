#if !DEBUG
#define OPTIMIZED_CODE // replace some function calls with fast elementary operations
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game.GobUtils;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.Sound;
using AW2.UI;
using AW2.Graphics;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A player's ship.
    /// </summary>
    public class Ship : Gob
    {
        private enum AerodynamicsType
        {
            ThrustTowardsHeading,
            ThrustToStraightenToHeading,
        };

        private const string SHIP_BIRTH_SOUND = "NewCraft";
        private static readonly ControlState[] g_defaultControlStates;

        #region Ship fields related to flying

        [TypeParameter]
        private Thruster _thruster;

        [TypeParameter]
        private AerodynamicsType _aerodynamics;

        /// <summary>
        /// Maximum turning speed of the ship, measured in radians per second.
        /// </summary>
        [TypeParameter]
        private float _turnSpeed;

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

        [TypeParameter]
        private CoughEngine _coughEngine;

        #region Ship fields related to other things

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
        /// Gobs that we have temporarily disabled while we move through them.
        /// </summary>
        private List<Gob> _temporarilyDisabledGobs; // TODO: Move to physics engine

        private bool _isBirthFlashing;

        #endregion Ship fields related to other things

        #region Ship fields for signalling visual things over the network

        private float _visualThrustForce; // TODO !!! Move to Thruster
        private bool _visualThrustForceSerializedThisFrame; // TODO !!! Move to Thruster

        #endregion Ship fields for signalling visual things over the network

        #region Ship properties

        public TimeSpan LastDamageTakenTime { get; set; }
        public TimeSpan LastWeaponFiredTime { get; set; }

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
        public Thruster Thruster { get { return _thruster; } }

        /// <summary>
        /// Called when the ship is thrusting. Parameter is proportional thrust, between -1 and 1.
        /// </summary>
        protected Action<float> Thrusting { get; set; }

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
        public new Player Owner { get { return (Player)base.Owner; } set { base.Owner = value; } }

        /// <summary>
        /// Names of all textures that this gob type will ever use.
        /// </summary>
        public override IEnumerable<CanonicalString> TextureNames
        {
            get { return base.TextureNames.Union(new CanonicalString[] { ShipInfo.IconEquipName }); }
        }

        public event Action<Gob> PhysicalCollidedInto;

        private SpriteFont PlayerNameFont { get { return Game.GraphicsEngine.GameContent.ConsoleFont; } }

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
            _thruster = new Thruster();
            _aerodynamics = AerodynamicsType.ThrustTowardsHeading;
            _turnSpeed = 3;
            _rollMax = (float)MathHelper.PiOver4;
            _rollSpeed = (float)(MathHelper.TwoPi / 2.0);
            _weapon1TypeName = (CanonicalString)"dummyweapon";
            _extraDeviceChargeMax = 5000;
            _extraDeviceChargeSpeed = 500;
            _weapon2ChargeMax = 5000;
            _weapon2ChargeSpeed = 500;
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
            _coughEngine = new CoughEngine();
        }

        public Ship(CanonicalString typeName)
            : base(typeName)
        {
            IsKeptInArenaBounds = true;
            _temporarilyDisabledGobs = new List<Gob>();
            LocationPredicter = new ShipLocationPredicter(this);
        }

        #endregion Ship constructors

        #region Methods related to gobs' functionality in the game world

        public override void Activate()
        {
            base.Activate();
            _thruster.Activate(this);
            _coughEngine.Activate(this);
            CreateGlow();
            Disable(); // re-enabled in Update()
            _isBirthFlashing = true;
            Game.SoundEngine.PlaySound(SHIP_BIRTH_SOUND, this);
        }

        public override void Update()
        {
            UpdateRoll();
            base.Update();
            foreach (var gob in _temporarilyDisabledGobs) gob.Enable();
            _temporarilyDisabledGobs.Clear();
            UpdateThrustInNetworkGame(); // TODO !!! Move to Thruster
            _thruster.Update();
            _coughEngine.Update();
            UpdateCharges();
            UpdateFlashing();
            StoreCurrentShipLocation();
        }

        public override void Dispose()
        {
            Game.PostFrameLogicEngine.DoOnce += () =>
            {
                Game.DataEngine.Devices.Remove(Weapon1);
                Game.DataEngine.Devices.Remove(Weapon2);
                Game.DataEngine.Devices.Remove(ExtraDevice);
            };
            _thruster.Dispose();
            base.Dispose();
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Methods related to serialisation

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                base.Serialize(writer, mode);

                // HACK to avoid null references:
                //   - ForwardShot using Ship.Model before LoadContent() is called
                //   - Thrust() using _thrusterSound before Activate() is called
                var shipMode = (mode & SerializationModeFlags.ConstantData) != 0
                    ? mode & ~SerializationModeFlags.VaryingData
                    : mode;

                if (shipMode.HasFlag(SerializationModeFlags.VaryingData))
                {
                    writer.Write((byte)MathHelper.Clamp(_visualThrustForce * 255, 0, 255));
                    _visualThrustForceSerializedThisFrame = true;
                    Action<ShipDevice> serializeDevice = device =>
                    {
                        var chargeData = device == null ? 0 : byte.MaxValue * device.Charge / device.ChargeMax;
                        writer.Write((byte)chargeData);
                    };
                    serializeDevice(Weapon1);
                    serializeDevice(Weapon2);
                    serializeDevice(ExtraDevice);
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            var oldRotation = Rotation;
            var oldDrawRotationOffset = DrawRotationOffset;
            base.Deserialize(reader, mode, framesAgo);
            if (Owner != null) Owner.SeizeShip(this);

            // HACK to avoid null references:
            //   - ForwardShot using Ship.Model before LoadContent() is called
            //   - Thrust() using _thrusterSound before Activate() is called
            var shipMode = (mode & SerializationModeFlags.ConstantData) != 0
                ? mode & ~SerializationModeFlags.VaryingData
                : mode;

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

            if (shipMode.HasFlag(SerializationModeFlags.VaryingData))
            {
                _visualThrustForce = reader.ReadByte() / 255f;
                Action<ShipDevice> deserializeDevice = device =>
                {
                    var data = reader.ReadByte();
                    if (device != null) device.Charge = data * device.ChargeMax / byte.MaxValue;
                };
                deserializeDevice(Weapon1);
                deserializeDevice(Weapon2);
                deserializeDevice(ExtraDevice);
            }
        }

        #endregion Methods related to serialisation

        #region Ship public methods

        /// <summary>
        /// Thrusts the ship.
        /// </summary>
        /// <param name="force">Thrust force factor relative to ship's maximum thrust.</param>
        public void Thrust(float force, TimeSpan duration)
        {
            System.Diagnostics.Debug.Assert(force >= 0 && force <= 1);
            if (Disabled) return;
            var proportionalForce = force;
            switch (_aerodynamics)
            {
                case AerodynamicsType.ThrustTowardsHeading:
                    _thruster.Thrust(proportionalForce);
                    break;
                case AerodynamicsType.ThrustToStraightenToHeading:
                    var thrustDirection = Math.Max(_thruster.MaxSpeed, Move.Length() + 1) * AWMathHelper.GetUnitVector2(Rotation) - Move;
                    _thruster.Thrust(proportionalForce, thrustDirection);
                    break;
                default: throw new ApplicationException("Unknown aerodynamics " + _aerodynamics);
            }
            if (Thrusting != null) Thrusting(proportionalForce);
            _visualThrustForce = proportionalForce;
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

        #endregion Ship public methods

        public override void Draw2D(Matrix gameToScreen, SpriteBatch spriteBatch, float scale, Player viewer)
        {
            // Draw player name
            if (Owner == null || !Owner.IsRemote) return;
            var screenPos = Vector2.Transform(Pos + DrawPosOffset, gameToScreen);
            var playerNameSize = PlayerNameFont.MeasureString(Owner.Name);
            var playerNamePos = new Vector2(screenPos.X - playerNameSize.X / 2, screenPos.Y + 35);
            var nameAlpha = (IsHiding ? Alpha : 1) * 0.8f;
            var nameColor = Color.Multiply(Owner.Color, nameAlpha);
            spriteBatch.DrawString(PlayerNameFont, Owner.Name, playerNamePos.Round(), nameColor);
        }

        public override Arena.CollisionSideEffectType Collide(CollisionArea myArea, CollisionArea theirArea, bool stuck, Arena.CollisionSideEffectType sideEffectTypes)
        {
            if ((sideEffectTypes & AW2.Game.Arena.CollisionSideEffectType.Reversible) != 0)
            {
                if (stuck)
                {
                    // Set the other gob as disabled while we move, then enable it after we finish moving.
                    // This works with the assumption that there are at least two moving iterations.
                    theirArea.Owner.Disable(); // re-enabled in Update()
                    _temporarilyDisabledGobs.Add(theirArea.Owner);
                    return Arena.CollisionSideEffectType.Reversible;
                }
            }
            return Arena.CollisionSideEffectType.None;
        }

        public override void PhysicalCollisionInto(Gob other, Vector2 moveDelta, float damageMultiplier)
        {
            base.PhysicalCollisionInto(other, moveDelta, damageMultiplier);
            if (PhysicalCollidedInto != null) PhysicalCollidedInto(other);
        }

        public override void InflictDamage(float damageAmount, DamageInfo cause)
        {
            if (damageAmount < 0) throw new ArgumentOutOfRangeException("damageAmount");
            if (damageAmount == 0) return;
            LastDamageTakenTime = Game.DataEngine.ArenaTotalTime;
            if (Owner != null) Owner.IncreaseShake(damageAmount);
            base.InflictDamage(damageAmount, cause);
        }

        public ShipLocationPredicter LocationPredicter { get; private set; }

        public override ChargeProvider GetChargeProvider(ShipDevice.OwnerHandleType deviceType)
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
            ShipDevice newDevice = null;
            switch (deviceType)
            {
                case ShipDevice.OwnerHandleType.PrimaryWeapon: newDevice = Weapon1 = Weapon.Create(typeName); break;
                case ShipDevice.OwnerHandleType.SecondaryWeapon: newDevice = Weapon2 = Weapon.Create(typeName); break;
                case ShipDevice.OwnerHandleType.ExtraDevice: newDevice = ExtraDevice = ShipDevice.Create(typeName); break;
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

        #region Private methods

        private void CreateGlow()
        {
            Gob.CreateGob<Peng>(Game, (CanonicalString)"playerglow", gob =>
            {
                gob.OwnerProxy = OwnerProxy;
                gob.Leader = this;
                Game.DataEngine.Arena.Gobs.Add(gob);
            });
        }

        private void InitializeDevice(ShipDevice device, ShipDevice.OwnerHandleType ownerHandle)
        {
            device.AttachTo(this, ownerHandle);
            Game.DataEngine.Devices.Add(device);
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
                moveLength <= _thruster.MaxSpeed ? Vector2.Dot(headingNormal, Move / _thruster.MaxSpeed) :
                Vector2.Dot(headingNormal, Move / moveLength);
            _rollAngle.Target = -_rollMax * force * headingFactor;
            _rollAngleGoalUpdated = true;
        }

        private void UpdateRoll()
        {
            _rollAngle.Step = Game.PhysicsEngine.ApplyChange(_rollSpeed, Game.GameTime.ElapsedGameTime);
            _rollAngle.Advance();
            if (!_rollAngleGoalUpdated)
                _rollAngle.Target = 0;
            _rollAngleGoalUpdated = false;
        }

        private void UpdateThrustInNetworkGame() // TODO !!! Move to Thruster
        {
            switch (Game.NetworkMode)
            {
                case NetworkMode.Client:
                    if (_visualThrustForce > 0)
                        Thrust(_visualThrustForce, Game.GameTime.ElapsedGameTime);
                    _visualThrustForce *= 0.977f; // fade down to half force in 30 frames (0.5 seconds)
                    if (_visualThrustForce < 0.5f) _visualThrustForce = 0;
                    break;
                case NetworkMode.Server:
                    if (_visualThrustForceSerializedThisFrame)
                    {
                        _visualThrustForceSerializedThisFrame = false;
                        _visualThrustForce = 0;
                    }
                    break;
            }
        }

        private void UpdateCharges()
        {
            float elapsedSeconds = (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
            if (ExtraDevice != null) ExtraDevice.Charge += _extraDeviceChargeSpeed * elapsedSeconds;
            if (Weapon2 != null) Weapon2.Charge += _weapon2ChargeSpeed * elapsedSeconds;
        }

        private void UpdateFlashing()
        {
            if (!_isBirthFlashing) return;
            Alpha = _birthAlpha.Evaluate(AgeInGameSeconds);
            if (AgeInGameSeconds < _birthAlpha.Keys[_birthAlpha.Keys.Count - 1].Position) return;
            Enable();
            _isBirthFlashing = false;
        }

        private void StoreCurrentShipLocation()
        {
            if (LocationPredicter == null) return;
            LocationPredicter.StoreOldShipLocation(new ShipLocationEntry
            {
                GameTime = Game.DataEngine.ArenaTotalTime,
                Move = Move,
                Pos = Pos,
                Rotation = Rotation,
                ControlStates = GetControlStates(),
            });
        }

        #endregion Private methods
    }
}
