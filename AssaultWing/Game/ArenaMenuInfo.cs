using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers;
using AW2.Helpers.Serialization;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Game
{
    [LimitedSerialization]
    public class ArenaMenuInfo
    {
        public static readonly Color C_LEVEL_1 = new Color(220, 0, 0);
        public static readonly Color C_LEVEL_2 = new Color(255, 132, 0);
        public static readonly Color C_LEVEL_3 = new Color(255, 196, 0);
        public static readonly Color C_LEVEL_4 = new Color(152, 200, 33);
        public static readonly Color C_LEVEL_5 = new Color(0, 255, 0);
        
        public enum ArenaSize { Tiny, Small, Medium, Large, Huge }
        public enum ArenaBonusAmount { None, Little, Average, Good, Crazy }
        public enum ArenaFlightEasiness { Deadly, Hard, Average, Easy, Stoner }
        
        #region Fields

        [TypeParameter]
        private string _docks;

        [TypeParameter]
        private CanonicalString _previewName;

        [TypeParameter]
        private string _infoText;

        [TypeParameter]
        private string _idealPlayers;

        [TypeParameter]
        private ArenaSize _size;

        [TypeParameter]
        private ArenaBonusAmount _bonusAmount;

        [TypeParameter]
        private ArenaFlightEasiness _flightEasiness;

        #endregion

        public string Name { get; set; }
        public string Docks { get { return _docks; } set { _docks = value; } }
        public CanonicalString PreviewName { get { return _previewName; } set { _previewName = value; } }
        public string InfoText { get { return _infoText; } set { _infoText = value; } }
        public string IdealPlayers { get { return _idealPlayers; } set { _idealPlayers = value; } }
        public ArenaSize Size { get { return _size; } set { _size = value; } }
        public ArenaBonusAmount BonusAmount { get { return _bonusAmount; } set { _bonusAmount = value; } }
        public ArenaFlightEasiness FlightEasiness { get { return _flightEasiness; } set { _flightEasiness = value; } }

        public static Color GetColorForSize(ArenaSize size)
        {
            switch (size)
            {
                case ArenaSize.Tiny:
                    return C_LEVEL_1;
                case ArenaSize.Small:
                    return C_LEVEL_2;
                case ArenaSize.Medium:
                    return C_LEVEL_3;
                case ArenaSize.Large:
                    return C_LEVEL_4;
                case ArenaSize.Huge:
                    return C_LEVEL_5;
            }

            return Color.White;
        }

        public static Color GetColorForBonusAmount(ArenaBonusAmount amount)
        {
            switch (amount)
            {
                case ArenaBonusAmount.None:
                    return C_LEVEL_1;
                case ArenaBonusAmount.Little:
                    return C_LEVEL_2;
                case ArenaBonusAmount.Average:
                    return C_LEVEL_3;
                case ArenaBonusAmount.Good:
                    return C_LEVEL_4;
                case ArenaBonusAmount.Crazy:
                    return C_LEVEL_5;
            }

            return Color.White;
        }

        public static Color GetColorForFlightEasiness(ArenaFlightEasiness easiness)
        {
            switch (easiness)
            {
                case ArenaFlightEasiness.Deadly:
                    return C_LEVEL_1;
                case ArenaFlightEasiness.Hard:
                    return C_LEVEL_2;
                case ArenaFlightEasiness.Average:
                    return C_LEVEL_3;
                case ArenaFlightEasiness.Easy:
                    return C_LEVEL_4;
                case ArenaFlightEasiness.Stoner:
                    return C_LEVEL_5;
            }

            return Color.White;
        }
    }
}
