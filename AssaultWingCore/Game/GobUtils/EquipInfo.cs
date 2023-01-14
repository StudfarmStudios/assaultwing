using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.GobUtils
{
    public class EquipInfo
    {
        private static readonly Color C_AMOUNT_TYPE_POOR = new Color(220, 0, 0);
        private static readonly Color C_AMOUNT_TYPE_LOW = new Color(255, 132, 0);
        private static readonly Color C_AMOUNT_TYPE_AVERAGE = new Color(255, 196, 0);
        private static readonly Color C_AMOUNT_TYPE_HIGH = new Color(152, 200, 33);
        private static readonly Color C_AMOUNT_TYPE_GREAT = new Color(0, 255, 0);

        private static readonly Color C_USAGE_TYPE_SINGLE = Color.Aqua;
        private static readonly Color C_USAGE_TYPE_CONTINUOUS = Color.Aqua;

        public enum EquipInfoAmountType { Poor, Low, Average, High, Great };
        public enum EquipInfoUsageType { Single, Continuous };

        #region Fields

        /// <summary>
        /// Name of the icon in the equip menu main display.
        /// </summary>
        [TypeParameter]
        private CanonicalString _iconEquipName;

        [TypeParameter]
        private CanonicalString _pictureName;

        [TypeParameter]
        private CanonicalString _titlePictureName;

        [TypeParameter]
        private string _infoText;

        #endregion

        /// <summary>
        /// Name of the icon in the equip menu main display.
        /// </summary>
        public CanonicalString IconEquipName { get { return _iconEquipName; } set { _iconEquipName = value; } }
        public CanonicalString PictureName { get { return _pictureName; } set { _pictureName = value; } }
        public CanonicalString TitlePictureName { get { return _titlePictureName; } set { _titlePictureName = value; } }
        public string InfoText { get { return _infoText; } set { _infoText = value; } }

        public static Color GetColor(Enum value)
        {
            if (value is EquipInfoUsageType) return GetColorForUsageType((EquipInfoUsageType)value);
            if (value is EquipInfoAmountType) return GetColorForAmountType((EquipInfoAmountType)value);
            throw new ApplicationException("Unexpected Enum type " + value.GetType());
        }

        public static Color GetColorForUsageType(EquipInfoUsageType usage)
        {
            switch (usage)
            {
                default: throw new ApplicationException("Unexpected EquipInfoUsageType " + usage);
                case EquipInfoUsageType.Single: return C_USAGE_TYPE_SINGLE;
                case EquipInfoUsageType.Continuous: return C_USAGE_TYPE_CONTINUOUS;
            }
        }

        public static Color GetColorForAmountType(EquipInfoAmountType amount)
        {
            switch (amount)
            {
                default: throw new ApplicationException("Unexpected EquipInfoAmountType " + amount);
                case EquipInfoAmountType.Poor: return C_AMOUNT_TYPE_POOR;
                case EquipInfoAmountType.Low: return C_AMOUNT_TYPE_LOW;
                case EquipInfoAmountType.Average: return C_AMOUNT_TYPE_AVERAGE;
                case EquipInfoAmountType.High: return C_AMOUNT_TYPE_HIGH;
                case EquipInfoAmountType.Great: return C_AMOUNT_TYPE_GREAT;
            }
        }

        public static Color GetReversedColorForAmountType(EquipInfoAmountType amount)
        {
            switch (amount)
            {
                default: throw new ApplicationException("Unexpected EquipInfoAmountType " + amount);
                case EquipInfoAmountType.Poor: return C_AMOUNT_TYPE_GREAT;
                case EquipInfoAmountType.Low: return C_AMOUNT_TYPE_HIGH;
                case EquipInfoAmountType.Average: return C_AMOUNT_TYPE_AVERAGE;
                case EquipInfoAmountType.High: return C_AMOUNT_TYPE_LOW;
                case EquipInfoAmountType.Great: return C_AMOUNT_TYPE_POOR;
            }
        }

        public EquipInfo()
        {
        }
    }
}
