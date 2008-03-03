using System;
using System.Collections.Generic;
using System.Text;
using AW2.Helpers;
using AW2.UI;
using Ship = AW2.Game.Gobs.Ship;
using Microsoft.Xna.Framework;

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
        #region Player fields

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
        string weapon1Name;

        /// <summary>
        /// Type of secondary weapon the player has chosen to use.
        /// Note that the player may be forced to use a weapon different from
        /// his original choice.
        /// </summary>
        /// <seealso cref="Weapon2Name"/>
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
        /// The player's controls for moving in menus and controlling his ship.
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

        #endregion Player fields

        #region Player properties

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
        /// The name of the player.
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// The name of the primary weapon, considering all current bonuses.
        /// </summary>
        public string Weapon1Name
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
        public string Weapon2Name
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

        #endregion Player properties

        /// <summary>
        /// Creates a new player.
        /// </summary>
        /// <param name="name">Name of the player.</param>
        /// <param name="shipTypeName">Name of the type of ship the player is flying.</param>
        /// <param name="weapon1Name">Name of the type of main weapon.</param>
        /// <param name="weapon2Name">Name of the type of secondary weapon.</param>
        /// <param name="controls">Player's in-game controls.</param>
        public Player(string name, string shipTypeName, string weapon1Name, string weapon2Name,
            PlayerControls controls)
        {
            this.name = name;
            this.controls = controls;
            this.shipTypeName = shipTypeName;
            this.weapon1Name = weapon1Name;
            this.weapon2Name = weapon2Name;
            this.weapon1Upgrades = 0;
            this.weapon2Upgrades = 0;
            this.bonuses = PlayerBonus.None;
            this.bonusTimeins = new PlayerBonusItems<TimeSpan>();
            this.bonusTimeouts = new PlayerBonusItems<TimeSpan>();
            this.lives = 3;
        }

        /// <summary>
        /// Performs necessary operations when the player's ship dies.
        /// </summary>
        public void Die()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            PhysicsEngine physics = (PhysicsEngine)AssaultWing.Instance.Services.GetService(typeof(PhysicsEngine));

            // Disown current ship so that we don't get any more
            // death reports this frame.
            ship.Owner = null;

            // Dying has some consequences.
            --lives;
            weapon1Upgrades = 0;
            weapon2Upgrades = 0;
            bonuses = PlayerBonus.None;

            if (lives > 0)
            {
                // TODO: Create dummy ship for a while, then create new ship.
                ship = (Ship)Gob.CreateGob(shipTypeName);
                ship.Pos = physics.GetFreePosition(ship, null);
                ship.Owner = this;
                ship.Weapon1Name = weapon1Name;
                ship.Weapon2Name = weapon2Name;
                data.AddGob(ship);
            }
        }

        #region Methods related to bonuses

        /// <summary>
        /// Adds an incremental upgrade on the player's primary weapon.
        /// </summary>
        public void UpgradeWeapon1()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon weapon1 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon1Name);
            weapon1Upgrades = Math.Min(weapon1Upgrades + 1, weapon1.UpgradeNames.Length + 1);
            ship.Weapon1Name = Weapon1Name;
            bonuses |= PlayerBonus.Weapon1Upgrade;
        }

        /// <summary>
        /// Removes all incremental upgrades from the player's primary weapon.
        /// </summary>
        public void DeupgradeWeapon1()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon weapon1 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon1Name);
            weapon1Upgrades = 0;
            ship.Weapon1Name = Weapon1Name;
            bonuses &= ~PlayerBonus.Weapon1Upgrade;
        }

        /// <summary>
        /// Adds an incremental upgrade on the player's secondary weapon.
        /// </summary>
        public void UpgradeWeapon2()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon weapon2 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon2Name);
            weapon2Upgrades = Math.Min(weapon2Upgrades + 1, weapon2.UpgradeNames.Length);
            ship.Weapon2Name = Weapon2Name;
            bonuses |= PlayerBonus.Weapon2Upgrade;
        }

        /// <summary>
        /// Removes all incremental upgrades from the player's secondary weapon.
        /// </summary>
        public void DeupgradeWeapon2()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon weapon1 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon1Name);
            weapon2Upgrades = 0;
            ship.Weapon2Name = Weapon2Name;
            bonuses &= ~PlayerBonus.Weapon2Upgrade;
        }

        /// <summary>
        /// Upgrades primary weapon's load time.
        /// </summary>
        public void UpgradeWeapon1LoadTime()
        {
            bonuses |= PlayerBonus.Weapon1LoadTime;

            // Make our ship recreate its weapon.
            ship.Weapon1Name = Weapon1Name;
        }

        /// <summary>
        /// Cancels a previous upgrade of primary weapon's load time.
        /// </summary>
        public void DeupgradeWeapon1LoadTime()
        {
            bonuses &= ~PlayerBonus.Weapon1LoadTime;

            // Make our ship recreate its weapon.
            ship.Weapon1Name = Weapon1Name;
        }

        /// <summary>
        /// Upgrades secondary weapon's load time.
        /// </summary>
        public void UpgradeWeapon2LoadTime()
        {
            bonuses |= PlayerBonus.Weapon2LoadTime;

            // Make our ship recreate its weapon.
            ship.Weapon2Name = Weapon2Name;
        }

        /// <summary>
        /// Cancels a previous upgrade of secondary weapon's load time.
        /// </summary>
        public void DeupgradeWeapon2LoadTime()
        {
            bonuses &= ~PlayerBonus.Weapon2LoadTime;

            // Make our ship recreate its weapon.
            ship.Weapon2Name = Weapon2Name;
        }

        #endregion Methods related to bonuses
    }
}
