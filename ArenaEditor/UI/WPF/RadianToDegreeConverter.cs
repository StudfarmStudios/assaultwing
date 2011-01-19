using System;
using System.Windows.Data;
using Microsoft.Xna.Framework;

namespace AW2.UI.WPF
{
    public class RadianToDegreeConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return MathHelper.ToRadians((float)value);
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return MathHelper.ToDegrees((float)value);
        }
    }
}
