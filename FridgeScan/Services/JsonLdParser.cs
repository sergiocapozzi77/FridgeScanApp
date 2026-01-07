using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace FridgeScan.Services;

public class JsonLdParser
{
    public async Task<RecipeSuggestion> GetFullRecipeDetailsAsync(string url)
    {
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        try
        {
            string html = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract schema Recipe object
            JObject recipeSchema = ExtractRecipeSchema(doc);

            // Extract __POST_CONTENT__ fallback
            var postContentNode = doc.DocumentNode.SelectSingleNode("//script[@id='__POST_CONTENT__']");
            JObject postData = postContentNode != null ? JObject.Parse(postContentNode.InnerText) : null;

            if (recipeSchema == null && postData == null)
                return null;

            var details = new RecipeSuggestion
            {
                Name =
                    recipeSchema?["name"]?.ToString()
                    ?? postData?["title"]?.ToString(),

                ImageUrl =
                    ExtractImageUrl(recipeSchema?["image"])
                    ?? ExtractImageUrl(postData?["image"])
                    ?? null,

                Serving =
                    recipeSchema?["recipeYield"]?.ToString()
                    ?? postData?["servings"]?.ToString()
                    ?? "N/A",

                Difficulty =
                    postData?["skillLevel"]?.ToString()
                    ?? "Easy",

                PrepTime =
                    ParseIso8601Duration(recipeSchema?["prepTime"]?.ToString())
                    ?? ParseIso8601Duration(postData?["schema"]?["prepTime"]?.ToString()),

                CookTime =
                    ParseIso8601Duration(recipeSchema?["cookTime"]?.ToString())
                    ?? ParseIso8601Duration(postData?["schema"]?["cookTime"]?.ToString()),

                Ingredients =
                    recipeSchema?["recipeIngredient"]?
                        .Select(i => SanitizeText(i.ToString()))
                        .ToList()
                    ?? postData?["ingredients"]?[0]?["ingredients"]?
                        .Select(i => SanitizeText($"{i["quantityText"]} {i["ingredientText"]} {i["note"]}"))
                        .ToList()
                    ?? new List<string>(),

                MethodSteps =
                    recipeSchema?["recipeInstructions"]?
                        .Select(step => ExtractStepText(step))
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList()
                    ?? postData?["methodSteps"]?
                        .Select(m => SanitizeText(m["content"]?[0]?["data"]?["value"]?.ToString()))
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList()
                    ?? new List<string>(),

                Nutritions =
                    recipeSchema?["nutrition"]?
                        .Children<JProperty>()
                        .Select(n => SanitizeText($"{n.Name}: {n.Value}"))
                        .ToList()
                    ?? postData?["nutritions"]?
                        .Select(n => SanitizeText($"{n["label"]}: {n["value"]}{n["unit"]} {n["additionalText"]}"))
                        .ToList()
                    ?? new List<string>()
            };

            return details;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore critico durante lo scraping: {ex.Message}");
            return null;
        }
    }

    private string ExtractImageUrl(JToken imageToken)
    {
        if (imageToken == null)
            return null;

        // Case 1: image is an array
        if (imageToken is JArray arr)
        {
            var first = arr.FirstOrDefault();
            if (first == null)
                return null;

            // If it has a "url" property → use it
            if (first["url"] != null)
                return first["url"]?.ToString();

            // Otherwise treat the element as a string
            return first.ToString();
        }

        // Case 2: image is an object with "url"
        if (imageToken is JObject obj && obj["url"] != null)
            return obj["url"]?.ToString();

        // Case 3: image is a plain string
        return imageToken.ToString();
    }

    private string ExtractStepText(JToken step)
    {
        // Case 1: step is an object with a "text" field
        if (step is JObject obj && obj.TryGetValue("text", out var textToken))
            return SanitizeText(textToken?.ToString());

        // Case 2: step is a string or something else
        return SanitizeText(step?.ToString());
    }

    private JObject ExtractRecipeSchema(HtmlDocument doc)
    {
        var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scriptNodes == null) return null;

        foreach (var node in scriptNodes)
        {
            try
            {
                var json = JToken.Parse(node.InnerText);

                // Case 1: JSON is an array → find the Recipe object
                if (json is JArray arr)
                {
                    var recipeObj = arr
                        .FirstOrDefault(x => x["@type"]?.ToString() == "Recipe") as JObject;

                    if (recipeObj != null)
                        return recipeObj;
                }

                // Case 2: JSON is a single object
                if (json is JObject obj && obj["@type"]?.ToString() == "Recipe")
                {
                    return obj;
                }
            }
            catch
            {
                // Ignore malformed JSON blocks
            }
        }

