using System;
using AW2.Game;
using AW2.Helpers;

namespace AW2.Menu
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
        string[] values;
        int currentValue;

        /// <summary>
        /// Index of the currently selected value in .
        /// </summary>
        public int CurrentValue
        {
            get { return currentValue; }
            set
            {
                currentValue = value.Modulo(values.Length);
                SetValue(values[currentValue]);
            }
        }

        /// <summary>
        /// The player whose equipment this selector is selecting.
        /// </summary>
        protected Player Player { get; private set; }

        /// <summary>
        /// Creates an equipment selector for a player with a set of possible values.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="values">The set of possible values.</param>
        public EquipmentSelector(Player player, string[] values)
        {
            Player = player;
            this.values = values;
        }

        /// <summary>
        /// Sets a value to the aspect.
        /// </summary>
        /// <param name="value">The value to set.</param>
        protected abstract void SetValue(string value);
    }

    /// <summary>
    /// Selector of the ship of a player.
    /// </summary>
    public class ShipSelector : EquipmentSelector
    {
        /// <summary>
        /// Creates a ship selector.
        /// </summary>
        /// <param name="player">The player whose ship we are selecting.</param>
        /// <param name="values">The possible values to select from.</param>
        public ShipSelector(Player player, string[] values)
            : base(player, values)
        {
            int currentI = 0;
            for (int i = 0; i < values.Length; ++i)
                if (values[i] == player.ShipName)
                {
                    currentI = i;
                    break;
                }
            CurrentValue = currentI;
        }

        /// <summary>
        /// Selects the ship of the player.
        /// </summary>
        /// <param name="value">The name of the ship.</param>
        protected override void SetValue(string value)
        {
            Player.ShipName = value;
        }
    }

    /// <summary>
    /// Selector of the primary weapon of a player.
    /// </summary>
    public class Weapon1Selector : EquipmentSelector
    {
        /// <summary>
        /// Creates a primary weapon selector.
        /// </summary>
        /// <param name="player">The player whose primary weapon we are selecting.</param>
        /// <param name="values">The possible values to select from.</param>
        public Weapon1Selector(Player player, string[] values)
            : base(player, values)
        {
            int currentI = 0;
            for (int i = 0; i < values.Length; ++i)
                if (values[i] == player.Weapon1Name)
                {
                    currentI = i;
                    break;
                }
            CurrentValue = currentI;
        }

        /// <summary>
        /// Selects the primary weapon of the player.
        /// </summary>
        /// <param name="value">The name of the primary weapon.</param>
        protected override void SetValue(string value)
        {
            Player.Weapon1Name = value;
        }
    }

    /// <summary>
    /// Selector of the secondary weapon of a player.
    /// </summary>
    public class Weapon2Selector : EquipmentSelector
    {
        /// <summary>
        /// Creates a secondary weapon selector.
        /// </summary>
        /// <param name="player">The player whose secondary weapon we are selecting.</param>
        /// <param name="values">The possible values to select from.</param>
        public Weapon2Selector(Player player, string[] values)
            : base(player, values)
        {
            int currentI = 0;
            for (int i = 0; i < values.Length; ++i)
                if (values[i] == player.Weapon2Name)
                {
                    currentI = i;
                    break;
                }
            CurrentValue = currentI;
        }

        /// <summary>
        /// Selects the secondary weapon of the player.
        /// </summary>
        /// <param name="value">The name of the secondary weapon.</param>
        protected override void SetValue(string value)
        {
            Player.Weapon2Name = value;
        }
    }
}
