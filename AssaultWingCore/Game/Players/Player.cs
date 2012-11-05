using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.UI;

namespace AW2.Game.Players
{
    /// <summary>
    /// Human player of the game. 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("ID:{ID} Name:{Name} ShipName:{ShipName}")]
    public class Player : Spectator
    {
        /// <summary>
        /// Time between death of player's ship and birth of a new ship,
        /// measured in seconds.
        /// </summary>
        private static readonly TimeSpan MOURNING_DELAY = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Function that maps relative shake damage to radians that the player's
        /// viewport will tilt to produce sufficient shake.
        /// </summary>
        private static readonly Curve g_shakeCurve;

        /// <summary>
        /// Function that maps a parameter to relative shake damage.
        /// </summary>
        /// Used in attenuating shake.
        private static readonly Curve g_shakeAttenuationCurve;

        /// <summary>
        /// Inverse of <c>shakeAttenuationCurve</c>.
        /// </summary>
        /// Used in attenuating shake.
        private static readonly Curve g_shakeAttenuationInverseCurve;

        #region Fields

        /// <summary>
        /// Time at which the player's ship is born, measured in game time.
        /// </summary>
        private TimeSpan _shipSpawnTime;

        /// <summary>
        /// Amount of accumulated damage that determines the amount of shake 
        /// the player is suffering right now. Measured relative to
        /// the maximum damage of the player's ship.
        /// </summary>
        /// Shaking affects the player's viewport and is caused by
        /// the player's ship receiving damage.
        private float _relativeShakeDamage;

        /// <summary>
        /// Current amount of shake. Access this field through property <c>Shake</c>.
        /// </summary>
        private float _shake;

        /// <summary>
        /// Time when field <c>shake</c> was calculated, in game time.
        /// </summary>
        private TimeSpan _shakeUpdateTime;

        private TimeSpan _lastRepairPendingNotify;

        #endregion Fields

        #region Player properties

        public List<GobTrackerItem> GobTrackerItems { get; private set; }

        public override bool NeedsViewport { get { return IsLocal; } }
        public override IEnumerable<Gob> Minions { get { if (Ship != null) yield return Ship; } }

        /// <summary>
        /// The ship the player is controlling in the game arena.
        /// </summary>
        public Ship Ship { get; set; }

        /// <summary>
        /// Amount of shake the player is suffering right now, in radians.
        /// Shaking affects the player's viewport and is caused by
        /// the player's ship receiving damage.
        /// </summary>
        public float Shake
        {
            get
            {
                if (Game.DataEngine.ArenaTotalTime > _shakeUpdateTime)
                {
                    // Attenuate shake damage for any skipped frames.
                    float skippedTime = (float)(Game.DataEngine.ArenaTotalTime - Game.GameTime.ElapsedGameTime - _shakeUpdateTime).TotalSeconds;
                    AttenuateShake(skippedTime);

                    // Calculate new shake.
                    _shake = g_shakeCurve.Evaluate(_relativeShakeDamage);
                    _shakeUpdateTime = Game.DataEngine.ArenaTotalTime;

                    // Attenuate shake damage for the current frame.
                    AttenuateShake((float)Game.GameTime.ElapsedGameTime.TotalSeconds);
                }
                return _shake;
            }
        }

        /// <summary>
        /// Increases the player's shake according to an amount of damage
        /// the player's ship has received. Negative amount will reduce
        /// shake.
        /// </summary>
        /// Shake won't get negative. There will be no shake if the player
        /// doesn't have a ship.
        /// <param name="damageAmount">The amount of damage.</param>
        public void IncreaseShake(float damageAmount)
        {
            if (Ship == null) return;
            _relativeShakeDamage = Math.Max(0, _relativeShakeDamage + damageAmount / Ship.MaxDamageLevel);
        }

        /// <summary>
        /// The name of the type of ship the player has chosen to fly.
        /// </summary>
        public CanonicalString ShipName { get; set; }

        /// <summary>
        /// The name of the primary weapon as the player has chosen it.
        /// </summary>
        public CanonicalString Weapon1Name
        {
            get
            {
                if (Ship != null) return Ship.Weapon1Name;
                var shipType = (Ship)Game.DataEngine.GetTypeTemplate(ShipName);
                return shipType.Weapon1Name;
            }
        }

        /// <summary>
        /// The name of the secondary weapon as the player has chosen it.
        /// </summary>
        public CanonicalString Weapon2Name { get; set; }

