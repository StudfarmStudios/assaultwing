using System;
using AW2.Core;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Settings
{
    public class PlayerSettingsItem
    {
        public string Name { get; set; }
        public string ShipName { get; set; }
        public string Weapon2Name { get; set; }
        public string ExtraDeviceName { get; set; }
        public string Password { get; set; }
    }

    public class PlayerSettings
    {
        public const int PLAYER_NAME_MAX_LENGTH = 12;
        public const int PLAYER_PASSWORD_MAX_LENGTH = 12;

        public static readonly PlayerSettingsItem PLAYER1DEFAULT = new PlayerSettingsItem
        {
            Name = "Newbie",
            ShipName = "Plissken",
            Weapon2Name = "bazooka",
            ExtraDeviceName = "repulsor",
            Password = "",
        };
        public static readonly PlayerSettingsItem PLAYER2DEFAULT = new PlayerSettingsItem
        {
            Name = "Gamer",
            ShipName = "Bugger",
            Weapon2Name = "rockets",
            ExtraDeviceName = "catmoflage",
            Password = "",
        };
        [Obsolete("TODO: Register various bots for known teams in gameplay modes.")]
        public const string BOTS_NAME = "The Bots";

        public PlayerSettingsItem Player1 { get; private set; }
        public PlayerSettingsItem Player2 { get; private set; }
        public bool BotsEnabled { get; set; }
        public string BotsPassword { get; set; }
        public TimeSpan TeamRebalancingInterval { get; set; }
        public string DefaultPlayerName {get; set; }

        public PlayerSettings()
        {
            InitialValues();
        }

        public void InitialValues()
        {
            Player1 = PLAYER1DEFAULT;
            Player2 = PLAYER2DEFAULT;
            BotsEnabled = true;
            BotsPassword = "";
            TeamRebalancingInterval = TimeSpan.FromSeconds(15);
            DefaultPlayerName = Player1.Name;
        }

        public void Reset(AssaultWingCore game)
        {
            InitialValues();
            UpdateFromSteam(game);
        }

        public void UpdateFromSteam(AssaultWingCore game) {
            if (!PlayerNameCustomized) {
                Player1.Name = game.Services.GetService<SteamApiService>().UserNick;
                DefaultPlayerName = Player1.Name;
            }
        }

        public bool PlayerNameCustomized {
            get {
                return DefaultPlayerName != Player1.Name;
            }
        }

        public void Validate(AssaultWingCore game)
        {
            if (CanonicalString.CanRegister) return; // cannot validate until CanonicalStrings are frozen

            UpdateFromSteam(game);

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
