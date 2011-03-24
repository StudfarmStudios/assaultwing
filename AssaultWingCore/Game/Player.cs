using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using AW2.UI;

namespace AW2.Game
{
    /// <summary>
    /// Player of the game. 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("ID:{ID} Name:{Name} ShipName:{ShipName}")]
    public class Player : Spectator
    {
        /// <summary>
        /// Time between death of player's ship and birth of a new ship,
        /// measured in seconds.
        /// </summary>
        private const float MOURNING_DELAY = 3;

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

        #region Player fields about general things

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

        private Vector2 _lastLookAtPos;
        private TimeSpan _lastRepairPendingNotify;

        #endregion Player fields about general things

        #region Player fields about statistics

        /// <summary>
        /// Number of opposing players' ships this player has killed.
        /// </summary>
        private int _kills;

        /// <summary>
        /// Number of times this player has died for some other reason
        /// than another player killing him.
        /// </summary>
        private int _suicides;

        #endregion Player fields about statistics

        #region Player properties

        public List<GobTrackerItem> GobTrackerItems { get; private set; }

        public int KillsWithoutDying { get; set; }

        /// <summary>
        /// The player's Color on radar.
        /// </summary>
        public Color PlayerColor { get; set; }

        /// <summary>
        /// Does the player need a viewport on the game window.
        /// </summary>
        public override bool NeedsViewport { get { return !IsRemote; } }

        /// <summary>
        /// Does the player state need to be updated to the clients.
        /// For use by game server only.
        /// </summary>
        public bool MustUpdateToClients { get; set; }

        /// <summary>
        /// The ship the player is controlling in the game arena.
        /// </summary>
        public Ship Ship { get; set; }

        public Vector2 LookAtPos
        {
            get
            {
                if (Ship != null) _lastLookAtPos = Ship.Pos + Ship.DrawPosOffset;
                return _lastLookAtPos;
            }
        }

        /// <summary>
        /// If positive, how many reincarnations the player has left.
        /// If negative, the player has infinite lives.
        /// If zero, the player cannot play.
        /// </summary>
        public int Lives { get; set; }

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

        public GameActionCollection BonusActions { get; private set; }

        /// <summary>
        /// Messages to display in the player's chat box, oldest first.
        /// </summary>
        public ChatContainer Messages { get; private set; }

        public PostprocessEffectNameContainer PostprocessEffectNames { get; private set; }

        public Func<bool> IsAllowedToCreateShip { get; set; }

        private bool IsTimeToCreateShip
        {
            get
            {
                if (!(Ship == null && Lives != 0 && _shipSpawnTime <= Game.DataEngine.ArenaTotalTime)) return false;
                return !(IsAllowedToCreateShip != null && !IsAllowedToCreateShip());
            }
        }

        #endregion Player properties

        #region Player properties about statistics

        /// <summary>
        /// Number of opposing players' ships this player has killed.
        /// </summary>
        public int Kills { get { return _kills; } set { _kills = value; } }

        /// <summary>
        /// Number of times this player has died for some other reason
        /// than another player killing him.
        /// </summary>
        public int Suicides { get { return _suicides; } set { _suicides = value; } }

        #endregion Player properties about statistics

        #region Events

        /// <summary>
        /// Called after the primary or secondary weapon of the player's ship is fired.
        /// </summary>
        public event Action WeaponFired;

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
            : this(game, name, shipTypeName, weapon2Name, extraDeviceName, controls, -1)
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
            CanonicalString extraDeviceName, int connectionId)
            : this(game, name, shipTypeName, weapon2Name, extraDeviceName, new PlayerControls
            {
                Thrust = new RemoteControl(),
                Left = new RemoteControl(),
                Right = new RemoteControl(),
                Down = new RemoteControl(),
                Fire1 = new RemoteControl(),
                Fire2 = new RemoteControl(),
                Extra = new RemoteControl()
            }, connectionId)
        {
        }

        private Player(AssaultWingCore game, string name, CanonicalString shipTypeName, CanonicalString weapon2Name,
            CanonicalString extraDeviceName, PlayerControls controls, int connectionId)
            : base(game, controls, connectionId)
        {
            Name = name;
            ShipName = shipTypeName;
            Weapon2Name = weapon2Name;
            ExtraDeviceName = extraDeviceName;
            Messages = new ChatContainer();
            PlayerColor = Color.Gray;
            BonusActions = new GameActionCollection(this);
            PostprocessEffectNames = new PostprocessEffectNameContainer(this);
            GobTrackerItems = new List<GobTrackerItem>();
        }

        #endregion Constructors

        #region Public methods

