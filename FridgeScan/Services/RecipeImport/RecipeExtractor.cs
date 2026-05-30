using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace FridgeScan.Services.RecipeImport;

public abstract class RecipeExtractor : IRecipeExtractor
{
    public abstract int Priority { get; }
    public abstract Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl);

    protected static readonly HttpClient HttpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36" }
        }
    };

    protected async Task<string> FetchHtmlAsync(string url)
    {
        var response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    protected static string SanitizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var decoded = WebUtility.HtmlDecode(input);
        decoded = Regex.Replace(decoded, "<.*?>", string.Empty);
        decoded = decoded
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ");
        decoded = Regex.Replace(decoded, @"\s+", " ");
        return decoded.Trim();
    }

    protected static string DecodeFractions(string text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;

        return text
            .Replace("&frac12;", "\u00BD").Replace("&#189;", "\u00BD")
            .Replace("&frac13;", "\u2153")
            .Replace("&frac14;", "\u00BC").Replace("&#188;", "\u00BC")
            .Replace("&frac15;", "\u2155")
            .Replace("&frac16;", "\u2159")
            .Replace("&frac18;", "\u215B")
            .Replace("&frac23;", "\u2154")
            .Replace("&frac34;", "\u00BE").Replace("&#190;", "\u00BE")
            .Replace("&frac38;", "\u215C")
            .Replace("&frac58;", "\u215D")
            .Replace("&frac78;", "\u215E");
    }

    protected static double ParseFraction(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0;

        input = input
            .Replace("\u00BD", "1/2").Replace("\u2153", "1/3")
            .Replace("\u00BC", "1/4").Replace("\u215B", "1/8")
            .Replace("\u2154", "2/3").Replace("\u00BE", "3/4")
            .Replace("\u215C", "3/8").Replace("\u215D", "5/8")
            .Replace("\u215E", "7/8").Replace("\u2155", "1/5")
            .Replace("\u2159", "1/6");

        if (input.Contains(' '))
        {
            var parts = input.Split(' ');
            return ParseFraction(parts[0]) + ParseFraction(parts[1]);
        }

        if (input.Contains('/'))
        {
            var parts = input.Split('/');
            if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var n) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d != 0)
                return n / d;
        }

        if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return value;

        return 0;
    }

    protected static readonly Dictionary<string, double> VolumeToMl = new()
    {
        { "cup", 240 }, { "cups", 240 },
        { "tbsp", 15 }, { "tablespoon", 15 }, { "tablespoons", 15 }, { "tbs", 15 },
        { "tsp", 5 }, { "teaspoon", 5 }, { "teaspoons", 5 },
        { "fl oz", 30 }, { "floz", 30 }
    };

    protected static readonly Dictionary<string, double> WeightToGrams = new()
    {
        { "oz", 28.35 }, { "ounce", 28.35 }, { "ounces", 28.35 },
        { "lb", 453.6 }, { "lbs", 453.6 }, { "pound", 453.6 }, { "pounds", 453.6 }
    };

    protected static string ConvertImperialToMetric(string ingredient)
    {
        if (string.IsNullOrWhiteSpace(ingredient))
            return ingredient;

        var parts = ingredient.Split(' ', 3);
        if (parts.Length < 2)
            return ingredient;

        double quantity = ParseFraction(parts[0]);
        string unit = parts[1].ToLowerInvariant();
        string rest = parts.Length > 2 ? parts[2] : "";

        if (VolumeToMl.TryGetValue(unit, out double mlFactor))
        {
            double ml = quantity * mlFactor;
            return $"{Math.Round(ml)} ml {rest}".Trim();
        }

        if (WeightToGrams.TryGetValue(unit, out double gFactor))
        {
            double grams = quantity * gFactor;
            return $"{Math.Round(grams)} g {rest}".Trim();
        }

        return ingredient;
    }

    protected static string ParseIso8601Duration(string? isoDuration)
    {
        if (string.IsNullOrEmpty(isoDuration)) return string.Empty;
        try
        {
            var duration = System.Xml.XmlConvert.ToTimeSpan(isoDuration);
            return $"{(int)duration.TotalMinutes} mins";
        }
        catch
        {
            return isoDuration;
        }
    }
}
