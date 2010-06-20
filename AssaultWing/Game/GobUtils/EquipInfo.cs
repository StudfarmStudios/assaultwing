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
        public enum EquipInfoAmountType { Poor, Low, Average, High, Great };

        #region Fields

        [TypeParameter]
        private CanonicalString _pictureName;

        [TypeParameter]
        private CanonicalString _titlePictureName;

        [TypeParameter]
        private string _infoText;

        #endregion

        public CanonicalString PictureName { get { return _pictureName; } set { _pictureName = value; } }
        public CanonicalString TitlePictureName { get { return _titlePictureName; } set { _titlePictureName = value; } }
        public string InfoText { get { return _infoText; } set { _infoText = value; } }


        public static Color GetColorForAmountType(EquipInfoAmountType amount)
        {
            switch (amount)
            {
                case EquipInfoAmountType.Poor:
                    return new Color(220, 0, 0);
                case EquipInfoAmountType.Low:
                    return new Color(255, 132, 0);
                case EquipInfoAmountType.Average:
                    return new Color(255, 196, 0);
                case EquipInfoAmountType.High:
                    return new Color(152, 200, 33);
                case EquipInfoAmountType.Great:
                    return new Color(0, 255, 0);
            }

            return Color.White;
        }

        public EquipInfo()
        {
        }
    }
}
