using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace FridgeScan.Services.RecipeImport;

public class NextDataRecipeExtractor : RecipeExtractor
{
    public override int Priority => 80;

    public override Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl)
    {
        var result = new RecipeExtractionResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scriptNode = doc.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']");
        if (scriptNode == null)
            return Task.FromResult(result);

        JObject root;
        try
        {
            root = JObject.Parse(scriptNode.InnerText);
        }
        catch
        {
            return Task.FromResult(result);
        }

        var pp = root["props"]?["pageProps"];
        if (pp == null)
            return Task.FromResult(result);

        var schema = pp["schema"];
        if (schema is JArray schemaArr)
        {
            schema = schemaArr.FirstOrDefault(x =>
                string.Equals((string?)x["@type"], "Recipe", StringComparison.OrdinalIgnoreCase));
        }

        result.Success = true;
        result.RecipeSource = "next-data";

        result.Name = SanitizeText((string?)pp["title"]);
        result.Servings = SanitizeText((string?)pp["servings"]);
        result.Difficulty = SanitizeText((string?)pp["skillLevel"]);

        if (schema is JObject schemaObj)
        {
            result.PrepTime = ParseIso8601Duration((string?)schemaObj["prepTime"]);
            result.CookTime = ParseIso8601Duration((string?)schemaObj["cookTime"]);
            result.Name ??= SanitizeText((string?)schemaObj["name"]);

            if (schemaObj["recipeIngredient"] is JArray ingredients)
                result.Ingredients = ingredients
                    .Select(i => ConvertImperialToMetric(SanitizeText(i.ToString())))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
        }

        result.Ingredients ??= ParseStructuredIngredients(pp["ingredients"]);
        result.MethodSteps = ParseMethodSteps(pp["method"]);

        var hasData = (result.Ingredients?.Count > 0) || (result.MethodSteps?.Count > 0);
        result.Success = hasData;

        return Task.FromResult(result);
    }

    private static List<string> ParseStructuredIngredients(JToken? ingredients)
    {
        var result = new List<string>();
        if (ingredients is not JArray sections) return result;

        foreach (var section in sections)
        {
            if (section["ingredients"] is not JArray items) continue;

            foreach (var item in items)
            {
                var qty = (string?)item["quantityText"] ?? "";
                var ing = (string?)item["ingredientText"] ?? "";
                var note = (string?)item["note"] ?? "";
                var line = string.IsNullOrEmpty(qty) ? ing : $"{qty} {ing}";
                if (!string.IsNullOrEmpty(note)) line = $"{line}, {note}";
                line = SanitizeText(line);
                if (!string.IsNullOrWhiteSpace(line))
                    result.Add(ConvertImperialToMetric(line));
            }
        }

        return result;
    }

    private static List<string> ParseMethodSteps(JToken? method)
    {
        var result = new List<string>();
        if (method is not JArray sections) return result;

        foreach (var section in sections)
        {
            if (section["steps"] is not JArray steps) continue;
            foreach (var step in steps)
            {
                var text = SanitizeText((string?)step["description"] ?? (string?)step["text"]);
                if (!string.IsNullOrWhiteSpace(text))
                    result.Add(text);
            }
        }

        return result;
    }
}
