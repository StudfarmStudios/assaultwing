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
        private int _currentValue;

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
        protected EquipmentSelector(Player player)
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

    public class ShipSelector : EquipmentSelector
    {
        public ShipSelector(Player player)
            : base(player)
        {
            Values = AssaultWing.Instance.DataEngine.GameplayMode.ShipTypes;
            CurrentValue = Values.IndexOf(player.ShipName);
        }

        protected override void SetValue(string value)
        {
            Player.ShipName = (CanonicalString)value;
        }
    }

    public class ExtraDeviceSelector : EquipmentSelector
    {
        public ExtraDeviceSelector(Player player)
            : base(player)
        {
            Values = AssaultWing.Instance.DataEngine.GameplayMode.ExtraDeviceTypes;
            CurrentValue = Values.IndexOf(player.ExtraDeviceName);
        }

        protected override void SetValue(string value)
        {
            Player.ExtraDeviceName = (CanonicalString)value;
        }
    }

    public class Weapon2Selector : EquipmentSelector
    {
        public Weapon2Selector(Player player)
            : base(player)
        {
            Values = AssaultWing.Instance.DataEngine.GameplayMode.Weapon2Types;
            CurrentValue = Values.IndexOf(player.Weapon2Name);
        }

        protected override void SetValue(string value)
        {
            Player.Weapon2Name = (CanonicalString)value;
        }
    }
}
