using System.Globalization;
using System.Windows.Data;

namespace Brain.Desktop.Converters;

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public class InverseBoolExtension
{
    public static InverseBoolConverter Instance { get; } = new();
}
