using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Gobs;
using AW2.Game.Particles;
using AW2.Helpers;
using AW2.Net;
using AW2.Net.Messages;
using AW2.UI;

namespace AW2.Game
{
    /// <summary>
    /// Player of the game. 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Id:{Id} name:{Name} shipType:{shipTypeName}")]
    public class Player : Spectator
    {
        class LookAtShip : AW2.Graphics.ILookAt
        {
            Vector2 oldPos;
            public Vector2 Position
            {
                get
                {
                    if (Ship != null) oldPos = Ship.Pos;
                    return oldPos;
                }
            }
            public Ship Ship { get; set; }
        }

        #region Player constants

        const int MESSAGE_KEEP_COUNT = 100;

        /// <summary>
        /// Time between death of player's ship and birth of a new ship,
        /// measured in seconds.
        /// </summary>
        const float MOURNING_DELAY = 3;

        #endregion Player constants

        #region Player fields about general things

        /// <summary>
        /// Least int that is known not to have been used as a player identifier
        /// on this game instance.
        /// </summary>
        /// <see cref="Player.Id"/>
        static int leastUnusedId = 0;

        /// <summary>
        /// Type of ship the player has chosen to fly.
        /// </summary>
        CanonicalString shipTypeName;

        public GameActionCollection BonusActions { get; private set; }
        
        /// <summary>
        /// How many reincarnations the player has left.
        /// </summary>
        protected int lives;

        /// <summary>
        /// Time at which the player's ship is born, measured in game time.
        /// </summary>
        TimeSpan shipSpawnTime;

        /// <summary>
        /// Amount of accumulated damage that determines the amount of shake 
        /// the player is suffering right now. Measured relative to
        /// the maximum damage of the player's ship.
        /// </summary>
        /// Shaking affects the player's viewport and is caused by
        /// the player's ship receiving damage.
        float relativeShakeDamage;

        /// <summary>
        /// Function that maps relative shake damage to radians that the player's
        /// viewport will tilt to produce sufficient shake.
        /// </summary>
        Curve shakeCurve;

        /// <summary>
        /// Function that maps a parameter to relative shake damage.
        /// </summary>
        /// Used in attenuating shake.
        Curve shakeAttenuationCurve;

        /// <summary>
        /// Inverse of <c>shakeAttenuationCurve</c>.
        /// </summary>
        /// Used in attenuating shake.
        Curve shakeAttenuationInverseCurve;

        /// <summary>
        /// Current amount of shake. Access this field through property <c>Shake</c>.
        /// </summary>
        float shake;

        /// <summary>
        /// Time when field <c>shake</c> was calculated, in game time.
        /// </summary>
        TimeSpan shakeUpdateTime;

        LookAtShip lookAt;

        Ship ship;

        #endregion Player fields about general things

        #region Player fields about statistics

        /// <summary>
        /// Number of opposing players' ships this player has killed.
        /// </summary>
        int kills;

        /// <summary>
        /// Number of times this player has died for some other reason
        /// than another player killing him.
        /// </summary>
        int suicides;

        #endregion Player fields about statistics

        #region Player properties

        /// <summary>
        /// The player's Color on radar.
        /// </summary>
        public Color PlayerColor { get; set; }

        /// <summary>
        /// If <c>true</c> then the player is playing at a remote game instance.
        /// If <c>false</c> then the player is playing at this game instance.
        /// </summary>
        public bool IsRemote { get { return ConnectionId >= 0; } }

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
        public Ship Ship { get { return ship; } set { lookAt.Ship = ship = value; } }

        /// <summary>
        /// If positive, how many reincarnations the player has left.
        /// If negative, the player has infinite lives.
        /// If zero, the player cannot play.
        /// </summary>
        public int Lives { get { return lives; } set { lives = value; } }

