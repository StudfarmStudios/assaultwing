using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.UI;
using AW2.Game.Particles;

namespace AW2.Game
{
    /// <summary>
    /// Bonuses that a player can have.
    /// </summary>
    /// This enum is closely related to the enum BonusAction which lists
    /// what can happen when a bonus is activated.
    /// <seealso cref="AW2.Game.Gobs.BonusAction"/>
    [Flags]
    public enum PlayerBonus
    {
        /// <summary>
        /// No bonuses
        /// </summary>
        None = 0,

        /// <summary>
        /// Primary weapon's load time upgrade
        /// </summary>
        Weapon1LoadTime = 0x0001,

        /// <summary>
        /// Secondary weapon's load time upgrade
        /// </summary>
        Weapon2LoadTime = 0x0002,

        /// <summary>
        /// Primary weapon upgrade
        /// </summary>
        /// This bonus is cumulative and the number of accumulated
        /// primary weapon upgrades is not expressed in these flags.
        Weapon1Upgrade = 0x0004,

        /// <summary>
        /// Secondary weapon upgrade
        /// </summary>
        /// This bonus is cumulative and the number of accumulated
        /// secondary weapon upgrades is not expressed in these flags.
        Weapon2Upgrade = 0x0008,
    }

    /// <summary>
    /// A collection of values associated with bonuses of a player instance.
    /// </summary>
    public class PlayerBonusItems<T>
    {
        /// <summary>
        /// Items associated with each type of player bonus.
        /// Indexed by bit positions of single flags of <b>PlayerBonus</b>.
        /// </summary>
        T[] items;

        /// <summary>
        /// Items associated with player bonuses.
        /// </summary>
        /// <param name="bonus">The player bonus.</param>
        /// <returns>The item associated with the bonus.</returns>
        public T this[PlayerBonus bonus]
        {
            get
            {
                for (int bit = 0; bit < sizeof(int) * 8; ++bit)
                    if (((int)bonus & (1 << bit)) != 0)
                        return items[bit];
                Log.Write("Warning: Unknown player bonus " + bonus);
                return items[0];
            }

            set
            {
                for (int bit = 0; bit < sizeof(int) * 8; ++bit)
                    if (((int)bonus & (1 << bit)) != 0)
                    {
                        items[bit] = value;
                        return;
                    }
                Log.Write("Warning: Unknown player bonus " + bonus);
            }
        }

        /// <summary>
        /// Creates a new item collection for player bonuses.
        /// </summary>
        public PlayerBonusItems()
        {
            items = new T[sizeof(int) * 8];
        }
    }

    /// <summary>
    /// Player of the game. 
    /// </summary>
    public class Player
    {
        #region Player constants

        /// <summary>
        /// Time between death of player's ship and birth of a new ship,
        /// measured in seconds.
        /// </summary>
        float mourningDelay = 3;

        #endregion Player constants

        #region Player fields about general things

        /// <summary>
        /// Handle to the remote game instance this player lives at,
        /// or <c>null</c> if the player is playing at this game instance.
        /// </summary>
        object remoteHandle;

        /// <summary>
        /// The human-readable name of the player.
        /// </summary>
        protected string name;

        /// <summary>
        /// Type of ship the player has chosen to fly.
        /// </summary>
        string shipTypeName;

        /// <summary>
        /// Type of primary weapon the player has chosen to use.
        /// Note that the player may be forced to use a weapon different from
        /// his original choice.
        /// </summary>
        /// <seealso cref="Weapon1Name"/>
        /// <seealso cref="Weapon1RealName"/>
        string weapon1Name;

        /// <summary>
        /// Type of secondary weapon the player has chosen to use.
        /// Note that the player may be forced to use a weapon different from
        /// his original choice.
        /// </summary>
        /// <seealso cref="Weapon2Name"/>
        /// <seealso cref="Weapon2RealName"/>
        string weapon2Name;

        /// <summary>
        /// Number of active primary weapon upgrades.
        /// </summary>
        /// <b>0</b> means the selected primary weapon is in use,
        /// <b>1</b> means the first upgrade of the selected primary weapon is in use,
        /// etc.
        int weapon1Upgrades;

