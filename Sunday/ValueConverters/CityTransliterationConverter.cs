using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Sunday.ValueConverters;

public sealed class CityTransliterationConverter : MarkupExtension, IValueConverter
{
    static readonly (string latin, string cyr)[] Digraphs =
    {
        ("sch", "щ"),
        ("sh", "ш"),
        ("ch", "ч"),
        ("zh", "ж"),
        ("kh", "х"),
        ("ts", "ц"),
        ("th", "т"),
        ("ph", "ф"),
        ("yo", "йо"),
        ("ye", "е"),
        ("yu", "ю"),
        ("ya", "я"),
        ("ck", "к"),
        ("qu", "кв")
    };

    static readonly Dictionary<char, string> Mono = new()
    {
        ['a'] = "а", ['b'] = "б", ['c'] = "к", ['d'] = "д", ['e'] = "е", ['f'] = "ф",
        ['g'] = "г", ['h'] = "х", ['i'] = "и", ['j'] = "дж", ['k'] = "к", ['l'] = "л",
        ['m'] = "м", ['n'] = "н", ['o'] = "о", ['p'] = "п", ['q'] = "к", ['r'] = "р",
        ['s'] = "с", ['t'] = "т", ['u'] = "у", ['v'] = "в", ['w'] = "в", ['x'] = "кс",
        ['y'] = "й", ['z'] = "з"
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return s;

        var src = s;
        var lower = src.ToLowerInvariant();
        var result = new System.Text.StringBuilder();

        int i = 0;
        while (i < src.Length)
        {
            bool matched = false;

            foreach (var (latin, cyr) in Digraphs)
            {
                if (i + latin.Length <= src.Length && lower.AsSpan(i, latin.Length).SequenceEqual(latin.AsSpan()))
                {
                    var chunk = src.Substring(i, latin.Length);
                    result.Append(ApplyCase(chunk, cyr));
                    i += latin.Length;
                    matched = true;
                    break;
                }
            }

            if (matched) continue;

            char ch = src[i];
            char lch = lower[i];

            if (lch == 'c')
            {
                if (i + 1 < src.Length)
                {
                    var next = lower[i + 1];
                    var repl = (next == 'e' || next == 'i' || next == 'y') ? "с" : "к";
                    result.Append(ApplyCase(src[i].ToString(), repl));
                }
                else
                {
                    result.Append(ApplyCase(src[i].ToString(), "к"));
                }

                i++;
                continue;
            }

            if (Mono.TryGetValue(lch, out var mono))
            {
                result.Append(ApplyCase(src[i].ToString(), mono));
            }
            else
            {
                result.Append(ch);
            }

            i++;
        }

        return result.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;

    static string ApplyCase(string original, string target)
    {
        bool allUpper = true;
        for (int i = 0; i < original.Length; i++)
            if (!char.IsUpper(original[i]))
            {
                allUpper = false;
                break;
            }

        bool title = !allUpper && original.Length > 0 && char.IsUpper(original[0]);

        if (allUpper) return target.ToUpperInvariant();
        if (title)
        {
            if (target.Length == 0) return target;
            if (target.Length == 1) return target.ToUpperInvariant();
            var first = char.ToUpperInvariant(target[0]);
            var rest = target.Substring(1).ToLowerInvariant();
            return first + rest;
        }

        return target.ToLowerInvariant();
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}