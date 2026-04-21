using System.Globalization;

namespace TourGuideHCM.App.Converters;

/// <summary>
/// Converter trả về true nếu string KHÔNG rỗng.
/// Dùng với IsVisible khi muốn ẩn element khi data rỗng.
/// </summary>
public class NotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s) return !string.IsNullOrWhiteSpace(s);
        return value != null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