        /// <summary>
        /// Number of active secondary weapon upgrades.
        /// </summary>
        /// <b>0</b> means the selected secondary weapon is in use,
        /// <b>1</b> means the first upgrade of the selected secondary weapon is in use,
        /// etc.
        int weapon2Upgrades;

        /// <summary>
        /// Bonuses that the player currently has.
        /// </summary>
        /// <b>Weapon1Upgrade</b> and <b>Weapon2Upgrade</b> are set
        /// if the player has one or more upgrades in the weapon. 
        /// The number of accumulated weapon upgrades
        /// is stored in <b>weapon1Upgrades</b> and <b>weapon2Upgrades</b>.
        /// <seealso cref="weapon1Upgrades"/>
        /// <seealso cref="weapon2Upgrades"/>
        PlayerBonus bonuses;

        /// <summary>
        /// Starting times of the player's bonuses.
        /// </summary>
        /// Starting time is the time when the bonus was activated.
        /// <seealso cref="PlayerBonus"/>
        PlayerBonusItems<TimeSpan> bonusTimeins;

        /// <summary>
        /// Ending times of the player's bonuses.
        /// </summary>
        /// <seealso cref="PlayerBonus"/>
        PlayerBonusItems<TimeSpan> bonusTimeouts;

        /// <summary>
        /// Messages to display in the player's chat box, oldest first.
        /// </summary>
        List<string> messages;

        /// <summary>
        /// The player's controls for moving in menus and controlling his ship.
        /// Uninitialised if the player lives at a remote game instance.
        /// </summary>
        protected PlayerControls controls;

        /// <summary>
        /// The ship the player is controlling.
        /// </summary>
        protected Ship ship;

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
        /// The player's index in <c>DataEngine</c>,
        /// or -1 if the player is not registered in <c>DataEngine</c>.
        /// </summary>
        public int Id
        {
            get
            {
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                for (int i = 0; ; ++i)
                {
                    Player player = data.GetPlayer(i);
                    if (player == this) return i;
                    if (player == null) break;
                }
                return -1;
            }
        }

        /// <summary>
        /// If <c>true</c> then the player is playing at a remote game instance.
        /// If <c>false</c> then the player is playing at this game instance.
        /// </summary>
        public bool IsRemote { get { return remoteHandle != null; } }

        /// <summary>
        /// The controls the player uses in menus and in game.
        /// </summary>
        public PlayerControls Controls { get { return controls; } }

        /// <summary>
        /// The ship the player is controlling in the game arena.
        /// </summary>
        public Ship Ship { get { return ship; } set { ship = value; } }

        /// <summary>
        /// How many reincarnations the player has left.
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
                if (AssaultWing.Instance.GameTime.TotalGameTime > shakeUpdateTime)
                {
                    // Attenuate shake damage for any skipped frames.
                    float skippedTime = (float)(AssaultWing.Instance.GameTime.TotalGameTime - AssaultWing.Instance.GameTime.ElapsedGameTime - shakeUpdateTime).TotalSeconds;
                    AttenuateShake(skippedTime);

                    // Calculate new shake.
                    shake = shakeCurve.Evaluate(relativeShakeDamage);
                    shakeUpdateTime = AssaultWing.Instance.GameTime.TotalGameTime;

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
            if (ship == null) return;
            relativeShakeDamage = Math.Max(0, relativeShakeDamage + damageAmount / ship.MaxDamageLevel);
        }

        /// <summary>
        /// The name of the player.
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// The name of the type of ship the player has chosen to fly.
        /// </summary>
        public string ShipName { get { return shipTypeName; } set { shipTypeName = value; } }

        /// <summary>
        /// The name of the primary weapon as the player has chosen it.
        /// </summary>
        public string Weapon1Name
        {
            get { return weapon1Name; }
            set
            {
                weapon1Name = value;
                weapon1Upgrades = 0;
            }
        }

