using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace FridgeScan.Services.RecipeImport;

public class PostContentRecipeExtractor : RecipeExtractor
{
    public override int Priority => 70;

    public override Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl)
    {
        var result = new RecipeExtractionResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scriptNode = doc.DocumentNode.SelectSingleNode("//script[@id='__POST_CONTENT__']");
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

        result.Success = true;
        result.RecipeSource = baseUrl.Host;

        result.Name = SanitizeText(SafeString(root["title"]));
        result.Servings = SanitizeText(SafeString(root["servings"]));
        result.Difficulty = SanitizeText(SafeString(root["skillLevel"]));

        if (root["schema"] is JObject schema)
        {
            result.PrepTime = ParseIso8601Duration(SafeString(schema["prepTime"]));
            result.CookTime = ParseIso8601Duration(SafeString(schema["cookTime"]));
        }

        if (root["ingredients"] is JArray ingredientSections)
        {
            var ingredients = new List<string>();
            foreach (var section in ingredientSections)
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
                        ingredients.Add(ConvertImperialToMetric(line));
                }
            }
            result.Ingredients = ingredients;
        }

        if (root["method"] is JArray methodSections)
        {
            var steps = new List<string>();
            foreach (var section in methodSections)
            {
                if (section["steps"] is not JArray stepItems) continue;
                foreach (var step in stepItems)
                {
                    var text = SanitizeText(SafeString(step["description"]) ?? SafeString(step["text"]));
                    if (!string.IsNullOrWhiteSpace(text))
                        steps.Add(text);
                }
            }
            result.MethodSteps = steps;
        }

        var hasData = (result.Ingredients?.Count > 0) || (result.MethodSteps?.Count > 0);
        result.Success = hasData;

        return Task.FromResult(result);
    }
}
