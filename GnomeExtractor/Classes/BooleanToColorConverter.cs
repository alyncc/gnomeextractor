using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace GnomeExtractor
{
    public class CellBackgroundColorConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var allowedSkills1 = values[0].ToString();
            var allowedSkills2 = values[1].ToString();
            var header = values[2].ToString();
            
            var index = 0;
            //var check = -1;

            foreach (var item in Globals.SkillNamesProfessions1)
            {
                if (item == header)
                {
                    //if (allowedSkills1 == "0") return Brushes.White;
                    if (allowedSkills1[index] == '1') return Brushes.GreenYellow;
                    break;
                }
                index++;
            }

            index = 0;
            foreach (var item in Globals.SkillNamesProfessions2)
            {
                if (item == header)
                {
                    //if (allowedSkills2 == "0") return Brushes.White;
                    if (allowedSkills2[index + 8] == '1') return Brushes.GreenYellow;
                    break;
                }
                index++;
            }
            return Brushes.White;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
