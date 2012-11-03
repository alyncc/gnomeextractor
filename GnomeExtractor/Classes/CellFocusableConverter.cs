using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace GnomeExtractor
{
    class CellFocusableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var header = value as string;
            for (int i = 0; i < StaticValues.FirstColumnNames.Length - 1; i++)
                if (StaticValues.FirstColumnNames[i] == header) return false;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}