        /// <summary>
        /// The name of the extra device as the player has chosen it.
        /// </summary>
        public CanonicalString ExtraDeviceName { get; set; }

        /// <summary>
        /// The controls the player uses in menus and in game.
        /// </summary>
        public PlayerControls Controls { get; set; }

        /// <summary>
        /// Messages to display in the player's chat box, oldest first.
        /// </summary>
        public MessageContainer Messages { get; private set; }

        public List<CanonicalString> PostprocessEffectNames { get; private set; }

        public Func<bool> IsAllowedToCreateShip { get; set; }

        private bool IsTimeToCreateShip
        {
            get
            {
                if (!(Ship == null && ArenaStatistics.Lives != 0 && _shipSpawnTime <= Game.DataEngine.ArenaTotalTime)) return false;
                return IsAllowedToCreateShip == null || IsAllowedToCreateShip();
            }
        }

        #endregion Player properties

        #region Events

        /// <summary>
        /// Called after a ship device of the player's ship is fired.
        /// </summary>
        public event Action<ShipDevice.OwnerHandleType> WeaponFired;
        public void OnWeaponFired(ShipDevice.OwnerHandleType ownerHandleType)
        {
            if (ownerHandleType != ShipDevice.OwnerHandleType.ExtraDevice) Ship.LastWeaponFiredTime = Game.DataEngine.ArenaTotalTime;
            if (WeaponFired != null) WeaponFired(ownerHandleType);
        }

        #endregion Events

        #region Constructors

        static Player()
        {
            g_shakeCurve = new Curve();
            g_shakeCurve.PreLoop = CurveLoopType.Constant;
            g_shakeCurve.PostLoop = CurveLoopType.Constant;
            g_shakeCurve.Keys.Add(new CurveKey(0, 0));
            g_shakeCurve.Keys.Add(new CurveKey(0.15f, 0.0f * MathHelper.PiOver4));
            g_shakeCurve.Keys.Add(new CurveKey(0.3f, 0.4f * MathHelper.PiOver4));
            g_shakeCurve.Keys.Add(new CurveKey(0.6f, 0.6f * MathHelper.PiOver4));
            g_shakeCurve.Keys.Add(new CurveKey(1, MathHelper.PiOver4));
            g_shakeCurve.ComputeTangents(CurveTangent.Linear);
            g_shakeAttenuationCurve = new Curve();
            g_shakeAttenuationCurve.PreLoop = CurveLoopType.Constant;
            g_shakeAttenuationCurve.PostLoop = CurveLoopType.Linear;
            g_shakeAttenuationCurve.Keys.Add(new CurveKey(0, 0));
            g_shakeAttenuationCurve.Keys.Add(new CurveKey(0.05f, 0.01f));
            g_shakeAttenuationCurve.Keys.Add(new CurveKey(1.0f, 1));
            g_shakeAttenuationCurve.ComputeTangents(CurveTangent.Linear);
            g_shakeAttenuationInverseCurve = new Curve();
            g_shakeAttenuationInverseCurve.PreLoop = CurveLoopType.Constant;
            g_shakeAttenuationInverseCurve.PostLoop = CurveLoopType.Linear;
            foreach (var key in g_shakeAttenuationCurve.Keys)
                g_shakeAttenuationInverseCurve.Keys.Add(new CurveKey(key.Value, key.Position));
            g_shakeAttenuationInverseCurve.ComputeTangents(CurveTangent.Linear);
        }

        /// <summary>
        /// Creates a new player who plays at the local game instance.
        /// </summary>
        /// <param name="name">Name of the player.</param>
        /// <param name="shipTypeName">Name of the type of ship the player is flying.</param>
        /// <param name="weapon2Name">Name of the type of secondary weapon.</param>
        /// <param name="extraDeviceName">Name of the type of extra device.</param>
        /// <param name="controls">Player's in-game controls.</param>
        public Player(AssaultWingCore game, string name, CanonicalString shipTypeName, CanonicalString weapon2Name,
            CanonicalString extraDeviceName, PlayerControls controls)
            : this(game, name, shipTypeName, weapon2Name, extraDeviceName, controls, CONNECTION_ID_LOCAL, null)
        {
        }

