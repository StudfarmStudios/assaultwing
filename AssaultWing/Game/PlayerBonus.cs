using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;

namespace AW2.Game
{
    /// <summary>
    /// Bonuses that a player can have.
    /// </summary>
    /// This enum is closely related to the enum BonusAction which lists
    /// what can happen when a bonus is activated.
    /// <seealso cref="AW2.Game.Gobs.BonusAction"/>
    [Flags]
    public enum PlayerBonusTypes : ushort
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
    /// PlayerBonus
    /// </summary>
    public struct PlayerBonus
    {
        static Texture2D bonusIconWeapon1LoadTimeTexture;
        static Texture2D bonusIconWeapon2LoadTimeTexture;

        /// <summary>
        /// Type of the bonus.
        /// </summary>
        public PlayerBonusTypes Types { get; set; }

        /// <summary>
        /// Explicit conversion from PlayerBonusTypes.
        /// </summary>
        public static explicit operator PlayerBonus(PlayerBonusTypes types)
        {
            return new PlayerBonus { Types = types };
        }

        /// <summary>
        /// Returns data related to PlayerBonus
        /// </summary>
        public PlayerBonusData GetData(Player player)
        {
            string bonusText;
            string bonusIconName;
            Texture2D bonusIcon;

            var data = AssaultWing.Instance.DataEngine;

            switch (Types)
            {
                case PlayerBonusTypes.Weapon1LoadTime:
                    bonusText = player.Weapon1RealName + "\nspeedloader";
                    bonusIconName = "b_icon_rapid_fire_1";
                    bonusIcon = bonusIconWeapon1LoadTimeTexture;
                    break;
                case PlayerBonusTypes.Weapon2LoadTime:
                    bonusText = player.Weapon2RealName + "\nspeedloader";
                    bonusIconName = "b_icon_rapid_fire_1";
                    bonusIcon = bonusIconWeapon2LoadTimeTexture;
                    break;
                case PlayerBonusTypes.Weapon1Upgrade:
                    {
                        Weapon weapon1 = player.Ship != null
                            ? player.Ship.Devices.Weapon1
                            : (Weapon)data.GetTypeTemplate(player.Weapon1RealName);
                        bonusText = player.Weapon1RealName;
                        bonusIconName = weapon1.IconName;
                        bonusIcon = AssaultWing.Instance.Content.Load<Texture2D>(bonusIconName);
                    }
                    break;
                case PlayerBonusTypes.Weapon2Upgrade:
                    {
                        Weapon weapon2 = player.Ship != null
                            ? player.Ship.Devices.Weapon2
                            : (Weapon)data.GetTypeTemplate(player.Weapon2RealName);
                        bonusText = player.Weapon2RealName;
                        bonusIconName = weapon2.IconName;
                        bonusIcon = AssaultWing.Instance.Content.Load<Texture2D>(bonusIconName);
                    }
                    break;
                default:
                    bonusText = "<unknown>";
                    bonusIconName = "dummytexture";
                    bonusIcon = AssaultWing.Instance.Content.Load<Texture2D>(bonusIconName);
                    Log.Write("Warning: Don't know how to draw player bonus box " + Types);
                    throw new ArgumentException("Don't know how to draw player bonus box " + Types);
            }
            return new PlayerBonusData { message = bonusText, icon = bonusIcon, iconName = bonusIconName };
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public static void LoadContent()
        {
            var content = AssaultWing.Instance.Content;
            bonusIconWeapon1LoadTimeTexture = content.Load<Texture2D>("b_icon_rapid_fire_1");
            bonusIconWeapon2LoadTimeTexture = content.Load<Texture2D>("b_icon_rapid_fire_1");
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public static void UnloadContent()
        {
            // Our textures and fonts are disposed by the graphics engine.
        }

    }

    /// <summary>
    /// Message and Icon of PlayerBonus
    /// </summary>
    public struct PlayerBonusData
    {
        /// <summary>
        /// Description of the bonus
        /// </summary>
        public string message;

        /// <summary>
        /// Visual description of the bonus
        /// </summary>
        public Texture2D icon;

        /// <summary>
        /// Name of the visual description of the bonus
        /// </summary>
        public string iconName;
    }

}