        /// <summary>
        /// The name of the secondary weapon as the player has chosen it.
        /// </summary>
        public string Weapon2Name
        {
            get { return weapon2Name; }
            set
            {
                weapon2Name = value;
                weapon2Upgrades = 0;
            }
        }

        /// <summary>
        /// The name of the primary weapon, considering all current bonuses.
        /// </summary>
        public string Weapon1RealName
        {
            get
            {
                if (weapon1Upgrades == 0)
                    return weapon1Name;
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                Weapon weapon1 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon1Name);
                return weapon1.UpgradeNames[weapon1Upgrades - 1];
            }
        }

        /// <summary>
        /// The name of the secondary weapon, considering all current bonuses.
        /// </summary>
        public string Weapon2RealName
        {
            get
            {
                if (weapon2Upgrades == 0)
                    return weapon2Name;
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                Weapon weapon2 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon2Name);
                return weapon2.UpgradeNames[weapon2Upgrades - 1];
            }
        }

        /// <summary>
        /// On/off bonuses that the player currently has.
        /// </summary>
        public PlayerBonus Bonuses { get { return bonuses; } }

        /// <summary>
        /// Starting times of the player's bonuses.
        /// </summary>
        /// Starting time is the time at which the bonus was activated.
        public PlayerBonusItems<TimeSpan> BonusTimeins { get { return bonusTimeins; } set { bonusTimeins = value; } }

        /// <summary>
        /// Ending times of the player's bonuses.
        /// </summary>
        public PlayerBonusItems<TimeSpan> BonusTimeouts { get { return bonusTimeouts; } set { bonusTimeouts = value; } }

        /// <summary>
        /// Messages to display in the player's chat box, oldest first.
        /// </summary>
        public List<string> Messages { get { return messages; } }

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

        /// <summary>
        /// Creates a new player.
        /// </summary>
        /// <param name="name">Name of the player.</param>
        /// <param name="shipTypeName">Name of the type of ship the player is flying.</param>
        /// <param name="weapon1Name">Name of the type of main weapon.</param>
        /// <param name="weapon2Name">Name of the type of secondary weapon.</param>
        Player(string name, string shipTypeName, string weapon1Name, string weapon2Name)
        {
            this.name = name;
            this.shipTypeName = shipTypeName;
            this.weapon1Name = weapon1Name;
            this.weapon2Name = weapon2Name;
            this.weapon1Upgrades = 0;
            this.weapon2Upgrades = 0;
            this.bonuses = PlayerBonus.None;
            this.bonusTimeins = new PlayerBonusItems<TimeSpan>();
            this.bonusTimeouts = new PlayerBonusItems<TimeSpan>();
            this.messages = new List<string>();
            this.lives = 3;
            this.shipSpawnTime = new TimeSpan(1);
            this.relativeShakeDamage = 0;
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
        }

        
        /// <summary>
        /// Creates a new player who plays at the local game instance.
        /// </summary>
        /// <param name="name">Name of the player.</param>
        /// <param name="shipTypeName">Name of the type of ship the player is flying.</param>
        /// <param name="weapon1Name">Name of the type of main weapon.</param>
        /// <param name="weapon2Name">Name of the type of secondary weapon.</param>
        /// <param name="controls">Player's in-game controls.</param>
        public Player(string name, string shipTypeName, string weapon1Name, string weapon2Name,
            PlayerControls controls)
            : this(name, shipTypeName, weapon1Name, weapon2Name)
        {
            remoteHandle = null;
            this.controls = controls;
        }

        /// <summary>
        /// Creates a new player who plays at a remote game instance.
        /// </summary>
        /// <param name="name">Name of the player.</param>
        /// <param name="shipTypeName">Name of the type of ship the player is flying.</param>
        /// <param name="weapon1Name">Name of the type of main weapon.</param>
        /// <param name="weapon2Name">Name of the type of secondary weapon.</param>
        /// <param name="remoteHandle">Handle to the remote game instance, meaningful to the network engine.</param>
        public Player(string name, string shipTypeName, string weapon1Name, string weapon2Name,
            object remoteHandle)
            : this(name, shipTypeName, weapon1Name, weapon2Name)
        {
            this.remoteHandle = remoteHandle;
        }

        /// <summary>
        /// Updates the player.
        /// </summary>
        public void Update()
        {
            if (AssaultWing.Instance.NetworkMode != NetworkMode.Client)
            {
                // Give birth to a new ship if it's time.
                if (ship == null && lives > 0 &&
                    shipSpawnTime <= AssaultWing.Instance.GameTime.TotalGameTime)
                {
                    CreateShip();
                }

                // Check player controls.
                if (!IsRemote)
                UpdateControlsServerLocal();
                                
                if (IsRemote)
                    UpdateControlsServerRemote();
            }
            else // otherwise we are a game client
            {
                // As a client, we only care about local player controls.
                if (!IsRemote)
                    UpdateControlsClientLocal();
            }
        }

        private void UpdateControlsServerLocal()
        {
            if (ship != null)
            {
                if (controls[PlayerControlType.Thrust].Force > 0)
                    ship.Thrust(controls[PlayerControlType.Thrust].Force);
                if (controls[PlayerControlType.Left].Force > 0)
                    ship.TurnLeft(controls[PlayerControlType.Left].Force);
                if (controls[PlayerControlType.Right].Force > 0)
                    ship.TurnRight(controls[PlayerControlType.Right].Force);
                if (controls[PlayerControlType.Fire1].Pulse)
                    ship.Fire1();
                if (controls[PlayerControlType.Fire2].Pulse)
                    ship.Fire2();
                if (controls[PlayerControlType.Extra].Pulse)
                    ship.DoExtra();
            }
        }

        private void UpdateControlsClientLocal()
        {
            foreach (PlayerControlType controlType in Enum.GetValues(typeof(PlayerControlType)))
            {
                Control control = controls[controlType];

                // TODO: We can skip sending an event if the control is known to be used only
                // as a pulse control and control.Pulse is false (even if control.Force > 0).
                if (control.Force > 0 || control.Pulse)
                {
                    /* TODO: Player at game client sends controls to network engine
                                            PlayerControlEvent eve = new PlayerControlEvent(player.Name, controlType, control.Force, control.Pulse);
                                            eventEngine.SendEvent(eve);
                     */
                }
            }
        }

        private void UpdateControlsServerRemote()
        {
            if (ship != null)
            {
#if false // TODO: Remote player at game server receives controls from network engine. Make sure each client sends only his players' controls.
                    bool doneThrust = false;
                    bool doneLeft = false;
                    bool doneRight = false;
                    bool doneDown = false;
                    bool doneFire1 = false;
                    bool doneFire2 = false;
                    bool doneExtra = false;
                    for (PlayerControlEvent controlEve = eventer.GetEvent<PlayerControlEvent>(); controlEve != null;
                        controlEve = eventer.GetEvent<PlayerControlEvent>())
                    {
                        Player player = data.GetPlayer(controlEve.PlayerName);
                        if (player == null) continue;
                        if (player.Ship == null) continue;
                        switch (controlEve.ControlType)
                        {
                            case PlayerControlType.Thrust:
                                if (doneThrust) break;
                                doneThrust = true;
                                player.Ship.Thrust(controlEve.Force);
                                break;
                            case PlayerControlType.Left:
                                if (doneLeft) break;
                                doneLeft = true;
                                player.Ship.TurnLeft(controlEve.Force);
                                break;
                            case PlayerControlType.Right:
                                if (doneRight) break;
                                doneRight = true;
                                player.Ship.TurnRight(controlEve.Force);
                                break;
                            case PlayerControlType.Down:
                                if (doneDown) break;
                                doneDown = true;
                                // This has no effect during a game.
                                break;
                            case PlayerControlType.Fire1:
                                if (doneFire1) break;
                                doneFire1 = true;
                                if (controlEve.Pulse)
                                    player.Ship.Fire1();
                                break;
                            case PlayerControlType.Fire2:
                                if (doneFire2) break;
                                doneFire2 = true;
                                if (controlEve.Pulse)
                                    player.Ship.Fire2();
                                break;
                            case PlayerControlType.Extra:
                                if (doneExtra) break;
                                doneExtra = true;
                                if (controlEve.Pulse)
                                    player.Ship.DoExtra();
                                break;
                            default:
                                throw new ArgumentException("Unexpected player control type " +
                                    Enum.GetName(typeof(PlayerControlType), controlEve.ControlType));
                        }
                    }
#endif
            }
        }

        /// <summary>
        /// Performs necessary operations when the player's ship dies.
        /// </summary>
        /// <param name="cause">The cause of death of the player's ship</param>
        public void Die(DeathCause cause)
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            PhysicsEngine physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));

            // Dying has some consequences.
            if (cause.IsSuicide) ++suicides;
            if (cause.IsKill) ++cause.Killer.Owner.kills;
            --lives;
            weapon1Upgrades = 0;
            weapon2Upgrades = 0;
            bonuses = PlayerBonus.None;
            ship = null;

            // Notify the player about his death and possible killer about his frag.
            SendMessage("Death by " + cause.ToPersonalizedString(this));
            if (cause.IsKill)
                cause.Killer.Owner.SendMessage("You nailed " + Name);
            
            // Schedule the making of a new ship, lives permitting.
            long ticks = (long)(mourningDelay * TimeSpan.TicksPerSecond);
            shipSpawnTime = AssaultWing.Instance.GameTime.TotalGameTime + new TimeSpan(ticks);
        }

        /// <summary>
        /// Resets the player's internal state for a new arena.
        /// Note that e.g. lives must be set by some external entity.
        /// </summary>
        public void Reset()
        {
            weapon1Upgrades = 0;
            weapon2Upgrades = 0;
            bonuses = PlayerBonus.None;
            bonusTimeins = new PlayerBonusItems<TimeSpan>();
            bonusTimeouts = new PlayerBonusItems<TimeSpan>();
            ship = null;
            shipSpawnTime = new TimeSpan(1);
            relativeShakeDamage = 0;
        }

        /// <summary>
        /// Sends a message to the player. The message will be displayed on the
        /// player's screen.
        /// </summary>
        /// <param name="message">The message.</param>
        public void SendMessage(string message)
        {
            TimeSpan time = AssaultWing.Instance.GameTime.TotalGameTime;
            messages.Add(string.Format("[{0}:{1:d2}] {2}", (int)time.TotalMinutes, time.Seconds, message));

            // Throw away very old messages.
            if (messages.Count > 10000)
                messages.RemoveRange(0, messages.Count - 5000);
        }

        /// <summary>
        /// Creates a ship for the player.
        /// </summary>
        private void CreateShip()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            PhysicsEngine physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));

            // Gain ownership over the ship only after its position has been set.
            // This way the ship won't be affecting its own spawn position.
            ship = null;
            Ship newShip = (Ship)Gob.CreateGob(shipTypeName);
            newShip.Owner = this;
            newShip.Weapon1Name = weapon1Name;
            newShip.Weapon2Name = weapon2Name;

            // Find a starting place for the new ship.
            // Use player spawn areas if there's any. Otherwise just randomise a position.
            SpawnPlayer bestSpawn = null;
            float bestSafeness = float.MinValue;
            data.ForEachGob(delegate(Gob gob)
            {
                SpawnPlayer spawn = gob as SpawnPlayer;
                if (spawn == null) return;
                float safeness = spawn.GetSafeness();
                if (safeness >= bestSafeness)
                {
                    bestSafeness = safeness;
                    bestSpawn = spawn;
                }
            });
            if (bestSpawn == null)
                newShip.Pos = physics.GetFreePosition(newShip, new AW2.Helpers.Geometric.Rectangle(Vector2.Zero, data.Arena.Dimensions));
            else
                bestSpawn.Spawn(newShip);

            data.AddGob(newShip);
            ship = newShip;

            // Create a player marker for the ship.
            string particleEngineName = Id == 1 ? "playerred" : "playergreen";
            Gob playerColor = Gob.CreateGob(particleEngineName);
            if (playerColor is ParticleEngine)
            {
                ParticleEngine particleEngine = (ParticleEngine)playerColor;
                particleEngine.Pos = ship.Pos;
                particleEngine.Rotation = ship.Rotation;
                particleEngine.Owner = this;
                particleEngine.Leader = ship;
            }
            else if (playerColor is Peng)
            {
                Peng peng = (Peng)playerColor;
                peng.Owner = this;
                peng.Leader = ship;
            }
            data.AddGob(playerColor);

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

        #region Methods related to bonuses

        /// <summary>
        /// Adds an incremental upgrade on the player's primary weapon.
        /// </summary>
        public void UpgradeWeapon1()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon weapon1 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon1Name);
            int oldWeapon1Upgrades = weapon1Upgrades;
            weapon1Upgrades = Math.Min(weapon1Upgrades + 1, weapon1.UpgradeNames.Length + 1);

            // Only change our weapon if it's a new one.
            if (oldWeapon1Upgrades != weapon1Upgrades)
                ship.Weapon1Name = Weapon1RealName;

            bonuses |= PlayerBonus.Weapon1Upgrade;
        }

        /// <summary>
        /// Removes all incremental upgrades from the player's primary weapon.
        /// </summary>
        public void DeupgradeWeapon1()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon weapon1 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon1Name);
            int oldWeapon1Upgrades = weapon1Upgrades;
            weapon1Upgrades = 0;

            // Only change our weapon if it's a new one.
            if (oldWeapon1Upgrades != weapon1Upgrades)
                ship.Weapon1Name = Weapon1RealName;

            bonuses &= ~PlayerBonus.Weapon1Upgrade;
        }

        /// <summary>
        /// Adds an incremental upgrade on the player's secondary weapon.
        /// </summary>
        public void UpgradeWeapon2()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon weapon2 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon2Name);
            int oldWeapon2Upgrades = weapon2Upgrades;
            weapon2Upgrades = Math.Min(weapon2Upgrades + 1, weapon2.UpgradeNames.Length);

            // Only change our weapon if it's a new one.
            if (oldWeapon2Upgrades != weapon2Upgrades)
                ship.Weapon2Name = Weapon2RealName;

            bonuses |= PlayerBonus.Weapon2Upgrade;
        }

        /// <summary>
        /// Removes all incremental upgrades from the player's secondary weapon.
        /// </summary>
        public void DeupgradeWeapon2()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon weapon1 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon1Name);
            int oldWeapon2Upgrades = weapon2Upgrades;
            weapon2Upgrades = 0;

            // Only change our weapon if it's a new one.
            if (oldWeapon2Upgrades != weapon2Upgrades)
                ship.Weapon2Name = Weapon2RealName;

            bonuses &= ~PlayerBonus.Weapon2Upgrade;
        }

        /// <summary>
        /// Upgrades primary weapon's load time.
        /// </summary>
        public void UpgradeWeapon1LoadTime()
        {
            bonuses |= PlayerBonus.Weapon1LoadTime;

            // Make our ship recreate its weapon.
            ship.Weapon1Name = Weapon1RealName;
        }

        /// <summary>
        /// Cancels a previous upgrade of primary weapon's load time.
        /// </summary>
        public void DeupgradeWeapon1LoadTime()
        {
            bonuses &= ~PlayerBonus.Weapon1LoadTime;

            // Make our ship recreate its weapon.
            ship.Weapon1Name = Weapon1RealName;
        }

        /// <summary>
        /// Upgrades secondary weapon's load time.
        /// </summary>
        public void UpgradeWeapon2LoadTime()
        {
            bonuses |= PlayerBonus.Weapon2LoadTime;

            // Make our ship recreate its weapon.
            ship.Weapon2Name = Weapon2RealName;
        }

        /// <summary>
        /// Cancels a previous upgrade of secondary weapon's load time.
        /// </summary>
        public void DeupgradeWeapon2LoadTime()
        {
            bonuses &= ~PlayerBonus.Weapon2LoadTime;

            // Make our ship recreate its weapon.
            ship.Weapon2Name = Weapon2RealName;
        }

        #endregion Methods related to bonuses
    }
}
