using System.Globalization;
using System.Text.RegularExpressions;

namespace FridgeScan.Services.RecipeImport;

public class RecipeIngredientParser : IRecipeIngredientParser
{
    private static readonly HashSet<string> KnownUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "g", "kg", "mg",
        "ml", "l", "cl", "dl",
        "oz", "lb", "lbs", "ounce", "ounces", "pound", "pounds",
        "cup", "cups",
        "tbsp", "tablespoon", "tablespoons", "tbs",
        "tsp", "teaspoon", "teaspoons",
        "fl oz", "floz",
        "pcs", "piece", "pieces",
        "pinch", "pinches",
        "handful", "handfuls",
        "bunch", "bunches",
        "clove", "cloves",
        "sprig", "sprigs",
        "slice", "slices",
        "cm", "mm",
        "can", "cans", "tin", "tins",
        "pack", "packet",
    };

    private static readonly Regex AffixedQuantityRegex = new(
        @"^([\d.\s/½⅓¼⅛⅔¾⅜⅝⅞⅕⅙]+)\s*([a-zA-Z]+)$",
        RegexOptions.Compiled);

    private static readonly Regex LeadingQuantityRegex = new(
        @"^([\d.,]+\s*(?:[½⅓¼⅛⅔¾⅜⅝⅞⅕⅙]|\d+/\d+)?)\s+(.*)",
        RegexOptions.Compiled);

    public List<ParsedIngredient> Parse(List<string> rawIngredients)
    {
        var results = new List<ParsedIngredient>();

        foreach (var raw in rawIngredients)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var ingredient = raw.Trim();
            ingredient = ingredient.Replace(',', '.');

            var parsed = new ParsedIngredient();
            var remaining = TryExtractQuantityAndUnit(ingredient, parsed);
            parsed.Name = remaining;

            results.Add(parsed);
        }

        return results;
    }

    private string TryExtractQuantityAndUnit(string input, ParsedIngredient parsed)
    {
        var affixMatch = AffixedQuantityRegex.Match(input);
        if (affixMatch.Success)
        {
            var qtyStr = affixMatch.Groups[1].Value;
            var unitStr = affixMatch.Groups[2].Value.ToLowerInvariant();

            if (KnownUnits.Contains(unitStr))
            {
                parsed.Quantity = ParseFraction(qtyStr);
                parsed.Unit = unitStr;
                return input[(affixMatch.Length)..].Trim();
            }
        }

        var leadMatch = LeadingQuantityRegex.Match(input);
        if (leadMatch.Success)
        {
            var qtyStr = leadMatch.Groups[1].Value;
            var rest = leadMatch.Groups[2].Value;

            foreach (var unit in KnownUnits.OrderByDescending(u => u.Length))
            {
                if (rest.StartsWith(unit, StringComparison.OrdinalIgnoreCase))
                {
                    var afterUnit = rest[unit.Length..];
                    if (afterUnit.Length == 0 || char.IsWhiteSpace(afterUnit[0]))
                    {
                        parsed.Quantity = ParseFraction(qtyStr);
                        parsed.Unit = unit.ToLowerInvariant();
                        return afterUnit.Trim();
                    }
                }
            }
        }

        return input;
    }

    private static float? ParseFraction(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var normalized = input
            .Replace("½", "1/2").Replace("⅓", "1/3")
            .Replace("¼", "1/4").Replace("⅛", "1/8")
            .Replace("⅔", "2/3").Replace("¾", "3/4")
            .Replace("⅜", "3/8").Replace("⅝", "5/8")
            .Replace("⅞", "7/8").Replace("⅕", "1/5")
            .Replace("⅙", "1/6");

        var spaceIdx = normalized.IndexOf(' ');
        if (spaceIdx > 0)
        {
            var whole = ParseFraction(normalized[..spaceIdx]);
            var frac = ParseFraction(normalized[(spaceIdx + 1)..]);
            return (whole ?? 0) + (frac ?? 0);
        }

        var slashIdx = normalized.IndexOf('/');
        if (slashIdx > 0)
        {
            if (float.TryParse(normalized[..slashIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var n) &&
                float.TryParse(normalized[(slashIdx + 1)..], NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d != 0)
                return n / d;
        }

        if (float.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return value;

        return null;
    }
}
