using System.Globalization;
using System.Text.RegularExpressions;

namespace FridgeScan.Services.RecipeImport;

public class RecipeIngredientParser : IRecipeIngredientParser
{
    private static readonly HashSet<string> KnownUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "g", "kg", "mg", "grams", "gram", "kilograms", "kilogram",
        "ml", "l", "cl", "dl",
        "milliliters", "milliliter", "millilitres", "millilitre",
        "liters", "liter", "litres", "litre",
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
        "pack", "packet", "packets",
        "stalk", "stalks",
        "stick", "sticks",
        "medium", "large", "small",
        // Italian units (common in recipes)
        "pizzico", "pizzichi",
        "cucchiaio", "cucchiai",
        "cucchiaino", "cucchiaini",
        "bicchiere", "bicchieri",
        "fetta", "fette",
        "spicchio", "spicchi",
        "mazzo", "mazzi",
        "rametto", "rametti",
        "lattina", "lattine",
        "confezione", "confezioni",
        "pezzo", "pezzi",
        "costa", "coste",
        "goccio", "gocci",
        "noce",
        "etto", "etti",
    };

    private static readonly Regex AffixedQuantityRegex = new(
        @"^([\d.\s/½⅓¼⅛⅔¾⅜⅝⅞⅕⅙]+)\s*([a-zA-Z]+)$",
        RegexOptions.Compiled);

    /// <summary>
    /// Matches leading quantity patterns:
    /// - Integer or decimal: "2", "1.5"
    /// - Simple fraction: "1/2"
    /// - Mixed number: "1 1/2"
    /// - Unicode fraction: "½", "1½", "1 ½"
    /// All followed by whitespace then the rest of the ingredient.
    /// </summary>
    private static readonly Regex LeadingQuantityRegex = new(
        @"^((?:(?:[-\d,]+(?:\s+\d+/\d+)?|[\d,]+/[\d,]+)(?:\.\d+)?|[.,\d]*\s*[½⅓¼⅛⅔¾⅜⅝⅞⅕⅙]))\s+(.*)",
        RegexOptions.Compiled);

    /// <summary>
    /// Matches a leading parenthetical note — e.g. "(1 and 1/2 cups)" or "(approximately 1 large)".
    /// </summary>
    private static readonly Regex LeadingParentheticalRegex = new(
        @"^\s*\([^)]*\)\s*",
        RegexOptions.Compiled);

    public List<ParsedIngredient> Parse(List<string> rawIngredients)
    {
        var results = new List<ParsedIngredient>();

        foreach (var raw in rawIngredients)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var ingredient = raw.Trim();

            var parsed = new ParsedIngredient { Original = ingredient };
            var remaining = TryExtractQuantityAndUnit(ingredient, parsed);
            parsed.Name = StripLeadingParenthetical(remaining, parsed);

            results.Add(parsed);
        }

        return results;
    }

    private string TryExtractQuantityAndUnit(string input, ParsedIngredient parsed)
    {
        // 1. Try affixed format: "200g", "250ml" (qty+unit at string end)
        var affixMatch = AffixedQuantityRegex.Match(input);
        if (affixMatch.Success)
        {
            var qtyStr = affixMatch.Groups[1].Value;
            var unitStr = affixMatch.Groups[2].Value.ToLowerInvariant();

            if (KnownUnits.Contains(unitStr))
            {
                parsed.Quantity = ParseFraction(NormalizeQuantity(qtyStr));
                parsed.Unit = unitStr;
                return input[(affixMatch.Length)..].Trim();
            }
        }

        // 2. Try leading format: "2 cups flour", "1/2 tsp salt", "½ lemon" (qty [unit] name)
        var leadMatch = LeadingQuantityRegex.Match(input);
        if (leadMatch.Success)
        {
            var qtyStr = leadMatch.Groups[1].Value;
            var rest = leadMatch.Groups[2].Value;
            var parsedQty = ParseFraction(NormalizeQuantity(qtyStr));

            if (!parsedQty.HasValue)
            {
                // Quantity didn't parse — don't use this match
                // (e.g. "1-2" with hyphen might fail; rest will be tried by trailing)
            }
            else
            {
                foreach (var unit in KnownUnits.OrderByDescending(u => u.Length))
                {
                    if (rest.StartsWith(unit, StringComparison.OrdinalIgnoreCase))
                    {
                        var afterUnit = rest[unit.Length..];
                        if (afterUnit.Length == 0 || char.IsWhiteSpace(afterUnit[0]))
                        {
                            parsed.Quantity = parsedQty;
                            parsed.Unit = unit.ToLowerInvariant();
                            return afterUnit.Trim();
                        }
                    }
                }

                // Leading quantity parsed but no known unit follows.
                // Still keep the quantity — the ingredient has no unit
                // e.g. "½ lemon finely zested"
                parsed.Quantity = parsedQty;
                return rest.Trim();
            }
        }

        // 3. Try "325g butter..." — digits then known unit (no space) then text
        if (input.Length > 2)
        {
            // Use a simple scan: find where digits end, check if that+next is a known unit
            var digitEnd = 0;
            while (digitEnd < input.Length && char.IsDigit(input[digitEnd])) digitEnd++;
            if (digitEnd > 0 && digitEnd < input.Length)
            {
                var unitEnd = digitEnd;
                while (unitEnd < input.Length && char.IsLetter(input[unitEnd])) unitEnd++;
                var candidateUnit = input[digitEnd..unitEnd];

                if (unitEnd < input.Length && char.IsWhiteSpace(input[unitEnd]) &&
                    KnownUnits.Contains(candidateUnit))
                {
                    var leadingQty = ParseFraction(input[..digitEnd]);
                    if (leadingQty.HasValue)
                    {
                        parsed.Quantity = leadingQty;
                        parsed.Unit = candidateUnit.ToLowerInvariant();
                        return input[unitEnd..].Trim();
                    }
                }
            }
        }

        // 3. Try trailing format: "Strutto 50 g", "Farina 00 250 g", "Uova 2" (name qty unit)
        var trailingResult = TryExtractTrailingQuantityAndUnit(input, parsed);
        if (trailingResult != input)
            return trailingResult;

        return input;
    }

    /// <summary>
    /// Tries to extract a trailing [quantity] [unit] pattern from the end of the string.
    /// Handles Italian recipe format: "Strutto 50 g", "Scorza di limone ½", "Uova 2".
    /// </summary>
    private static string TryExtractTrailingQuantityAndUnit(string input, ParsedIngredient parsed)
    {
        // Already parsed — skip
        if (parsed.Quantity.HasValue) return input;

        // Check if the last character is a standalone unicode fraction: "Scorza di limone ½"
        if (input.Length > 1)
        {
            var lastChar = input[^1];
            if ("½⅓¼⅛⅔¾⅜⅝⅞⅕⅙".Contains(lastChar))
            {
                var candidateQty = ParseFraction(lastChar.ToString());
                if (candidateQty.HasValue)
                {
                    parsed.Quantity = candidateQty;
                    return input[..^1].Trim();
                }
            }
        }

        var lastSpace = input.LastIndexOf(' ');
        if (lastSpace <= 0) return input;

        var candidateUnit = input[(lastSpace + 1)..];

        if (KnownUnits.Contains(candidateUnit))
        {
            var beforeUnit = input[..lastSpace];
            var secondLastSpace = beforeUnit.LastIndexOf(' ');
            if (secondLastSpace <= 0) return input;

            var candidateQtyStr = beforeUnit[(secondLastSpace + 1)..];
            var candidateQty = ParseFraction(NormalizeQuantity(candidateQtyStr));

            if (candidateQty.HasValue)
            {
                parsed.Quantity = candidateQty;
                parsed.Unit = candidateUnit.ToLowerInvariant();
                return beforeUnit[..secondLastSpace].Trim();
            }

            return input;
        }

        // No known unit — try bare number at end: "Uova 2", "Tuorli 1"
        if (float.TryParse(candidateUnit, NumberStyles.Any, CultureInfo.InvariantCulture, out var bareQty))
        {
            parsed.Quantity = bareQty;
            return input[..lastSpace].Trim();
        }

        // Check if the last "word" starts with a unicode fraction attached to a letter
        // like "q.b." — not a quantity
        return input;
    }

    private static string StripLeadingParenthetical(string name, ParsedIngredient parsed)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        var match = LeadingParentheticalRegex.Match(name);
        if (match.Success)
        {
            parsed.Notes = match.Value.Trim();
            return name[match.Length..].Trim();
        }

        return name;
    }

    /// <summary>
    /// Normalizes a quantity string: replaces the first comma with a dot
    /// if it appears in a numeric context (European decimal separator).
    /// Only acts on the isolated quantity substring, not the full ingredient,
    /// so commas in ingredient names are never affected.
    /// </summary>
    private static string NormalizeQuantity(string qty)
    {
        // Handle ranges: "1-2 tsp" → use "1" (first value in range)
        var hyphenIdx = qty.IndexOf('-');
        if (hyphenIdx > 0)
        {
            qty = qty[..hyphenIdx].Trim();
        }

        // Only replace a comma that appears between digits (European decimal)
        var commaIdx = qty.IndexOf(',');
        if (commaIdx > 0 && commaIdx < qty.Length - 1 &&
            char.IsDigit(qty[commaIdx - 1]) && char.IsDigit(qty[commaIdx + 1]))
        {
            return qty[..commaIdx] + "." + qty[(commaIdx + 1)..];
        }

        return qty;
    }

    private static float? ParseFraction(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Normalize unicode fractions to ASCII with a leading space
        // so "1½" becomes "1 1/2" (not "11/2")
        var normalized = input
            .Replace("½", " 1/2").Replace("⅓", " 1/3")
            .Replace("¼", " 1/4").Replace("⅛", " 1/8")
            .Replace("⅔", " 2/3").Replace("¾", " 3/4")
            .Replace("⅜", " 3/8").Replace("⅝", " 5/8")
            .Replace("⅞", " 7/8").Replace("⅕", " 1/5")
            .Replace("⅙", " 1/6")
            .TrimStart();

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
