using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.UI;
using AW2.Game.Particles;
using AW2.Net;
using AW2.Net.Messages;

namespace AW2.Game
{
    /// <summary>
    /// Bonuses that a player can have.
    /// </summary>
    /// This enum is closely related to the enum BonusAction which lists
    /// what can happen when a bonus is activated.
    /// <seealso cref="AW2.Game.Gobs.BonusAction"/>
    [Flags]
    public enum PlayerBonus : ushort
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
    [System.Diagnostics.DebuggerDisplay("Id:{Id} name:{Name} shipType:{shipTypeName}")]
    public class Player : INetworkSerializable
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
        /// Least int that is known not to have been used as a player identifier
        /// on this game instance.
        /// </summary>
        /// <see cref="Player.Id"/>
        static int leastUnusedId = 0;

        /// <summary>
        /// Type of ship the player has chosen to fly.
        /// </summary>
        CanonicalString shipTypeName;

        /// <summary>
        /// Type of primary weapon the player has chosen to use.
        /// Note that the player may be forced to use a weapon different from
        /// his original choice.
        /// </summary>
        /// <seealso cref="Weapon1Name"/>
        /// <seealso cref="Weapon1RealName"/>
        CanonicalString weapon1Name;

        /// <summary>
        /// Type of secondary weapon the player has chosen to use.
        /// Note that the player may be forced to use a weapon different from
        /// his original choice.
        /// </summary>
        /// <seealso cref="Weapon2Name"/>
        /// <seealso cref="Weapon2RealName"/>
        CanonicalString weapon2Name;

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
        /// The player's unique identifier.
        /// </summary>
        /// The identifier may change if a remote game server says so.
        public int Id { get; set; }

        /// <summary>
        /// The player's Color on radar.
        /// </summary>
        public Color RadarColor { get; set; }


        /// <summary>
        /// Identifier of the connection behind which this player lives,
        /// or negative if the player lives at the local game instance.
        /// </summary>
        public int ConnectionId { get; private set; }

        /// <summary>
        /// If <c>true</c> then the player is playing at a remote game instance.
        /// If <c>false</c> then the player is playing at this game instance.
        /// </summary>
        public bool IsRemote { get { return ConnectionId >= 0; } }

        /// <summary>
        /// Does the player state need to be updated to the clients.
        /// For use by game server only.
        /// </summary>
        public bool MustUpdateToClients { get; set; }

        /// <summary>
        /// The controls the player uses in menus and in game.
        /// </summary>
        public PlayerControls Controls { get { return controls; } }

