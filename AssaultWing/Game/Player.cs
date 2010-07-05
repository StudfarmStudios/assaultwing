using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Graphics.OverlayComponents;
using AW2.Helpers;
using AW2.Net;
using AW2.Net.Messages;
using AW2.UI;

namespace AW2.Game
{
    /// <summary>
    /// Player of the game. 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("ID:{ID} Name:{Name} ShipName:{ShipName}")]
    public class Player : Spectator
    {
        public class Message
        {
            public TimeSpan GameTime { get; private set; }
            public string Text { get; private set; }
            public Color TextColor;
            public Message(string text)
            {
                GameTime = AssaultWing.Instance.DataEngine.ArenaTotalTime;
                Text = text;
            }
        }

        #region Player constants

        private const int MESSAGE_KEEP_COUNT = 100;

        /// <summary>
        /// Time between death of player's ship and birth of a new ship,
        /// measured in seconds.
        /// </summary>
        private const float MOURNING_DELAY = 3;

        #endregion Player constants

        #region Player fields about general things

        /// <summary>
        /// How many reincarnations the player has left.
        /// </summary>
        protected int _lives;

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
        /// Function that maps relative shake damage to radians that the player's
        /// viewport will tilt to produce sufficient shake.
        /// </summary>
        private Curve _shakeCurve;

        /// <summary>
        /// Function that maps a parameter to relative shake damage.
        /// </summary>
        /// Used in attenuating shake.
        private Curve _shakeAttenuationCurve;

        /// <summary>
        /// Inverse of <c>shakeAttenuationCurve</c>.
        /// </summary>
        /// Used in attenuating shake.
        private Curve _shakeAttenuationInverseCurve;

        /// <summary>
        /// Current amount of shake. Access this field through property <c>Shake</c>.
        /// </summary>
        private float _shake;

        /// <summary>
        /// Time when field <c>shake</c> was calculated, in game time.
        /// </summary>
        private TimeSpan _shakeUpdateTime;

        private Ship _ship;
        private Vector2 _lastLookAtPos;
        private List<GobTrackerItem> _gobTrackerItems = new List<GobTrackerItem>();

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

        public List<GobTrackerItem> GobTrackerItems { get { return _gobTrackerItems; } set { _gobTrackerItems = value; } }

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
        public Ship Ship { get { return _ship; } set { _ship = value; } }

        public Vector2 LookAtPos
        {
            get
            {
                if (Ship != null) _lastLookAtPos = Ship.Pos;
                return _lastLookAtPos;
            }
        }

        /// <summary>
        /// If positive, how many reincarnations the player has left.
        /// If negative, the player has infinite lives.
        /// If zero, the player cannot play.
        /// </summary>
        public int Lives { get { return _lives; } set { _lives = value; } }