        return null;
    }

    private string DecodeFractions(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&frac12;", "½")
            .Replace("&frac13;", "⅓")
            .Replace("&frac14;", "¼")
            .Replace("&frac15;", "⅕")
            .Replace("&frac16;", "⅙")
            .Replace("&frac18;", "⅛")
            .Replace("&frac23;", "⅔")
            .Replace("&frac34;", "¾")
            .Replace("&frac38;", "⅜")
            .Replace("&frac58;", "⅝")
            .Replace("&frac78;", "⅞");
    }


    private string SanitizeText(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        // 1. Decodifica le entità HTML (trasforma &frac12; in ½, &amp; in &, ecc.)
        string decoded = WebUtility.HtmlDecode(input);

        // 2. Rimuove eventuali tag HTML residui (es. <b> o <a>) tramite Regex
        string cleanText = System.Text.RegularExpressions.Regex.Replace(decoded, "<.*?>", String.Empty).Trim();
        cleanText = DecodeFractions(cleanText);
        cleanText = ConvertImperialToMetric(cleanText);

        return cleanText.Trim();

    }

    private double ParseFraction(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0;

        // Replace unicode fractions with decimal
        input = input
            .Replace("½", "1/2")
            .Replace("⅓", "1/3")
            .Replace("¼", "1/4")
            .Replace("⅛", "1/8")
            .Replace("⅔", "2/3")
            .Replace("¾", "3/4")
            .Replace("⅜", "3/8")
            .Replace("⅝", "5/8")
            .Replace("⅞", "7/8");

        // Mixed number: "1 1/2"
        if (input.Contains(" "))
        {
            var parts = input.Split(' ');
            return ParseFraction(parts[0]) + ParseFraction(parts[1]);
        }

        // Fraction: "1/2"
        if (input.Contains("/"))
        {
            var parts = input.Split('/');
            if (double.TryParse(parts[0], out double n) &&
                double.TryParse(parts[1], out double d))
                return n / d;
        }

        // Normal number
        if (double.TryParse(input, out double value))
            return value;

        return 0;
    }

    private readonly Dictionary<string, double> VolumeToMl = new()
{
    { "cup", 240 },
    { "cups", 240 },
    { "tbsp", 15 },
    { "tablespoon", 15 },
    { "tablespoons", 15 },
    { "tbs", 15 },
    { "tsp", 5 },
    { "teaspoon", 5 },
    { "teaspoons", 5 },
    { "fl oz", 30 },
    { "floz", 30 }
};

    private readonly Dictionary<string, double> WeightToGrams = new()
{
    { "oz", 28.35 },
    { "ounce", 28.35 },
    { "ounces", 28.35 },
    { "lb", 453.6 },
    { "lbs", 453.6 },
    { "pound", 453.6 },
    { "pounds", 453.6 }
};


    private string ConvertImperialToMetric(string ingredient)
    {
        if (string.IsNullOrWhiteSpace(ingredient))
            return ingredient;

        var parts = ingredient.Split(' ', 3); // quantity, unit, rest
        if (parts.Length < 2)
            return ingredient;

        double quantity = ParseFraction(parts[0]);
        string unit = parts[1].ToLower();
        string rest = parts.Length > 2 ? parts[2] : "";

        // Volume conversions
        if (VolumeToMl.TryGetValue(unit, out double mlFactor))
        {
            double ml = quantity * mlFactor;
            return $"{Math.Round(ml)} ml {rest}".Trim();
        }

        // Weight conversions
        if (WeightToGrams.TryGetValue(unit, out double gFactor))
        {
            double grams = quantity * gFactor;
            return $"{Math.Round(grams)} g {rest}".Trim();
        }

        return ingredient; // no conversion
    }

    private string ParseIso8601Duration(string isoDuration)
    {
        if (string.IsNullOrEmpty(isoDuration)) return "0 mins";
        try
        {
            // Converte PT20M in "20 mins"
            var duration = System.Xml.XmlConvert.ToTimeSpan(isoDuration);
            return $"{(int)duration.TotalMinutes} mins";
        }
        catch { return isoDuration; }
    }
}