        /// <summary>
        /// The ship the player is controlling in the game arena.
        /// </summary>
        public Ship Ship { get; set; }

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
            if (Ship == null) return;
            relativeShakeDamage = Math.Max(0, relativeShakeDamage + damageAmount / Ship.MaxDamageLevel);
        }

        /// <summary>
        /// The human-readable name of the player.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The name of the type of ship the player has chosen to fly.
        /// </summary>
        public CanonicalString ShipName { get { return shipTypeName; } set { shipTypeName = value; } }

        /// <summary>
        /// The name of the primary weapon as the player has chosen it.
        /// </summary>
        public CanonicalString Weapon1Name
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
        public CanonicalString Weapon2Name
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
        public CanonicalString Weapon1RealName
        {
            get
            {
                if (weapon1Upgrades == 0)
                    return weapon1Name;
                Weapon weapon1 = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(TypeTemplateType.Weapon, weapon1Name);
                return weapon1.UpgradeNames[weapon1Upgrades - 1];
            }
        }

        /// <summary>
        /// The name of the secondary weapon, considering all current bonuses.
        /// </summary>
        public CanonicalString Weapon2RealName
        {
            get
            {
                if (weapon2Upgrades == 0)
                    return weapon2Name;
                Weapon weapon2 = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(TypeTemplateType.Weapon, weapon2Name);
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

        #region Constructors

        /// <summary>
        /// Creates a new player.
        /// </summary>
        /// <param name="name">Name of the player.</param>
        /// <param name="shipTypeName">Name of the type of ship the player is flying.</param>
        /// <param name="weapon1Name">Name of the type of main weapon.</param>
        /// <param name="weapon2Name">Name of the type of secondary weapon.</param>
        Player(string name, CanonicalString shipTypeName, CanonicalString weapon1Name, CanonicalString weapon2Name)
        {
            Id = leastUnusedId++;
            this.Name = name;
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
            this.RadarColor = Color.Gray;
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
        public Player(string name, CanonicalString shipTypeName, CanonicalString weapon1Name, CanonicalString weapon2Name,
            PlayerControls controls)
            : this(name, shipTypeName, weapon1Name, weapon2Name)
        {
            ConnectionId = -1;
            this.controls = controls;
        }

        /// <summary>
        /// Creates a new player who plays at a remote game instance.
        /// </summary>
        /// <param name="name">Name of the player.</param>
        /// <param name="shipTypeName">Name of the type of ship the player is flying.</param>
        /// <param name="weapon1Name">Name of the type of main weapon.</param>
        /// <param name="weapon2Name">Name of the type of secondary weapon.</param>
        /// <param name="connectionId">Identifier of the connection to the remote game instance
        /// at which the player lives.</param>
        /// <see cref="AW2.Net.Connection.Id"/>
        public Player(string name, CanonicalString shipTypeName, CanonicalString weapon1Name, CanonicalString weapon2Name,
            int connectionId)
            : this(name, shipTypeName, weapon1Name, weapon2Name)
        {
            ConnectionId = connectionId;
            controls = new PlayerControls();
            controls.thrust = new RemoteControl();
            controls.left = new RemoteControl();
            controls.right = new RemoteControl();
            controls.down = new RemoteControl();
            controls.fire1 = new RemoteControl();
            controls.fire2 = new RemoteControl();
            controls.extra = new RemoteControl();
        }

        #endregion Constructors

        #region General public methods

        /// <summary>
        /// Updates the player.
        /// </summary>
        public void Update()
        {
            // Player bonus expirations.
            foreach (PlayerBonus playerBonus in Enum.GetValues(typeof(PlayerBonus)))
                if (playerBonus != PlayerBonus.None &&
                    (Bonuses & playerBonus) != 0 &&
                    BonusTimeouts[playerBonus] <= AssaultWing.Instance.GameTime.TotalGameTime)
                {
                    RemoveBonus(playerBonus);
                }

            if (AssaultWing.Instance.NetworkMode != NetworkMode.Client)
            {
                // Give birth to a new ship if it's time.
                if (Ship == null && lives != 0 &&
                    shipSpawnTime <= AssaultWing.Instance.GameTime.TotalGameTime)
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
            weapon1Upgrades = 0;
            weapon2Upgrades = 0;
            bonuses = PlayerBonus.None;
            Ship = null;

            // Notify the player about his death and possible killer about his frag.
            SendMessage("Death by " + cause.ToPersonalizedString(this));
            if (cause.IsKill)
                cause.Killer.Owner.SendMessage("You nailed " + Name);
            
            // Schedule the making of a new ship, lives permitting.
            shipSpawnTime = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(mourningDelay);

            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                MustUpdateToClients = true;
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
            Ship = null;
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

        #endregion General public methods

        #region Methods related to bonuses

        /// <summary>
        /// Adds a bonus or bonuses to the player.
        /// </summary>
        /// <param name="bonus">The bonus or bonuses.</param>
        /// <param name="expiryTime">Time of expiry of the bonus or bonuses in game time.</param>
        public void AddBonus(PlayerBonus bonus, TimeSpan expiryTime)
        {
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                MustUpdateToClients = true;
            bonuses |= bonus;
            if ((bonus & PlayerBonus.Weapon1LoadTime) != 0)
            {
                SetBonusTimes(PlayerBonus.Weapon1LoadTime, expiryTime);
                UpgradeWeapon1LoadTime();
            }
            if ((bonus & PlayerBonus.Weapon2LoadTime) != 0)
            {
                SetBonusTimes(PlayerBonus.Weapon2LoadTime, expiryTime);
                UpgradeWeapon2LoadTime();
            }
            if ((bonus & PlayerBonus.Weapon1Upgrade) != 0)
            {
                SetBonusTimes(PlayerBonus.Weapon1Upgrade, expiryTime);
                UpgradeWeapon1();
            }
            if ((bonus & PlayerBonus.Weapon2Upgrade) != 0)
            {
                SetBonusTimes(PlayerBonus.Weapon2Upgrade, expiryTime);
                UpgradeWeapon2();
            }
        }

        /// <summary>
        /// Removes a bonus from the player.
        /// </summary>
        /// <param name="bonus">The bonus.</param>
        public void RemoveBonus(PlayerBonus bonus)
        {
            if (AssaultWing.Instance.NetworkMode == NetworkMode.Server)
                MustUpdateToClients = true;
            bonuses &= ~bonus;
            if ((bonus & PlayerBonus.Weapon1LoadTime) != 0)
                DeupgradeWeapon1LoadTime();
            if ((bonus & PlayerBonus.Weapon2LoadTime) != 0)
                DeupgradeWeapon2LoadTime();
            if ((bonus & PlayerBonus.Weapon1Upgrade) != 0)
                DeupgradeWeapon1();
            if ((bonus & PlayerBonus.Weapon2Upgrade) != 0)
                DeupgradeWeapon2();
        }

        /// <summary>
        /// Does the player have a bonus.
        /// </summary>
        /// <param name="bonus">The bonus to query.</param>
        /// <returns><c>true</c> if the player has the bonus, <c>false</c> otherwise.</returns>
        public bool HasBonus(PlayerBonus bonus)
        {
            return (bonuses & bonus) != 0;
        }

        /// <summary>
        /// Adds an incremental upgrade on the player's primary weapon.
        /// </summary>
        void UpgradeWeapon1()
        {
            Weapon weapon1 = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(TypeTemplateType.Weapon, weapon1Name);
            weapon1Upgrades = Math.Min(weapon1Upgrades + 1, weapon1.UpgradeNames.Length + 1);
            UpdateShipWeapons();
        }

        /// <summary>
        /// Removes all incremental upgrades from the player's primary weapon.
        /// </summary>
        void DeupgradeWeapon1()
        {
            weapon1Upgrades = 0;
            UpdateShipWeapons();
        }

        /// <summary>
        /// Adds an incremental upgrade on the player's secondary weapon.
        /// </summary>
        void UpgradeWeapon2()
        {
            Weapon weapon2 = (Weapon)AssaultWing.Instance.DataEngine.GetTypeTemplate(TypeTemplateType.Weapon, weapon2Name);
            weapon2Upgrades = Math.Min(weapon2Upgrades + 1, weapon2.UpgradeNames.Length);
            UpdateShipWeapons();
        }

        /// <summary>
        /// Removes all incremental upgrades from the player's secondary weapon.
        /// </summary>
        void DeupgradeWeapon2()
        {
            weapon2Upgrades = 0;
            UpdateShipWeapons();
        }

        /// <summary>
        /// Upgrades primary weapon's load time.
        /// </summary>
        void UpgradeWeapon1LoadTime()
        {
        }

        /// <summary>
        /// Cancels a previous upgrade of primary weapon's load time.
        /// </summary>
        void DeupgradeWeapon1LoadTime()
        {
        }

        /// <summary>
        /// Upgrades secondary weapon's load time.
        /// </summary>
        void UpgradeWeapon2LoadTime()
        {
        }

        /// <summary>
        /// Cancels a previous upgrade of secondary weapon's load time.
        /// </summary>
        void DeupgradeWeapon2LoadTime()
        {
        }

        #endregion Methods related to bonuses

        #region Methods related to serialisation

        /// <summary>
        /// Serialises the gob to a binary writer.
        /// </summary>
        /// Subclasses should call the base implementation
        /// before performing their own serialisation.
        /// <param name="writer">The writer where to write the serialised data.</param>
        /// <param name="mode">Which parts of the gob to serialise.</param>
        public void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)Id);
                writer.Write(Name, 32, true);
                writer.Write(shipTypeName, 32, true);
                writer.Write(weapon1Name, 32, true);
                writer.Write(weapon2Name, 32, true);
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                writer.Write((sbyte)weapon1Upgrades);
                writer.Write((sbyte)weapon2Upgrades);
                writer.Write((ushort)bonuses);
                writer.Write((short)lives);
                writer.Write((short)kills);
                writer.Write((short)suicides);
            }
        }

        /// <summary>
        /// Deserialises the gob from a binary writer.
        /// </summary>
        /// Subclasses should call the base implementation
        /// before performing their own deserialisation.
        /// <param name="reader">The reader where to read the serialised data.</param>
        /// <param name="mode">Which parts of the gob to deserialise.</param>
        public void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                Id = reader.ReadInt32();
                Name = reader.ReadString(32);
                shipTypeName = (CanonicalString)reader.ReadString(32);
                weapon1Name = (CanonicalString)reader.ReadString(32);
                weapon2Name = (CanonicalString)reader.ReadString(32);
            }
            if ((mode & SerializationModeFlags.VaryingData) != 0)
            {
                weapon1Upgrades = reader.ReadSByte();
                weapon2Upgrades = reader.ReadSByte();
                PlayerBonus oldBonuses = bonuses;
                PlayerBonus newBonuses = (PlayerBonus)reader.ReadUInt16();
                RemoveBonus(oldBonuses & (oldBonuses ^ newBonuses));
                AddBonus(newBonuses & (oldBonuses ^ newBonuses), 
                    AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(999)); // HACK: bonus expiryTime
                lives = reader.ReadInt16();
                kills = reader.ReadInt16();
                suicides = reader.ReadInt16();
            }
        }

        #endregion Methods related to serialisation

        #region Private methods

        /// <summary>
        /// Applies the player's controls to his ship, if there is any.
        /// </summary>
        void ApplyControlsToShip()
        {
            if (Ship != null)
            {
                if (controls[PlayerControlType.Thrust].Force > 0)
                    Ship.Thrust(controls[PlayerControlType.Thrust].Force);
                if (controls[PlayerControlType.Left].Force > 0)
                    Ship.TurnLeft(controls[PlayerControlType.Left].Force);
                if (controls[PlayerControlType.Right].Force > 0)
                    Ship.TurnRight(controls[PlayerControlType.Right].Force);
                if (controls[PlayerControlType.Fire1].Pulse)
                    Ship.Fire1();
                if (controls[PlayerControlType.Fire2].Pulse)
                    Ship.Fire2();
                if (controls[PlayerControlType.Extra].Pulse)
                    Ship.DoExtra();
            }
        }

        /// <summary>
        /// Sends the player's controls to the game server.
        /// </summary>
        void SendControlsToServer()
        {
            PlayerControlsMessage message = new PlayerControlsMessage();
            message.PlayerId = Id;
            foreach (PlayerControlType controlType in Enum.GetValues(typeof(PlayerControlType)))
            {
                Control control = controls[controlType];
                message.SetControlState(controlType,
                    new PlayerControlsMessage.ControlState { force = control.Force, pulse = control.Pulse });
            }
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
                newShip.Weapon1Name = weapon1Name;
                newShip.Weapon2Name = weapon2Name;

                // Find a starting place for the new ship.
                // Use player spawn areas if there's any. Otherwise just randomise a position.
                SpawnPlayer bestSpawn = null;
                float bestSafeness = float.MinValue;
                foreach (var otherGob in arena.Gobs)
                {
                    SpawnPlayer spawn = otherGob as SpawnPlayer;
                    if (spawn == null) continue;
                    float safeness = spawn.GetSafeness();
                    if (safeness >= bestSafeness)
                    {
                        bestSafeness = safeness;
                        bestSpawn = spawn;
                    }
                }
                if (bestSpawn == null)
                    newShip.Pos = arena.GetFreePosition(newShip,
                        new AW2.Helpers.Geometric.Rectangle(Vector2.Zero, arena.Dimensions));
                else
                    bestSpawn.Spawn(newShip);

                arena.Gobs.Add(newShip);
                Ship = newShip;
            });

            // Create a player marker for the ship.
            var particleEngineName = new CanonicalString(Id == 1 ? "playerred" : "playergreen");
            Gob.CreateGob(particleEngineName, playerColor =>
            {
                if (playerColor is ParticleEngine)
                {
                    ParticleEngine particleEngine = (ParticleEngine)playerColor;
                    particleEngine.Pos = Ship.Pos;
                    particleEngine.Rotation = Ship.Rotation;
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
        /// Makes the player's ship update its weapons according to
        /// the player's current weapon choice and active bonuses.
        /// </summary>
        void UpdateShipWeapons()
        {
            if (Ship == null) return;
            if (Ship.Weapon1Name != Weapon1RealName)
                Ship.Weapon1Name = Weapon1RealName;
            if (Ship.Weapon2Name != Weapon2RealName)
                Ship.Weapon2Name = Weapon2RealName;
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

        void SetBonusTimes(PlayerBonus bonus, TimeSpan expiryTime)
        {
            BonusTimeins[bonus] = AssaultWing.Instance.GameTime.TotalGameTime;
            BonusTimeouts[bonus] = expiryTime;
        }

        #endregion Private methods
    }
}
