using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ILEdit.Converters
{
    public class AllTrueMultiConverter : System.Windows.Data.IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return values.OfType<bool>().All(x => x);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
