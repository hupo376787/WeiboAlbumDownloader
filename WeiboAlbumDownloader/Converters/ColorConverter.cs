using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WeiboAlbumDownloader.Enums;

namespace WeiboAlbumDownloader.Converters
{
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = (MessageEnum)value;
            if (type == MessageEnum.Error)
                return new SolidColorBrush(Colors.Red);
            else if (type == MessageEnum.Warning)
                return new SolidColorBrush(Colors.Yellow);
            else if (type == MessageEnum.Success)
                return new SolidColorBrush(Colors.Green);

            return new SolidColorBrush(Colors.Orange);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
