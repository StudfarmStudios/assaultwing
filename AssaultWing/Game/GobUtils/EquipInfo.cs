using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    public class EquipInfo
    {
        public enum EquipInfoAmountType { Poor, Low, Average, Good, Excellent };

        #region Fields

        [TypeParameter]
        private CanonicalString _pictureName;

        [TypeParameter]
        private CanonicalString _titlePictureName;

        #endregion

        public CanonicalString PictureName { get { return _pictureName; } set { _pictureName = value; } }
        public CanonicalString TitlePictureName { get { return _titlePictureName; } set { _titlePictureName = value; } }
       
        public EquipInfo()
        {
        }
    }
}