        /// <summary>
        /// Creates a new player who plays at a remote game instance.
        /// </summary>
        /// <param name="name">Name of the player.</param>
        /// <param name="shipTypeName">Name of the type of ship the player is flying.</param>
        /// <param name="weapon2Name">Name of the type of secondary weapon.</param>
        /// <param name="extraDeviceName">Name of the type of extra device.</param>
        /// <param name="connectionId">Identifier of the connection to the remote game instance
        /// at which the player lives.</param>
        /// <see cref="AW2.Net.Connection.ID"/>
        public Player(AssaultWingCore game, string name, CanonicalString shipTypeName, CanonicalString weapon2Name,
            CanonicalString extraDeviceName, int connectionId, IPAddress ipAddress)
            : this(game, name, shipTypeName, weapon2Name, extraDeviceName, new PlayerControls
            {
                Thrust = new RemoteControl(),
                Left = new RemoteControl(),
                Right = new RemoteControl(),
                Fire1 = new RemoteControl(),
                Fire2 = new RemoteControl(),
                Extra = new RemoteControl()
            }, connectionId, ipAddress)
        {
        }

        private Player(AssaultWingCore game, string name, CanonicalString shipTypeName, CanonicalString weapon2Name,
            CanonicalString extraDeviceName, PlayerControls controls, int connectionId, IPAddress ipAddress)
            : base(game, connectionId, ipAddress)
        {
            Name = name;
            ShipName = shipTypeName;
            Weapon2Name = weapon2Name;
            ExtraDeviceName = extraDeviceName;
            Controls = controls;
            Messages = new MessageContainer(Game);
            PostprocessEffectNames = new List<CanonicalString>();
            GobTrackerItems = new List<GobTrackerItem>();
        }

        #endregion Constructors

        #region Public methods

        public override void Update()
        {
            base.Update();
            if (Game.NetworkMode != NetworkMode.Client)
            {
                if (IsTimeToCreateShip) CreateShip();
                ApplyControlsToShip();
            }
            else // otherwise we are a game client
            {
                if (IsLocal) ApplyControlsToShip();
            }
        }

        public override AW2.Graphics.AWViewport CreateViewport(Rectangle onScreen)
        {
            return new AW2.Graphics.PlayerViewport(this, onScreen, () => PostprocessEffectNames);
        }

        public override void ResetForArena()
        {
            base.ResetForArena();
            _shipSpawnTime = Game.DataEngine.ArenaTotalTime + MOURNING_DELAY;
            _shakeUpdateTime = TimeSpan.Zero;
            _relativeShakeDamage = 0;
            PostprocessEffectNames.Clear();
            Ship = null;
        }

        public void NotifyRepairPending()
        {
            if (Game.GameTime.TotalGameTime < _lastRepairPendingNotify + Dock.UNDAMAGED_TIME_REQUIRED) return;
            _lastRepairPendingNotify = Game.GameTime.TotalGameTime;
            Messages.Add(new PlayerMessage("To repair, avoid damage and don't shoot", PlayerMessage.DEFAULT_COLOR));
        }

        public void SeizeShip(Ship ship)
        {
            if (Ship == ship) return;
            Ship = ship;
            ship.Owner = this;
            ship.Death += MinionDeathHandler.OnMinionDeath;
            ship.Death += ShipDeathHandler;
            ship.SetDeviceType(ShipDevice.OwnerHandleType.PrimaryWeapon, Weapon1Name);
            ship.SetDeviceType(ShipDevice.OwnerHandleType.SecondaryWeapon, Weapon2Name);
            ship.SetDeviceType(ShipDevice.OwnerHandleType.ExtraDevice, ExtraDeviceName);
        }