        /// <summary>
        /// Amount of shake the player is suffering right now, in radians.
        /// Shaking affects the player's viewport and is caused by
        /// the player's ship receiving damage.
        /// </summary>
        public float Shake
        {
            get
            {
                if (AssaultWing.Instance.DataEngine.ArenaTotalTime > _shakeUpdateTime)
                {
                    // Attenuate shake damage for any skipped frames.
                    float skippedTime = (float)(AssaultWing.Instance.DataEngine.ArenaTotalTime - AssaultWing.Instance.GameTime.ElapsedGameTime - _shakeUpdateTime).TotalSeconds;
                    AttenuateShake(skippedTime);

                    // Calculate new shake.
                    _shake = _shakeCurve.Evaluate(_relativeShakeDamage);
                    _shakeUpdateTime = AssaultWing.Instance.DataEngine.ArenaTotalTime;

                    // Attenuate shake damage for the current frame.
                    AttenuateShake((float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds);
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
                if (_ship != null) return _ship.Weapon1Name;
                var shipType = (Ship)AssaultWing.Instance.DataEngine.GetTypeTemplate(ShipName);
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
        public List<Message> Messages { get; private set; }

        public PostprocessEffectNameContainer PostprocessEffectNames { get; private set; }

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

        #region Constructors

        /// <summary>
        /// Creates a new player who plays at the local game instance.
        /// </summary>
        /// <param name="name">Name of the player.</param>
        /// <param name="shipTypeName">Name of the type of ship the player is flying.</param>
        /// <param name="weapon2Name">Name of the type of secondary weapon.</param>
        /// <param name="extraDeviceName">Name of the type of extra device.</param>
        /// <param name="controls">Player's in-game controls.</param>
        public Player(string name, CanonicalString shipTypeName, CanonicalString weapon2Name,
            CanonicalString extraDeviceName, PlayerControls controls)
            : this(name, shipTypeName, weapon2Name, extraDeviceName, controls, -1)
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
        public Player(string name, CanonicalString shipTypeName, CanonicalString weapon2Name,
            CanonicalString extraDeviceName, int connectionId)
            : this(name, shipTypeName, weapon2Name, extraDeviceName, new PlayerControls
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

        private Player(string name, CanonicalString shipTypeName, CanonicalString weapon2Name,
            CanonicalString extraDeviceName, PlayerControls controls, int connectionId)
            : base(controls, connectionId)
        {
            KillsWithoutDying = 0;
            Name = name;
            ShipName = shipTypeName;
            Weapon2Name = weapon2Name;
            ExtraDeviceName = extraDeviceName;
            Messages = new List<Message>();
            _lives = 3;
            _shipSpawnTime = new TimeSpan(1);
            _relativeShakeDamage = 0;
            PlayerColor = Color.Gray;
            _shakeCurve = new Curve();
            _shakeCurve.PreLoop = CurveLoopType.Constant;
            _shakeCurve.PostLoop = CurveLoopType.Constant;
            _shakeCurve.Keys.Add(new CurveKey(0, 0));
            _shakeCurve.Keys.Add(new CurveKey(0.15f, 0.0f * MathHelper.PiOver4));
            _shakeCurve.Keys.Add(new CurveKey(0.3f, 0.4f * MathHelper.PiOver4));
            _shakeCurve.Keys.Add(new CurveKey(0.6f, 0.6f * MathHelper.PiOver4));
            _shakeCurve.Keys.Add(new CurveKey(1, MathHelper.PiOver4));
            _shakeCurve.ComputeTangents(CurveTangent.Linear);
            _shakeAttenuationCurve = new Curve();
            _shakeAttenuationCurve.PreLoop = CurveLoopType.Constant;
            _shakeAttenuationCurve.PostLoop = CurveLoopType.Linear;
            _shakeAttenuationCurve.Keys.Add(new CurveKey(0, 0));
            _shakeAttenuationCurve.Keys.Add(new CurveKey(0.05f, 0.01f));
            _shakeAttenuationCurve.Keys.Add(new CurveKey(1.0f, 1));
            _shakeAttenuationCurve.ComputeTangents(CurveTangent.Linear);
            _shakeAttenuationInverseCurve = new Curve();
            _shakeAttenuationInverseCurve.PreLoop = CurveLoopType.Constant;
            _shakeAttenuationInverseCurve.PostLoop = CurveLoopType.Linear;
            foreach (CurveKey key in _shakeAttenuationCurve.Keys)
                _shakeAttenuationInverseCurve.Keys.Add(new CurveKey(key.Value, key.Position));
            _shakeAttenuationInverseCurve.ComputeTangents(CurveTangent.Linear);
            BonusActions = new GameActionCollection(this);
            PostprocessEffectNames = new PostprocessEffectNameContainer(this);
        }

        #endregion Constructors

        #region General public methods

        public void RemoveGobTrackerItem(GobTrackerItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Trying to remove NULL GobTrackerItem from the GobTrackerList");
            }

            if (_gobTrackerItems.Contains(item))
                _gobTrackerItems.Remove(item);
        }

        public void AddGobTrackerItem(GobTrackerItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Trying to add NULL GobTrackerItem to the GobTrackerList");
            }

            if (!_gobTrackerItems.Contains(item))
                _gobTrackerItems.Add(item);
        }

        /// <summary>
        /// Updates the player.
        /// </summary>
        public override void Update()
        {
            foreach (var action in BonusActions)
            {
                action.Update();
                if (action.EndTime <= AssaultWing.Instance.DataEngine.ArenaTotalTime)
                    BonusActions.RemoveLater(action);
            }
            BonusActions.CommitRemoves();

            if (AssaultWing.Instance.NetworkMode != NetworkMode.Client)
            {
                // Give birth to a new ship if it's time.
                if (Ship == null && _lives != 0 &&
                    _shipSpawnTime <= AssaultWing.Instance.DataEngine.ArenaTotalTime)
                {
                    CreateShip();
                }

                // Check player controls.
                ApplyControlsToShip();
            }
            else // otherwise we are a game client
            {
                // As a client, we only care about local player controls.
                if (!IsRemote)
                {
                    SendControlsToServer();
                    ApplyControlsToShip();
                }
            }

            // Game server sends state updates about players to game clients.
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server && MustUpdateToClients)
            {
                MustUpdateToClients = false;
                var message = new PlayerUpdateMessage();
                message.PlayerID = ID;
                message.Write(this, SerializationModeFlags.VaryingData);
                AssaultWing.Instance.NetworkEngine.GameClientConnections.Send(message);
            }
        }

        private void CreateDeathMessage(string message, Color messageColor, Vector2 Pos, bool isSuicide)
        {
            string iconName = "b_icon_add_kill";

            if (isSuicide) iconName = "b_icon_take_life";
            
            Gob.CreateGob<ArenaMessage>((CanonicalString)"deathmessage", gob =>
            {
                gob.ResetPos(Pos, gob.Move, gob.Rotation);
                gob.Message = message;
                gob.IconName = iconName;
                gob.DrawColor = messageColor;
                AssaultWing.Instance.DataEngine.Arena.Gobs.Add(gob);
            });
        }

        private void SendDeathMessageToBystanders(DeathCause cause, string bystanderMessage)
        {
            foreach (Player plr in AssaultWing.Instance.DataEngine.Players)
            {
                if (plr.ID != ID)
                {
                    if (cause.IsKill && plr.ID == cause.Killer.Owner.ID)
                    {
                    }
                    else
                    {
                        plr.SendMessage(bystanderMessage, KILL_COLOR);
                    }
                }
            }
        }

        private static readonly int KILLINGSPREE_KILLS_REQUIRED = 3;

        private void SendKillingSpreeMessage(Player player)
        {
            string message = player.Name + " IS ON FIRE! (" + player.KillsWithoutDying + " kills)";

            if (player.KillsWithoutDying > 5)
            {
                message = player.Name + " IS UNSTOPPABLE! (" + player.KillsWithoutDying + " kills) OMG!";
            }

            foreach (Player plr in AssaultWing.Instance.DataEngine.Players)
            {
                plr.SendMessage(message, KILLING_SPREE_COLOR);
            }
        }

        /// <summary>
        /// Performs necessary operations when the player's ship dies.
        /// </summary>
        /// <param name="cause">The cause of death of the player's ship</param>
        public void Die(DeathCause cause)
        {
            // Dying has some consequences.
            if (cause.IsSuicide) ++_suicides;
            if (cause.IsKill)
            {
                ++cause.Killer.Owner._kills;
                if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                    cause.Killer.Owner.MustUpdateToClients = true;
            }
            
            // Take a life (it's not easy to die)
            --_lives;
            // Reset killing-spree
            KillsWithoutDying = 0;
            BonusActions.Clear();

            var bystanderMessage = "";

            if (cause.IsKill)
            {
                cause.Killer.Owner.SendMessage("You nailed " + Name, KILL_COLOR);
                // Increase killing spree
                ++cause.Killer.Owner.KillsWithoutDying;
                // If Killer is on a KillingSpree, send message
                if (cause.Killer.Owner.KillsWithoutDying >= KILLINGSPREE_KILLS_REQUIRED)
                    SendKillingSpreeMessage(cause.Killer.Owner);
                CreateDeathMessage(cause.Killer.Owner.Name, cause.Killer.Owner.PlayerColor, Ship.Pos, false);
                bystanderMessage = cause.Killer.Owner.Name + " fragged " + Name;
            }
            if (cause.IsSuicide)
            {
                CreateDeathMessage(Name, PlayerColor, Ship.Pos, true);
                bystanderMessage = Name + " could not take it anymore";
            }

            // Send message about death to other players too
            SendDeathMessageToBystanders(cause, bystanderMessage);

            Ship = null;

            // Notify the player about his death and possible killer about his frag.
            SendMessage("Death by " + cause.ToPersonalizedString(this), DEATH_COLOR);

            // Schedule the making of a new ship, lives permitting.
            _shipSpawnTime = AssaultWing.Instance.DataEngine.ArenaTotalTime + TimeSpan.FromSeconds(MOURNING_DELAY);

            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                MustUpdateToClients = true;
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
            Lives = AssaultWing.Instance.DataEngine.GameplayMode.StartLives;
            BonusActions.Clear();
            Messages.Clear();
            Ship = null;
        }

        public static readonly Color DEFAULT_COLOR = new Color(1f, 1f, 1f, 1f);
        public static readonly Color BONUS_COLOR = new Color(0.3f, 0.7f, 1f, 1f);
        public static readonly Color DEATH_COLOR = new Color(1f, 0.2f, 0.2f, 1f);
        public static readonly Color KILL_COLOR = new Color(0.2f, 1f, 0.2f, 1f);
        public static readonly Color KILLING_SPREE_COLOR = new Color(255, 228, 0);
        public static readonly Color PLAYER_STATUS_COLOR = new Color(1f, 0.52f, 0.13f);

        public void SendMessage(string message)
        {
            SendMessage(message, DEFAULT_COLOR);
        }

        /// <summary>
        /// Sends a message to the player. The message will be displayed on the player's screen.
        /// </summary>
        public void SendMessage(string message, Color messageColor)
        {
            if (message == null) throw new ArgumentNullException("Null message");
            message = message.Replace("\n", " ");
            message = message.Capitalize();

            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server && IsRemote)
            {
                var messageMessage = new PlayerMessageMessage { PlayerID = ID, Color = messageColor, Text = message };
                AssaultWing.Instance.NetworkEngine.GameClientConnections[ConnectionID].Send(messageMessage);
            }
            Message msg = new Message(message);
            msg.TextColor = messageColor;
            Messages.Add(msg);

            // Throw away very old messages.
            if (Messages.Count >= 2 * MESSAGE_KEEP_COUNT)
                Messages.RemoveRange(0, Messages.Count - MESSAGE_KEEP_COUNT);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (Ship != null)
                Ship.Die(new DeathCause());
        }

        #endregion General public methods

        #region Methods related to serialisation

        /// <summary>
        /// Serialises the gob to a binary writer.
        /// </summary>
        /// Subclasses should call the base implementation
        /// before performing their own serialisation.
        /// <param name="writer">The writer where to write the serialised data.</param>
        /// <param name="mode">Which parts of the gob to serialise.</param>
        public override void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)ShipName.Canonical);
                writer.Write((int)Weapon2Name.Canonical);
                writer.Write((int)ExtraDeviceName.Canonical);
                writer.Write((Color)PlayerColor);
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((short)_lives);
                writer.Write((short)_kills);
                writer.Write((short)_suicides);
                writer.Write((byte)PostprocessEffectNames.Count);
                foreach (var effectName in PostprocessEffectNames)
                    writer.Write((int)effectName.Canonical);
                BonusActions.Serialize(writer, mode);
            }
        }

        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode, TimeSpan messageAge)
        {
            base.Deserialize(reader, mode, messageAge);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                ShipName = new CanonicalString(reader.ReadInt32());
                Weapon2Name = new CanonicalString(reader.ReadInt32());
                ExtraDeviceName = new CanonicalString(reader.ReadInt32());
                PlayerColor = reader.ReadColor();
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                _lives = reader.ReadInt16();
                _kills = reader.ReadInt16();
                _suicides = reader.ReadInt16();
                int effectNameCount = reader.ReadByte();
                PostprocessEffectNames.Clear();
                for (int i = 0; i < effectNameCount; ++i)
                    PostprocessEffectNames.Add(new CanonicalString(reader.ReadInt32()));
                BonusActions.Deserialize(reader, mode, messageAge);
            }
        }

        #endregion Methods related to serialisation

        #region Private methods

        /// <summary>
        /// Applies the player's controls to his ship, if there is any.
        /// </summary>
        private void ApplyControlsToShip()
        {
            if (Ship == null || Ship.IsDisposed) return;
            if (Controls.Thrust.Force > 0)
                Ship.Thrust(Controls.Thrust.Force, AssaultWing.Instance.GameTime.ElapsedGameTime, Ship.Rotation);
            if (Controls.Left.Force > 0)
                Ship.TurnLeft(Controls.Left.Force, AssaultWing.Instance.GameTime.ElapsedGameTime);
            if (Controls.Right.Force > 0)
                Ship.TurnRight(Controls.Right.Force, AssaultWing.Instance.GameTime.ElapsedGameTime);
            if (Controls.Fire1.Pulse || Controls.Fire1.Force > 0)
                Ship.Weapon1.Fire(Controls.Fire1.State);
            if (Controls.Fire2.Pulse || Controls.Fire2.Force > 0)
                Ship.Weapon2.Fire(Controls.Fire2.State);
            if (Controls.Extra.Pulse || Controls.Extra.Force > 0)
                Ship.ExtraDevice.Fire(Controls.Extra.State);
        }

        /// <summary>
        /// Sends the player's controls to the game server.
        /// </summary>
        private void SendControlsToServer()
        {
            PlayerControlsMessage message = new PlayerControlsMessage();
            message.PlayerID = ID;
            foreach (PlayerControlType controlType in Enum.GetValues(typeof(PlayerControlType)))
                message.SetControlState(controlType, Controls[controlType].State);
            AssaultWing.Instance.NetworkEngine.GameServerConnection.Send(message);
        }

        /// <summary>
        /// Creates a ship for the player.
        /// </summary>
        private void CreateShip()
        {
            // Gain ownership over the ship only after its position has been set.
            // This way the ship won't be affecting its own spawn position.
            Ship = null;
            Gob.CreateGob<Ship>(ShipName, newShip =>
            {
                newShip.Owner = this;
                newShip.SetDeviceType(ShipDevice.OwnerHandleType.PrimaryWeapon, Weapon1Name);
                newShip.SetDeviceType(ShipDevice.OwnerHandleType.SecondaryWeapon, Weapon2Name);
                newShip.SetDeviceType(ShipDevice.OwnerHandleType.ExtraDevice, ExtraDeviceName);
                PositionShip(newShip);
                AssaultWing.Instance.DataEngine.Arena.Gobs.Add(newShip);
                Ship = newShip;
            });
        }

        private void PositionShip(Ship ship)
        {
            var arena = AssaultWing.Instance.DataEngine.Arena;

            // Use player spawn areas if there's any. Otherwise just randomise a position.
            var spawns =
                from g in arena.Gobs
                let spawn = g as SpawnPlayer
                where spawn != null
                let threat = spawn.GetThreat(this)
                orderby threat ascending
                select spawn;
            var bestSpawn = spawns.FirstOrDefault();
            if (bestSpawn == null)
            {
                var newShipPos = arena.GetFreePosition(ship,
                    new AW2.Helpers.Geometric.Rectangle(Vector2.Zero, arena.Dimensions));
                ship.ResetPos(newShipPos, ship.Move, ship.Rotation);
            }
            else
                bestSpawn.Spawn(ship);
        }

        /// <summary>
        /// Attenuates the player's viewport shake for passed time.
        /// </summary>
        /// This method should be called regularly. It decreases <c>relativeShakeDamage</c>.
        /// <param name="seconds">Passed time in seconds.</param>
        private void AttenuateShake(float seconds)
        {
            // Attenuation is done along a steepening curve;
            // the higher the shake damage the faster the attenuation.
            // 'relativeShakeDamage' is thought of as the value of the curve
            // for some parameter x which represents time to wait for the shake to stop.
            // In effect, this ensures that it won't take too long for
            // even very big shakes to stop.
            float shakeTime = _shakeAttenuationInverseCurve.Evaluate(_relativeShakeDamage);
            shakeTime = Math.Max(0, shakeTime - seconds);
            _relativeShakeDamage = _shakeAttenuationCurve.Evaluate(shakeTime);
        }

        #endregion Private methods
    }
}
