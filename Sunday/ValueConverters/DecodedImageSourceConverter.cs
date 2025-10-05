using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Sunday.ValueConverters;

public sealed class DateToDayOfWeekConverter : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryGetDate(value, out var dt)) return value;

        var fmt = "long";
        var casing = "lower";
        var useCulture = culture;

        if (parameter is string p && p.Length > 0)
        {
            var tokens = p.Split(new[] { ',', '|', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant());
            foreach (var t in tokens)
            {
                if (t is "short" or "abbr") fmt = "short";
                else if (t is "long" or "full") fmt = "long";
                else if (t is "l" or "lower") casing = "lower";
                else if (t is "u" or "upper") casing = "upper";
                else if (t is "t" or "title") casing = "title";
                else
                {
                    try
                    {
                        useCulture = CultureInfo.GetCultureInfo(t);
                    }
                    catch
                    {
                    }
                }
            }
        }

        string text = fmt == "short"
            ? useCulture.DateTimeFormat.AbbreviatedDayNames[(int)dt.DayOfWeek]
            : useCulture.DateTimeFormat.GetDayName(dt.DayOfWeek);

        return ApplyCase(text, casing, useCulture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;

    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    static bool TryGetDate(object value, out DateTime dt)
    {
        if (value is DateTime d)
        {
            dt = d;
            return true;
        }

        if (value is DateTimeOffset o)
        {
            dt = o.DateTime;
            return true;
        }

        if (value is string s && DateTime.TryParse(s, out var p))
        {
            dt = p;
            return true;
        }

        dt = default;
        return false;
    }

    static string ApplyCase(string text, string casing, CultureInfo culture)
    {
        return casing switch
        {
            "upper" => text.ToUpper(culture),
            "title" => text.Length == 0 ? text : char.ToUpper(text[0], culture) + text.Substring(1).ToLower(culture),
            _ => text.ToLower(culture)
        };
    }
}

public class DecodedImageSourceConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!string.IsNullOrEmpty(value?.ToString()))
        {
            try
            {
                int decodeFactor = System.Convert.ToInt32(parameter);
                using var stream = File.OpenRead(value.ToString());
                var imageSource =
                    Bitmap.DecodeToWidth(stream, decodeFactor, BitmapInterpolationMode.LowQuality);
                return imageSource;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return null!;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public class AsyncTask : INotifyPropertyChanged
    {
        public AsyncTask(Func<object> valueFunc)
        {
            AsyncValue = "loading async value"; //temp value for demo
            LoadValue(valueFunc);
        }

        private async Task LoadValue(Func<object> valueFunc)
        {
            AsyncValue = await Task<object>.Run(() => { return valueFunc(); });
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("AsyncValue"));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public object AsyncValue { get; set; }
    }
}