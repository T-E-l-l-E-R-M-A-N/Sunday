using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Sunday.ValueConverters;

public class TimeToHourTextConverter : MarkupExtension, IValueConverter
{
    public string AssumeKind { get; set; } = "auto";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
            return string.Empty;

        // Параметр конвертера имеет приоритет над AssumeKind
        var mode = (parameter as string)?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(mode))
            mode = AssumeKind?.Trim().ToLowerInvariant();

        // 1) Сначала пытаемся распарсить как DateTimeOffset (лучше сохраняет смещение, если оно есть)
        if (DateTimeOffset.TryParse(s, culture, DateTimeStyles.AllowWhiteSpaces, out var dto))
        {
            var local = dto.ToLocalTime();
            return $"{local.Hour} ч";
        }

        // 2) Если не получилось — пробуем DateTime, указав предполагаемую зону для строк без смещения
        var styles = DateTimeStyles.AllowWhiteSpaces;

        // По умолчанию (auto) считаем безсмещённые строки как UTC,
        // чтобы не "сдвигать" их дважды в неизвестную сторону.
        if (mode == "local")
            styles |= DateTimeStyles.AssumeLocal;
        else
            styles |= DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

        if (DateTime.TryParse(s, culture, styles, out var dt))
        {
            // Если получили Utc — переводим в локальное
            DateTime localDt = dt.Kind switch
            {
                DateTimeKind.Utc => dt.ToLocalTime(),
                DateTimeKind.Local => dt,
                _ => (mode == "local") ? DateTime.SpecifyKind(dt, DateTimeKind.Local)
                    : DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime()
            };

            return $"{localDt.Hour} ч";
        }

        // Если распарсить не удалось — возвращаем исходное значение как есть
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}