        /// <summary>
        /// Amount of shake the player is suffering right now, in radians.
        /// Shaking affects the player's viewport and is caused by
        /// the player's ship receiving damage.
        /// </summary>
        public float Shake
        {
            get
            {
                if (AssaultWing.Instance.GameTime.TotalArenaTime > shakeUpdateTime)
                {
                    // Attenuate shake damage for any skipped frames.
                    float skippedTime = (float)(AssaultWing.Instance.GameTime.TotalArenaTime - AssaultWing.Instance.GameTime.ElapsedGameTime - shakeUpdateTime).TotalSeconds;
                    AttenuateShake(skippedTime);

                    // Calculate new shake.
                    shake = shakeCurve.Evaluate(relativeShakeDamage);
                    shakeUpdateTime = AssaultWing.Instance.GameTime.TotalArenaTime;

                    // Attenuate shake damage for the current frame.
                    AttenuateShake((float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds);
                }
                return shake;
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
            relativeShakeDamage = Math.Max(0, relativeShakeDamage + damageAmount / Ship.MaxDamageLevel);
        }

        /// <summary>
        /// The name of the type of ship the player has chosen to fly.
        /// </summary>
        public CanonicalString ShipName { get { return shipTypeName; } set { shipTypeName = value; } }

        /// <summary>
        /// The name of the primary weapon as the player has chosen it.
        /// </summary>
        public CanonicalString Weapon1Name
        {
            get
            {
                if (ship != null) return ship.Weapon1Name;
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

        /// <summary>
        /// Messages to display in the player's chat box, oldest first.
        /// </summary>
        public List<string> Messages { get; private set; }

        public PostprocessEffectNameContainer PostprocessEffectNames { get; private set; }

        #endregion Player properties

        #region Player properties about statistics

        /// <summary>
        /// Number of opposing players' ships this player has killed.
        /// </summary>
        public int Kills { get { return kills; } set { kills = value; } }

        /// <summary>
        /// Number of times this player has died for some other reason
        /// than another player killing him.
        /// </summary>
        public int Suicides { get { return suicides; } set { suicides = value; } }

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
        /// <see cref="AW2.Net.Connection.Id"/>
        public Player(string name, CanonicalString shipTypeName, CanonicalString weapon2Name,
            CanonicalString extraDeviceName, int connectionId)
            : this(name, shipTypeName, weapon2Name, extraDeviceName, new PlayerControls
            {
                thrust = new RemoteControl(),
                left = new RemoteControl(),
                right = new RemoteControl(),
                down = new RemoteControl(),
                fire1 = new RemoteControl(),
                fire2 = new RemoteControl(),
                extra = new RemoteControl()
            }, connectionId)
        {
        }

        /// <summary>
        /// Creates a new player.
        /// </summary>
        private Player(string name, CanonicalString shipTypeName, CanonicalString weapon2Name,
            CanonicalString extraDeviceName, PlayerControls controls, int connectionId)
            : base(controls, connectionId)
        {
            Id = leastUnusedId++;
            Name = name;
            this.shipTypeName = shipTypeName;
            Weapon2Name = weapon2Name;
            ExtraDeviceName = extraDeviceName;
            Messages = new List<string>();
            lives = 3;
            shipSpawnTime = new TimeSpan(1);
            relativeShakeDamage = 0;
            PlayerColor = Color.Gray;
            shakeCurve = new Curve();
            shakeCurve.PreLoop = CurveLoopType.Constant;
            shakeCurve.PostLoop = CurveLoopType.Constant;
            shakeCurve.Keys.Add(new CurveKey(0, 0));
            shakeCurve.Keys.Add(new CurveKey(0.15f, 0.0f * MathHelper.PiOver4));
            shakeCurve.Keys.Add(new CurveKey(0.3f, 0.4f * MathHelper.PiOver4));
            shakeCurve.Keys.Add(new CurveKey(0.6f, 0.6f * MathHelper.PiOver4));
            shakeCurve.Keys.Add(new CurveKey(1, MathHelper.PiOver4));
            shakeCurve.ComputeTangents(CurveTangent.Linear);
            shakeAttenuationCurve = new Curve();
            shakeAttenuationCurve.PreLoop = CurveLoopType.Constant;
            shakeAttenuationCurve.PostLoop = CurveLoopType.Linear;
            shakeAttenuationCurve.Keys.Add(new CurveKey(0, 0));
            shakeAttenuationCurve.Keys.Add(new CurveKey(0.05f, 0.01f));
            shakeAttenuationCurve.Keys.Add(new CurveKey(1.0f, 1));
            shakeAttenuationCurve.ComputeTangents(CurveTangent.Linear);
            shakeAttenuationInverseCurve = new Curve();
            shakeAttenuationInverseCurve.PreLoop = CurveLoopType.Constant;
            shakeAttenuationInverseCurve.PostLoop = CurveLoopType.Linear;
            foreach (CurveKey key in shakeAttenuationCurve.Keys)
                shakeAttenuationInverseCurve.Keys.Add(new CurveKey(key.Value, key.Position));
            shakeAttenuationInverseCurve.ComputeTangents(CurveTangent.Linear);
            lookAt = new LookAtShip();
            BonusActions = new GameActionCollection();
            PostprocessEffectNames = new PostprocessEffectNameContainer(this);
        }

        #endregion Constructors

        #region General public methods

        /// <summary>
        /// Updates the player.
        /// </summary>
        public override void Update()
        {
            base.Update();

            foreach (var action in BonusActions)
            {
                action.Update();
                if (action.actionTimeouts <= AssaultWing.Instance.GameTime.TotalArenaTime)
                    BonusActions.RemoveLater(action);
            }
            BonusActions.CommitRemoves();

            if (AssaultWing.Instance.NetworkMode != NetworkMode.Client)
            {
                // Give birth to a new ship if it's time.
                if (Ship == null && lives != 0 &&
                    shipSpawnTime <= AssaultWing.Instance.GameTime.TotalArenaTime)
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
                message.PlayerId = Id;
                message.Write(this, SerializationModeFlags.VaryingData);
                AssaultWing.Instance.NetworkEngine.GameClientConnections.Send(message);
            }
        }

        /// <summary>
        /// Performs necessary operations when the player's ship dies.
        /// </summary>
        /// <param name="cause">The cause of death of the player's ship</param>
        public void Die(DeathCause cause)
        {
            // Dying has some consequences.
            if (cause.IsSuicide) ++suicides;
            if (cause.IsKill)
            {
                ++cause.Killer.Owner.kills;
                if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                    cause.Killer.Owner.MustUpdateToClients = true;
            }
            --lives;

            BonusActions.Clear();
            Ship = null;

            // Notify the player about his death and possible killer about his frag.
            SendMessage("Death by " + cause.ToPersonalizedString(this));
            if (cause.IsKill)
                cause.Killer.Owner.SendMessage("You nailed " + Name);
            
            // Schedule the making of a new ship, lives permitting.
            shipSpawnTime = AssaultWing.Instance.GameTime.TotalArenaTime + TimeSpan.FromSeconds(MOURNING_DELAY);

            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                MustUpdateToClients = true;
        }

        /// <summary>
        /// Creates a viewport for the player.
        /// </summary>
        /// <param name="onScreen">Location of the viewport on screen.</param>
        public override AW2.Graphics.AWViewport CreateViewport(Rectangle onScreen)
        {
            return new AW2.Graphics.PlayerViewport(this, onScreen, lookAt, () => PostprocessEffectNames);
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
        public override void Reset()
        {
            base.Reset();
            shipSpawnTime = new TimeSpan(1);
            relativeShakeDamage = 0;
            Lives = AssaultWing.Instance.DataEngine.GameplayMode.StartLives;
            BonusActions.Clear();
            Ship = null;
        }

        /// <summary>
        /// Sends a message to the player. The message will be displayed on the player's screen.
        /// </summary>
        public void SendMessage(string message)
        {
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server && IsRemote)
            {
                var messageMessage = new PlayerMessageMessage { PlayerId = Id, Text = message };
                AssaultWing.Instance.NetworkEngine.GameClientConnections[ConnectionId].Send(messageMessage);
            }

            var time = AssaultWing.Instance.GameTime.TotalArenaTime;
            var line = string.Format("[{0}:{1:d2}] {2}", (int)time.TotalMinutes, time.Seconds, message);
            Messages.Add(line);

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
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)Id);
                writer.Write(Name, 32, true);
                writer.Write(shipTypeName, 32, true);
                writer.Write(Weapon2Name, 32, true);
                writer.Write((uint)PlayerColor.PackedValue);
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((short)lives);
                writer.Write((short)kills);
                writer.Write((short)suicides);
                writer.Write((short)PostprocessEffectNames.Count);
                foreach (var effectName in PostprocessEffectNames)
                    writer.Write((int)effectName.Canonical);
                // TODO!!! BonusActions.Serialize(writer, mode);
            }
        }

        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode, TimeSpan messageAge)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                Id = reader.ReadInt32();
                Name = reader.ReadString(32);
                shipTypeName = (CanonicalString)reader.ReadString(32);
                Weapon2Name = (CanonicalString)reader.ReadString(32);
                var playerColor = new Color { PackedValue = reader.ReadUInt32() };
                PlayerColor = playerColor;
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                lives = reader.ReadInt16();
                kills = reader.ReadInt16();
                suicides = reader.ReadInt16();
                int effectNameCount = reader.ReadInt16();
                PostprocessEffectNames.Clear();
                for (int i = 0; i < effectNameCount; ++i)
                    PostprocessEffectNames.Add(new CanonicalString(reader.ReadInt32()));
                // TODO!!! BonusActions.Deserialize(reader, mode, messageAge);
            }
        }

        #endregion Methods related to serialisation

        #region Private methods

        /// <summary>
        /// Applies the player's controls to his ship, if there is any.
        /// </summary>
        void ApplyControlsToShip()
        {
            if (Ship == null) return;
            if (Controls.thrust.Force > 0)
                Ship.Thrust(Controls.thrust.Force, AssaultWing.Instance.GameTime.ElapsedGameTime, Ship.Rotation);
            if (Controls.left.Force > 0)
                Ship.TurnLeft(Controls.left.Force, AssaultWing.Instance.GameTime.ElapsedGameTime);
            if (Controls.right.Force > 0)
                Ship.TurnRight(Controls.right.Force, AssaultWing.Instance.GameTime.ElapsedGameTime);
            if (Controls.fire1.Pulse || Controls.fire1.Force > 0)
                Ship.Weapon1.Fire(Controls.fire1.State);
            if (Controls.fire2.Pulse || Controls.fire2.Force > 0)
                Ship.Weapon2.Fire(Controls.fire2.State);
            if (Controls.extra.Pulse || Controls.extra.Force > 0)
                Ship.ExtraDevice.Fire(Controls.extra.State);
        }

        /// <summary>
        /// Sends the player's controls to the game server.
        /// </summary>
        void SendControlsToServer()
        {
            PlayerControlsMessage message = new PlayerControlsMessage();
            message.PlayerId = Id;
            foreach (PlayerControlType controlType in Enum.GetValues(typeof(PlayerControlType)))
                message.SetControlState(controlType, Controls[controlType].State);
            AssaultWing.Instance.NetworkEngine.GameServerConnection.Send(message);
        }

        /// <summary>
        /// Creates a ship for the player.
        /// </summary>
        void CreateShip()
        {
            // Gain ownership over the ship only after its position has been set.
            // This way the ship won't be affecting its own spawn position.
            Ship = null;
            Gob.CreateGob(shipTypeName, gob =>
            {
                if (!(gob is Ship))
                    throw new Exception("Cannot create non-ship ship for player (" + gob.GetType().Name + ")");
                var arena = AssaultWing.Instance.DataEngine.Arena;
                Ship newShip = (Ship)gob;
                newShip.Owner = this;
                newShip.SetDeviceType(ShipDevice.OwnerHandleType.PrimaryWeapon, Weapon1Name);
                newShip.SetDeviceType(ShipDevice.OwnerHandleType.SecondaryWeapon, Weapon2Name);
                newShip.SetDeviceType(ShipDevice.OwnerHandleType.ExtraDevice, ExtraDeviceName);

                // Find a starting place for the new ship.
                // Use player spawn areas if there's any. Otherwise just randomise a position.
                var spawns =
                    from g in arena.Gobs
                    let spawn = g as SpawnPlayer
                    where spawn != null
                    let safeness = spawn.GetSafeness()
                    orderby safeness descending
                    select spawn;
                var bestSpawn = spawns.FirstOrDefault();
                if (bestSpawn == null)
                {
                    var newShipPos = arena.GetFreePosition(newShip,
                        new AW2.Helpers.Geometric.Rectangle(Vector2.Zero, arena.Dimensions));
                    newShip.ResetPos(newShipPos, newShip.Move, newShip.Rotation);
                }
                else
                    bestSpawn.Spawn(newShip);

                arena.Gobs.Add(newShip);
                Ship = newShip;
            });

            // Create a player marker for the ship.
            Gob.CreateGob((CanonicalString)"playerglow", playerColor =>
            {
                if (playerColor is ParticleEngine)
                {
                    var particleEngine = (ParticleEngine)playerColor;
                    particleEngine.ResetPos(Ship.Pos, particleEngine.Move, Ship.Rotation);
                    particleEngine.Owner = this;
                    particleEngine.Leader = Ship;
                }
                else if (playerColor is Peng)
                {
                    Peng peng = (Peng)playerColor;
                    peng.Owner = this;
                    peng.Leader = Ship;
                }
                AssaultWing.Instance.DataEngine.Arena.Gobs.Add(playerColor);
            });
        }

        /// <summary>
        /// Attenuates the player's viewport shake for passed time.
        /// </summary>
        /// This method should be called regularly. It decreases <c>relativeShakeDamage</c>.
        /// <param name="seconds">Passed time in seconds.</param>
        void AttenuateShake(float seconds)
        {
            // Attenuation is done along a steepening curve;
            // the higher the shake damage the faster the attenuation.
            // 'relativeShakeDamage' is thought of as the value of the curve
            // for some parameter x which represents time to wait for the shake to stop.
            // In effect, this ensures that it won't take too long for
            // even very big shakes to stop.
            float shakeTime = shakeAttenuationInverseCurve.Evaluate(relativeShakeDamage);
            shakeTime = Math.Max(0, shakeTime - seconds);
            relativeShakeDamage = shakeAttenuationCurve.Evaluate(shakeTime);
        }

        #endregion Private methods
    }
}
