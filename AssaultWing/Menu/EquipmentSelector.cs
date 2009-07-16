using System.Collections.Generic;
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
        int currentValue;

        /// <summary>
        /// Index of the currently selected value in the list of possible values.
        /// </summary>
        public int CurrentValue
        {
            get { return currentValue; }
            set
            {
                currentValue = value.Modulo(Values.Count);
                SetValue(Values[currentValue]);
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
        /// Creates an equipment selector for a player with a set of possible values.
        /// </summary>
        /// <param name="player">The player.</param>
        public EquipmentSelector(Player player)
        {
            Values = new List<string>();
            Player = player;
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
        public ShipSelector(Player player)
            : base(player)
        {
            Values = AssaultWing.Instance.DataEngine.GameplayMode.ShipTypes;

            // Find the value the player currently has.
            CurrentValue = Values.IndexOf(player.ShipName);
        }

        /// <summary>
        /// Selects the ship of the player.
        /// </summary>
        /// <param name="value">The name of the ship.</param>
        protected override void SetValue(string value)
        {
            Player.ShipName = (CanonicalString)value;
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
        public Weapon1Selector(Player player)
            : base(player)
        {
            // Find possible values.
            Values = AssaultWing.Instance.DataEngine.GameplayMode.Weapon1Types;

            // Find the value the player currently has.
            CurrentValue = Values.IndexOf(player.Weapon1Name);
        }

        /// <summary>
        /// Selects the primary weapon of the player.
        /// </summary>
        /// <param name="value">The name of the primary weapon.</param>
        protected override void SetValue(string value)
        {
            Player.Weapon1Name = (CanonicalString)value;
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
        public Weapon2Selector(Player player)
            : base(player)
        {
            // Find possible values.
            Values = AssaultWing.Instance.DataEngine.GameplayMode.Weapon2Types;

            // Find the value the player currently has.
            CurrentValue = Values.IndexOf(player.Weapon2Name);
        }

        /// <summary>
        /// Selects the secondary weapon of the player.
        /// </summary>
        /// <param name="value">The name of the secondary weapon.</param>
        protected override void SetValue(string value)
        {
            Player.Weapon2Name = (CanonicalString)value;
        }
    }
}
