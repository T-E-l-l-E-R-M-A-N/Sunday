using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Sunday.ValueConverters;

public class DateToShortFormatConverter : MarkupExtension, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            var date = DateTime.Parse(s);
            // Получаем сокращённое название месяца с маленькой буквы
            string month = culture.DateTimeFormat.GetAbbreviatedMonthName(date.Month).ToUpper().Remove(3);
            return $"{date.Day} {month}";
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Обратное преобразование не требуется
        throw new NotSupportedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}