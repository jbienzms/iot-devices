using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace SolarSystem
{
    public class OrbitConverter : IValueConverter
    {
        private const double OUTERMOST = 40d; // Really should be adaptive
        private const double DIAMETER = 770d; // Really should be adaptive too

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double orbit = (double)value;
            double factor = (parameter != null ? System.Convert.ToDouble(parameter) : 1);
            //orbit = orbit / 150; // Convert to AU
            //orbit = orbit * factor;
            // double result = (Math.Pow(orbit / OUTERMOST, 0.4) * DIAMETER) * factor;
            double result = ((orbit / OUTERMOST) * DIAMETER) * factor;

            if (targetType == typeof(Thickness))
            {
                return new Thickness((int)result, 0, 0, 0);
            }
            else
            {
                return result;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
