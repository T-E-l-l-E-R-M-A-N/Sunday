using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;

namespace Sunday.ValueConverters;

public class WeatherTextToWallConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var template =  value.ToString().ToLower() switch
        {
            "ясно" => App.Current.FindResource("ClearSkyWall") as DataTemplate,
            "переменная облачность" => App.Current.FindResource("ClearSkyWall") as DataTemplate,
            _ => App.Current.FindResource("ClearSkyWall") as DataTemplate
        };

        return template;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}