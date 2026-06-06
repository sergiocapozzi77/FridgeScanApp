using FridgeScan.Helpers;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace FridgeScan.Services.RecipeImport;

public class NextDataRecipeExtractor : RecipeExtractor
{
    private const string Tag = "FridgeScan.NextDataExtractor";

    public override int Priority => 80;

    public override Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl)
    {
        var result = new RecipeExtractionResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scriptNode = doc.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']");
        if (scriptNode == null)
        {
            Logger.Debug(Tag, $"no __NEXT_DATA__ script tag ({baseUrl.Host})");
            return Task.FromResult(result);
        }

        JObject root;
        try
        {
            root = JObject.Parse(scriptNode.InnerText);
        }
        catch (Exception ex)
        {
            Logger.Debug(Tag, $"__NEXT_DATA__ JSON parse error: {ex.Message}");
            return Task.FromResult(result);
        }

        var pp = root["props"]?["pageProps"];
        if (pp == null)
        {
            Logger.Debug(Tag, "__NEXT_DATA__ has no props.pageProps");
            return Task.FromResult(result);
        }

        var schema = pp["schema"];
        if (schema is JArray schemaArr)
        {
            schema = schemaArr.FirstOrDefault(x =>
                string.Equals(SafeString(x?["@type"]), "Recipe", StringComparison.OrdinalIgnoreCase));
        }

        result.Success = true;
        result.RecipeSource = baseUrl.Host;

        result.Name = SanitizeText(SafeString(pp["title"]));
        result.Servings = SanitizeText(SafeString(pp["servings"]));
        result.Difficulty = SanitizeText(SafeString(pp["skillLevel"]));

        if (schema is JObject schemaObj)
        {
            result.PrepTime = ParseIso8601Duration(SafeString(schemaObj["prepTime"]));
            result.CookTime = ParseIso8601Duration(SafeString(schemaObj["cookTime"]));
            result.Name ??= SanitizeText(SafeString(schemaObj["name"]));

            if (schemaObj["recipeIngredient"] is JArray ingredients)
                result.Ingredients = ingredients
                    .Select(i => SanitizeText(i.ToString()))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
        }

        result.Ingredients ??= ParseStructuredIngredients(pp["ingredients"]);
        result.MethodSteps = ParseMethodSteps(pp["method"]);

        var hasData = (result.Ingredients?.Count > 0) || (result.MethodSteps?.Count > 0);
        result.Success = hasData;

        var ingCount = result.Ingredients?.Count ?? 0;
        var stepCount = result.MethodSteps?.Count ?? 0;
        Logger.Debug(Tag, $"extracted name='{result.Name}', ingredients={ingCount}, steps={stepCount}, success={hasData} ({baseUrl.Host})");

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
                var qty = SafeString(item["quantityText"]) ?? "";
                var ing = SafeString(item["ingredientText"]) ?? "";
                var note = SafeString(item["note"]) ?? "";
                var line = string.IsNullOrEmpty(qty) ? ing : $"{qty} {ing}";
                if (!string.IsNullOrEmpty(note)) line = $"{line}, {note}";
                line = SanitizeText(line);
                if (!string.IsNullOrWhiteSpace(line))
                    result.Add(line);
            }
        }

        return result;
    }

    private static List<InstructionSection>? ParseMethodSteps(JToken? method)
    {
        var steps = new List<string>();
        if (method is not JArray sections) return null;

        foreach (var section in sections)
        {
            if (section["steps"] is not JArray stepItems) continue;
            foreach (var step in stepItems)
            {
                var text = SanitizeText(SafeString(step["description"]) ?? SafeString(step["text"]));
                if (!string.IsNullOrWhiteSpace(text))
                    steps.Add(text);
            }
        }

        return steps.Count > 0
            ? new List<InstructionSection> { new() { Steps = steps } }
            : null;
    }
}
