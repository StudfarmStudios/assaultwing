using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Game;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Game.Players;
using AW2.Helpers;
using AW2.Settings;

namespace AW2.Menu.Equip
{
    /// <summary>
    /// Selector of some aspect of a player's equipment.
    /// Helper class for <see cref="EquipMenuComponent"/>.
    /// </summary>
    /// Each player has his own selectors. Each selector controls one
    /// aspect of a player such as his ship or primary weapon. A selector
    /// responds to commands to change the value that is selected for
    /// the aspect.
    public abstract class EquipmentSelector
    {
        private int _currentValue;

        /// <summary>
        /// Position of the selector's top left corner in menu system coordinates.
        /// </summary>
        protected Vector2 Pos { get; private set; }
        protected PlayerSettingsItem Settings { get; private set; }
        protected AssaultWingCore Game { get; private set; }

        /// <summary>
        /// Index of the currently selected value in the list of possible values.
        /// </summary>
        public int CurrentValue
        {
            get { return _currentValue; }
            set
            {
                _currentValue = value.Modulo(Values.Count);
                SetValue(Values[_currentValue]);
            }
        }

        /// <summary>
        /// The possible values for the aspect.
        /// </summary>
        protected IList<string> Values { get; set; }

        /// <summary>
        /// The player whose equipment this selector is selecting.
        /// </summary>
        protected Player Player { get; private set; }

        /// <summary>
        /// Creates an equipment selector for a player with an empty set of possible values.
        /// Subclasses should set the list of values.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="pos">Top left corner of the selector in menu system coordinates</param>
        protected EquipmentSelector(AssaultWingCore game, Player player, PlayerSettingsItem settings, Vector2 pos)
        {
            Values = new List<string>();
            Game = game;
            Player = player;
            Settings = settings;
            Pos = pos;
        }

        public abstract void Draw(Vector2 view, SpriteBatch spriteBatch);

        /// <summary>
        /// Sets a value to the aspect.
        /// </summary>
        /// <param name="value">The value to set.</param>
        protected abstract void SetValue(string value);
    }

    public class ShipSelector : EquipmentSelector
    {
        public ShipSelector(AssaultWingCore game, Player player, PlayerSettingsItem settings, Vector2 pos)
            : base(game, player, settings, pos)
        {
            Values = game.DataEngine.GameplayMode.ShipTypes;
            CurrentValue = Values.IndexOf(player.ShipName);
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            var ship = (Ship)Game.DataEngine.GetTypeTemplate(Player.ShipName);
            var shipTexture = Game.Content.Load<Texture2D>(ship.ShipInfo.IconEquipName);
            spriteBatch.Draw(shipTexture, Pos - view, Color.White);
        }

        protected override void SetValue(string value)
        {
            Player.ShipName = (CanonicalString)value;
            Settings.ShipName = value;
        }
    }

    public class ExtraDeviceSelector : EquipmentSelector
    {
        public ExtraDeviceSelector(AssaultWingCore game, Player player, PlayerSettingsItem settings, Vector2 pos)
            : base(game, player, settings, pos)
        {
            Values = game.DataEngine.GameplayMode.ExtraDeviceTypes;
            CurrentValue = Values.IndexOf(player.ExtraDeviceName);
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            var extraDevice = (ShipDevice)Game.DataEngine.GetTypeTemplate(Player.ExtraDeviceName);
            var extraDeviceTexture = Game.Content.Load<Texture2D>(extraDevice.DeviceInfo.IconEquipName);
            spriteBatch.Draw(extraDeviceTexture, Pos - view, Color.White);
        }

        protected override void SetValue(string value)
        {
            Player.ExtraDeviceName = (CanonicalString)value;
            Settings.ExtraDeviceName = value;
        }
    }

    public class Weapon2Selector : EquipmentSelector
    {
        public Weapon2Selector(AssaultWingCore game, Player player, PlayerSettingsItem settings, Vector2 pos)
            : base(game, player, settings, pos)
        {
            Values = game.DataEngine.GameplayMode.Weapon2Types;
            CurrentValue = Values.IndexOf(player.Weapon2Name);
        }

        public override void Draw(Vector2 view, SpriteBatch spriteBatch)
        {
            var weapon2 = (Weapon)Game.DataEngine.GetTypeTemplate(Player.Weapon2Name);
            var weapon2Texture = Game.Content.Load<Texture2D>(weapon2.DeviceInfo.IconEquipName);
            spriteBatch.Draw(weapon2Texture, Pos - view, Color.White);
        }

        protected override void SetValue(string value)
        {
            Player.Weapon2Name = (CanonicalString)value;
            Settings.Weapon2Name = value;
        }
    }
}
