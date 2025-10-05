using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Sunday.ViewModels;

namespace Sunday.ValueConverters;

public class SideMenuIconsConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is PageType type)
        {
            return type switch
            {
                PageType.Index => "mdi home",
                PageType.Search => "mdi magnify",
                PageType.Pinned => "mdi star",
                PageType.Settings => "mdi cog",
                PageType.About => "mdi information",
                _ => ""
            };
        }
        
        return null!;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}