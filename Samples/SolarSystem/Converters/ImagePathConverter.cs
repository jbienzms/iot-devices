using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolarSystem.Model;
using Windows.UI.Xaml.Data;

namespace SolarSystem
{
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool thumb = string.Equals(parameter, "thumb");

            var body = value as CelestialBody;

            if (body == null) { return null; }

            if (thumb == true)
            {
                return string.Format("Assets/Thumbs/{0}.png", body.BodyName);
            }
            else
            {
                return string.Format("Assets/Images/{0}.png", body.BodyName);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
