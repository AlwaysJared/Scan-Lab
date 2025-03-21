using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Client.Converters
{
    public static class BooleanConverters
    {
        public static readonly IValueConverter InvertedBooleanConverter =
            new FuncValueConverter<bool, bool>(b => !b);
    }
}