        public override void ReconnectOnClient(Spectator oldSpectator)
        {
            var oldPlayer = oldSpectator as Player;
            if (oldPlayer == null || oldPlayer.Ship == null) return;
            oldPlayer.Ship.Death -= oldPlayer.ShipDeathHandler;
            oldPlayer.Ship.Death -= MinionDeathHandler.OnMinionDeath;
            SeizeShip(oldPlayer.Ship);
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
#if NETWORK_PROFILING
            using (new NetworkProfilingScope(this))
#endif
            {
                checked
                {
                    base.Serialize(writer, mode);
                    if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer) ||
                        mode.HasFlag(SerializationModeFlags.ConstantDataFromClient))
                    {
                        writer.Write((CanonicalString)ShipName);
                        writer.Write((CanonicalString)Weapon2Name);
                        writer.Write((CanonicalString)ExtraDeviceName);
                    }
                }
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if (mode.HasFlag(SerializationModeFlags.ConstantDataFromServer) ||
                mode.HasFlag(SerializationModeFlags.ConstantDataFromClient))
            {
                var newShipName = reader.ReadCanonicalString();
                var newWeapon2Name = reader.ReadCanonicalString();
                var newExtraDeviceName = reader.ReadCanonicalString();
                if (Game.DataEngine.GetTypeTemplate(newShipName) is Ship) ShipName = newShipName;
                if (Game.DataEngine.GetTypeTemplate(newWeapon2Name) is Weapon) Weapon2Name = newWeapon2Name;
                if (Game.DataEngine.GetTypeTemplate(newExtraDeviceName) is ShipDevice) ExtraDeviceName = newExtraDeviceName;
            }
        }

        #endregion Public methods

        #region Private methods

        private void ShipDeathHandler(Coroner coroner)
        {
            if (Game.NetworkMode != NetworkMode.Client)
                _shipSpawnTime = Game.DataEngine.ArenaTotalTime + MOURNING_DELAY;
            Ship = null;
        }

        /// <summary>
        /// Applies the player's controls to his ship, if there is any.
        /// </summary>
        private void ApplyControlsToShip()
        {
            if (!Game.IsShipControlsEnabled || Ship == null || Ship.IsDisposed) return;
            if (Ship.LocationPredicter != null)
            {
                Ship.LocationPredicter.StoreControlStates(Controls.GetStates(), Game.GameTime.TotalGameTime);
            }
            if (Controls.Thrust.Force > 0) Ship.Thrust(Controls.Thrust.Force);
            if (Controls.Left.Force > 0) Ship.TurnLeft(Controls.Left.Force);
            if (Controls.Right.Force > 0) Ship.TurnRight(Controls.Right.Force);
            if (!Ship.IsNewborn && Game.NetworkMode != NetworkMode.Client) // client shoots only when the server says so
            {
                TryFire(ShipDevice.OwnerHandleType.PrimaryWeapon);
                TryFire(ShipDevice.OwnerHandleType.SecondaryWeapon);
                TryFire(ShipDevice.OwnerHandleType.ExtraDevice);
            }
        }

        private void TryFire(ShipDevice.OwnerHandleType ownerHandleType)
        {
            var control = Controls[ownerHandleType];
            if (!control.HasSignal) return;
            var result = Ship.TryFire(ownerHandleType, control);
            if (result == ShipDevice.FiringResult.Success) OnWeaponFired(ownerHandleType);
        }

        /// <summary>
        /// Creates a ship for the player.
        /// </summary>
        private void CreateShip()
        {
            if (Ship != null) throw new InvalidOperationException("Player already has a ship");
            Gob.CreateGob<Ship>(Game, ShipName, newShip =>
            {
                SeizeShip(newShip);
                newShip.Rotation = Gob.DEFAULT_ROTATION; // must initialize rotation for SnakeShip.InitializeTailState()
                SpawnPlayer.PositionNewMinion(newShip, Game.DataEngine.Arena);
                Game.DataEngine.Arena.Gobs.Add(newShip);
            });
            Game.Stats.Send(new
            {
                Ship = ShipName.Value,
                Weapon2 = Weapon2Name.Value,
                Device = ExtraDeviceName.Value,
                Player = Game.Stats.GetStatsString(this),
                Pos = Ship.Pos,
            });
        }

        /// <summary>
        /// Attenuates the player's viewport shake for passed time.
        /// This method should be called regularly. It decreases <c>relativeShakeDamage</c>.
        /// <param name="seconds">Passed time in seconds.</param>
        /// </summary>
        private void AttenuateShake(float seconds)
        {
            // Attenuation is done along a steepening curve;
            // the higher the shake damage the faster the attenuation.
            // 'relativeShakeDamage' is thought of as the value of the curve
            // for some parameter x which represents time to wait for the shake to stop.
            // In effect, this ensures that it won't take too long for
            // even very big shakes to stop.
            float shakeTime = g_shakeAttenuationInverseCurve.Evaluate(_relativeShakeDamage);
            shakeTime = Math.Max(0, shakeTime - seconds);
            _relativeShakeDamage = g_shakeAttenuationCurve.Evaluate(shakeTime);
        }

        #endregion Private methods
    }
}
