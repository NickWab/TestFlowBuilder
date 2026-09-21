using System.Globalization;
using System.Windows.Data;

namespace ATECore.TestFlowBuilder.Views;

public sealed class UpperCaseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.ToUpperInvariant() ?? "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
