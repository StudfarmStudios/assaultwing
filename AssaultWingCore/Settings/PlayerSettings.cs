using System;
using AW2.Helpers;
using AW2.Core;

namespace AW2.Settings
{
    public class PlayerSettingsItem
    {
        private string _name;
        private string _shipName;
        private string _weapon2Name;
        private string _extraDeviceName;

        public string Name { get { return _name; } set { _name = value; } }
        public string ShipName { get { return _shipName; } set { _shipName = value; } }
        public string Weapon2Name { get { return _weapon2Name; } set { _weapon2Name = value; } }
        public string ExtraDeviceName { get { return _extraDeviceName; } set { _extraDeviceName = value; } }
    }

    public class PlayerSettings
    {
        public const int PLAYER_NAME_MAX_LENGTH = 12;

        public static readonly PlayerSettingsItem PLAYER1DEFAULT = new PlayerSettingsItem
        {
            Name = "Newbie",
            ShipName = "Plissken",
            Weapon2Name = "bazooka",
            ExtraDeviceName = "repulsor",
        };
        public static readonly PlayerSettingsItem PLAYER2DEFAULT = new PlayerSettingsItem
        {
            Name = "Lamer",
            ShipName = "Bugger",
            Weapon2Name = "rockets",
            ExtraDeviceName = "catmoflage",
        };

        private PlayerSettingsItem _player1;
        private PlayerSettingsItem _player2;

        public PlayerSettingsItem Player1 { get { return _player1; } }
        public PlayerSettingsItem Player2 { get { return _player2; } }

        public PlayerSettings()
        {
            Reset();
        }

        public void Reset()
        {
            _player1 = PLAYER1DEFAULT;
            _player2 = PLAYER2DEFAULT;
        }

        public void Validate(AssaultWingCore game)
        {
            if (CanonicalString.CanRegister) return; // cannot validate until CanonicalStrings are frozen
            Validate(game, Player1, PLAYER1DEFAULT);
            Validate(game, Player2, PLAYER2DEFAULT);
        }

        private static void Validate(AssaultWingCore game, PlayerSettingsItem item, PlayerSettingsItem defaults)
        {
            item.Name = item.Name.Substring(0, Math.Min(item.Name.Length, PLAYER_NAME_MAX_LENGTH));
            if (!IsValidSetting<AW2.Game.Gobs.Ship>(game, item.ShipName)) item.ShipName = defaults.ShipName;
            if (!IsValidSetting<AW2.Game.GobUtils.Weapon>(game, item.Weapon2Name)) item.Weapon2Name = defaults.Weapon2Name;
            if (!IsValidSetting<AW2.Game.GobUtils.ShipDevice>(game, item.ExtraDeviceName)) item.ExtraDeviceName = defaults.ExtraDeviceName;
        }

        private static bool IsValidSetting<T>(AssaultWingCore game, string name)
        {
            if (!CanonicalString.IsRegistered(name)) return false;
            var template = game.DataEngine.GetTypeTemplate((CanonicalString)name);
            if (template == null) return false;
            return template is T;
        }
    }
}