        public override void Update()
        {
            foreach (var action in BonusActions)
            {
                action.Update();
                if (action.EndTime <= Game.DataEngine.ArenaTotalTime)
                    BonusActions.RemoveLater(action);
            }
            BonusActions.CommitRemoves();

            if (Game.NetworkMode != NetworkMode.Client)
            {
                if (IsTimeToCreateShip) CreateShip();
                ApplyControlsToShip();
            }
            else // otherwise we are a game client
            {
                if (!IsRemote) ApplyControlsToShip();
            }
        }

        public override AW2.Graphics.AWViewport CreateViewport(Rectangle onScreen)
        {
            return new AW2.Graphics.PlayerViewport(this, onScreen, () => PostprocessEffectNames);
        }

        /// <summary>
        /// Initialises the player for a game session, that is, for the first arena.
        /// </summary>
        public override void InitializeForGameSession()
        {
            Kills = Suicides = 0;
        }

        /// <summary>
        /// Resets the player's internal state for a new arena.
        /// </summary>
        public override void ResetForArena()
        {
            base.ResetForArena();
            _shipSpawnTime = TimeSpan.Zero;
            _shakeUpdateTime = TimeSpan.Zero;
            _relativeShakeDamage = 0;
            KillsWithoutDying = 0;
            Lives = Game.DataEngine.GameplayMode.StartLives;
            BonusActions.Clear();
            Ship = null;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (Ship != null) Ship.Die();
        }

        public override string ToString()
        {
            return Name;
        }

        public void NotifyRepairPending()
        {
            if (Game.NetworkMode == NetworkMode.Client) return;
            if (Game.GameTime.TotalGameTime < _lastRepairPendingNotify + Dock.UNDAMAGED_TIME_REQUIRED) return;
            _lastRepairPendingNotify = Game.GameTime.TotalGameTime;
            Messages.Add(new PlayerMessage("Repair pending due to recent damage", PlayerMessage.DEFAULT_COLOR));
        }

