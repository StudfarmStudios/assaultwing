using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    public class EquipInfo
    {
        public static readonly Color C_AMOUNT_TYPE_1 = new Color(220, 0, 0);
        public static readonly Color C_AMOUNT_TYPE_2 = new Color(255, 132, 0);
        public static readonly Color C_AMOUNT_TYPE_3 = new Color(255, 196, 0);
        public static readonly Color C_AMOUNT_TYPE_4 = new Color(152, 200, 33);
        public static readonly Color C_AMOUNT_TYPE_5 = new Color(0, 255, 0);

        public enum EquipInfoAmountType { Poor, Low, Average, High, Great };
        public enum EquipInfoUsageType { Charge, Continuous };

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


        public static Color GetColorForAmountType(EquipInfoAmountType amount)
        {
            switch (amount)
            {
                case EquipInfoAmountType.Poor:
                    return C_AMOUNT_TYPE_1;
                case EquipInfoAmountType.Low:
                    return C_AMOUNT_TYPE_2;
                case EquipInfoAmountType.Average:
                    return C_AMOUNT_TYPE_3;
                case EquipInfoAmountType.High:
                    return C_AMOUNT_TYPE_4;
                case EquipInfoAmountType.Great:
                    return C_AMOUNT_TYPE_5;
            }

            return Color.White;
        }

        public EquipInfo()
        {
        }
    }
}
