using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace HyperlinkingPDFsWithUI
{
    [ValueConversion(typeof(string), typeof(BitmapImage))]
    class ConnectedHeaderToImageConverter : IValueConverter
    {
        /// <summary>
        /// Create a static instance to reference.
        /// </summary>
        public static ConnectedHeaderToImageConverter Instance = new ConnectedHeaderToImageConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string image = @"pack://application:,,,/";

            // Cast object to bool and return the corresponding image.
            if ((bool)value)
            {
                image += @"Images/GreenCheck.jpg";
                return new BitmapImage(new Uri(image));
            }
            else
            {
                image += @"Images/RedX.png";
                return new BitmapImage(new Uri(image));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
