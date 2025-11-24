using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Admin.Converters
{
    public class HalfWidthConverter : IValueConverter
    {
        public static readonly HalfWidthConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
                return d / 2;
            return 400; // default fallback
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
