using System;
using System.Collections.Generic;
using System.Text;
using AW2.UI;
using Ship = AW2.Game.Gobs.Ship;
using Microsoft.Xna.Framework;

namespace AW2.Game
{
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
        /// </summary>
        string weapon1Name;

        /// <summary>
        /// Type of secondary weapon the player has chosen to use.
        /// </summary>
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

        /// <summary>
        /// Adds an incremental upgrade on the player's primary weapon.
        /// </summary>
        public void UpgradeWeapon1()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon weapon1 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon1Name);
            weapon1Upgrades = Math.Min(weapon1Upgrades + 1, weapon1.UpgradeNames.Length + 1);
            ship.Weapon1Name = weapon1.UpgradeNames[weapon1Upgrades - 1];
        }

        /// <summary>
        /// Adds an incremental upgrade on the player's secondary weapon.
        /// </summary>
        public void UpgradeWeapon2()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
            Weapon weapon2 = (Weapon)data.GetTypeTemplate(typeof(Weapon), weapon2Name);
            weapon2Upgrades = Math.Min(weapon2Upgrades + 1, weapon2.UpgradeNames.Length);
            ship.Weapon2Name = weapon2.UpgradeNames[weapon2Upgrades - 1];
        }
    }
}