        public void SeizeShip(Ship ship)
        {
            if (Ship == ship) return;
            ship.Death += ShipDeathHandler;
            Ship = ship;
        }

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((CanonicalString)ShipName);
                writer.Write((CanonicalString)Weapon2Name);
                writer.Write((CanonicalString)ExtraDeviceName);
                writer.Write((Color)PlayerColor);
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((short)Lives);
                writer.Write((short)_kills);
                writer.Write((short)_suicides);
                writer.Write((byte)PostprocessEffectNames.Count);
                foreach (var effectName in PostprocessEffectNames)
                    writer.Write((CanonicalString)effectName);
                BonusActions.Serialize(writer, mode);
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                var newShipName = reader.ReadCanonicalString();
                var newWeapon2Name = reader.ReadCanonicalString();
                var newExtraDeviceName = reader.ReadCanonicalString();
                PlayerColor = reader.ReadColor();
                if (Game.DataEngine.GetTypeTemplate(newShipName) is Ship) ShipName = newShipName;
                if (Game.DataEngine.GetTypeTemplate(newWeapon2Name) is Weapon) Weapon2Name = newWeapon2Name;
                if (Game.DataEngine.GetTypeTemplate(newExtraDeviceName) is ShipDevice) ExtraDeviceName = newExtraDeviceName;
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                Lives = reader.ReadInt16();
                _kills = reader.ReadInt16();
                _suicides = reader.ReadInt16();
                int effectNameCount = reader.ReadByte();
                PostprocessEffectNames.Clear();
                for (int i = 0; i < effectNameCount; ++i)
                    PostprocessEffectNames.Add(reader.ReadCanonicalString());
                BonusActions.Deserialize(reader, mode, framesAgo);
            }
        }

        #endregion Public methods

        #region Private methods

        private void ShipDeathHandler(Coroner coroner)
        {
            if (Game.NetworkMode != NetworkMode.Client)
            {
                Die_HandleCounters(coroner);
                Die_SendMessages(coroner);
                _shipSpawnTime = Game.DataEngine.ArenaTotalTime + TimeSpan.FromSeconds(MOURNING_DELAY);
                MustUpdateToClients = true;
            }
            BonusActions.Clear();
            Ship = null;
        }

        private void Die_HandleCounters(Coroner coroner)
        {
            switch (coroner.DeathType)
            {
                default: throw new ApplicationException("Unexpected DeathType " + coroner.DeathType);
                case Coroner.DeathTypeType.Suicide:
                    _suicides++;
                    break;
                case Coroner.DeathTypeType.Kill:
                    coroner.ScoringPlayer._kills++;
                    coroner.ScoringPlayer.KillsWithoutDying++;
                    if (Game.NetworkMode == NetworkMode.Server)
                        coroner.ScoringPlayer.MustUpdateToClients = true;
                    break;
            }
            Lives--;
            KillsWithoutDying = 0;
        }

        private void Die_SendMessages(Coroner coroner)
        {
            switch (coroner.DeathType)
            {
                default: throw new ApplicationException("Unexpected DeathType " + coroner.DeathType);
                case Coroner.DeathTypeType.Kill:
                    CreateKillMessage(coroner.ScoringPlayer, Ship.Pos);
                    coroner.ScoringPlayer.Messages.Add(new PlayerMessage(coroner.MessageToScoringPlayer, PlayerMessage.KILL_COLOR));
                    Messages.Add(new PlayerMessage(coroner.MessageToCorpse, PlayerMessage.DEATH_COLOR));
                    break;
                case Coroner.DeathTypeType.Suicide:
                    CreateSuicideMessage(this, Ship.Pos);
                    Messages.Add(new PlayerMessage(coroner.MessageToCorpse, PlayerMessage.SUICIDE_COLOR));
                    break;
            }
            var bystanderMessage = new PlayerMessage(coroner.MessageToBystander, PlayerMessage.DEFAULT_COLOR);
            foreach (var plr in coroner.GetBystanders(Game.DataEngine.Players)) plr.Messages.Add(bystanderMessage);
            if (coroner.SpecialMessage != null)
            {
                var specialMessage = new PlayerMessage(coroner.SpecialMessage, PlayerMessage.SPECIAL_KILL_COLOR);
                foreach (var plr in Game.DataEngine.Players) plr.Messages.Add(specialMessage);
            }
        }

        private void CreateSuicideMessage(Player perpetrator, Vector2 pos)
        {
            CreateDeathMessage(perpetrator, pos, "b_icon_take_life");
        }

        private void CreateKillMessage(Player perpetrator, Vector2 pos)
        {
            CreateDeathMessage(perpetrator, pos, "b_icon_add_kill");
        }

        private void CreateDeathMessage(Player perpetrator, Vector2 Pos, string iconName)
        {
            Gob.CreateGob<ArenaMessage>(Game, (CanonicalString)"deathmessage", gob =>
            {
                gob.ResetPos(Pos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                gob.Message = perpetrator.Name;
                gob.IconName = iconName;
                gob.DrawColor = perpetrator.PlayerColor;
                Game.DataEngine.Arena.Gobs.Add(gob);
            });
        }

        /// <summary>
        /// Applies the player's controls to his ship, if there is any.
        /// </summary>
        private void ApplyControlsToShip()
        {
            if (Ship == null || Ship.IsDisposed) return;
            if (Ship.LocationPredicter != null)
            {
                Ship.LocationPredicter.StoreControlStates(Controls.GetStates(), Game.GameTime.TotalGameTime);
            }
            if (Controls.Thrust.Force > 0)
                Ship.Thrust(Controls.Thrust.Force, Game.GameTime.ElapsedGameTime, Ship.Rotation);
            if (Controls.Left.Force > 0)
                Ship.TurnLeft(Controls.Left.Force, Game.GameTime.ElapsedGameTime);
            if (Controls.Right.Force > 0)
                Ship.TurnRight(Controls.Right.Force, Game.GameTime.ElapsedGameTime);
            if (Controls.Right.Force == 0 && Controls.Left.Force == 0)
                Ship.StopTurning();
            if (Controls.Fire1.Pulse || Controls.Fire1.Force > 0)
            {
                Ship.Weapon1.Fire(Controls.Fire1.State);
                if (WeaponFired != null) WeaponFired();
            }
            if (Controls.Fire2.Pulse || Controls.Fire2.Force > 0)
            {
                Ship.Weapon2.Fire(Controls.Fire2.State);
                if (WeaponFired != null) WeaponFired();
            }

            if (Controls.Extra.Pulse || Controls.Extra.Force > 0)
                Ship.ExtraDevice.Fire(Controls.Extra.State);
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
                newShip.Owner = this;
                newShip.SetDeviceType(ShipDevice.OwnerHandleType.PrimaryWeapon, Weapon1Name);
                newShip.SetDeviceType(ShipDevice.OwnerHandleType.SecondaryWeapon, Weapon2Name);
                newShip.SetDeviceType(ShipDevice.OwnerHandleType.ExtraDevice, ExtraDeviceName);
                newShip.Rotation = Gob.DEFAULT_ROTATION; // must initialize rotation for SnakeShip.InitializeTailState()
                Game.DataEngine.Arena.Gobs.Add(newShip);
                SpawnPlayer.PositionNewShip(newShip);
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
