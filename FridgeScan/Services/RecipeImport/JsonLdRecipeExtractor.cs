using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace FridgeScan.Services.RecipeImport;

public class JsonLdRecipeExtractor : RecipeExtractor
{
    public override int Priority => 100;

    public override Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl)
    {
        var result = new RecipeExtractionResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var schema = ExtractRecipeSchema(doc);
        if (schema == null)
            return Task.FromResult(result);

        result.Success = true;
        result.RecipeSource = "json-ld";

        result.Name = SanitizeText(SafeString(schema["name"]));
        result.Description = SanitizeText(SafeString(schema["description"]));
        result.PrepTime = ParseIso8601Duration(SafeString(schema["prepTime"]));
        result.CookTime = ParseIso8601Duration(SafeString(schema["cookTime"]));
        result.Servings = SanitizeText(SafeString(schema["recipeYield"]));

        result.ImageUrl = ExtractImageUrl(schema["image"]);

        if (schema["author"] is JObject author)
            result.Author = SanitizeText(SafeString(author["name"]));
        else
            result.Author = SanitizeText(SafeString(schema["author"]));

        if (schema["recipeIngredient"] is JArray ingredients)
        {
            result.Ingredients = ingredients
                .Select(i => ConvertImperialToMetric(SanitizeText(i.ToString())))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        if (schema["recipeInstructions"] is JArray instructions)
        {
            result.MethodSteps = instructions
                .Select(ExtractInstructionText)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        if (schema["nutrition"] is JObject nutrition)
        {
            result.Nutritions = nutrition.Properties()
                .Select(p => SanitizeText($"{p.Name}: {p.Value}"))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        return Task.FromResult(result);
    }

    private static JObject? ExtractRecipeSchema(HtmlDocument doc)
    {
        var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scriptNodes == null) return null;

        foreach (var node in scriptNodes)
        {
            try
            {
                var json = JToken.Parse(node.InnerText);

                if (json is JArray arr)
                {
                    foreach (var item in arr)
                    {
                        var found = FindRecipeInNode(item);
                        if (found != null) return found;
                    }
                }
                else
                {
                    var found = FindRecipeInNode(json);
                    if (found != null) return found;
                }
            }
            catch { /* skip malformed JSON */ }
        }

        return null;
    }

    private static JObject? FindRecipeInNode(JToken node)
    {
        if (node is JObject obj && string.Equals(SafeString(obj["@type"]), "Recipe", StringComparison.OrdinalIgnoreCase))
            return obj;

        if (node is JObject root && root["@graph"] is JArray graph)
        {
            foreach (var item in graph)
            {
                if (item is JObject go && string.Equals(SafeString(go["@type"]), "Recipe", StringComparison.OrdinalIgnoreCase))
                    return go;
            }
        }

        if (node is JObject root2 && root2["mainEntity"] is JObject me &&
            string.Equals(SafeString(me["@type"]), "Recipe", StringComparison.OrdinalIgnoreCase))
            return me;

        return null;
    }

    private static string ExtractImageUrl(JToken? imageToken)
    {
        if (imageToken == null) return string.Empty;

        if (imageToken is JArray arr)
        {
            var first = arr.FirstOrDefault();
            if (first == null) return string.Empty;
            if (first["url"] != null) return first["url"]!.ToString();
            return first.ToString();
        }

        if (imageToken is JObject obj && obj["url"] != null)
            return obj["url"]!.ToString();

        return imageToken.ToString();
    }

    private static string ExtractInstructionText(JToken step)
    {
        if (step is JObject obj && obj.TryGetValue("text", out var textToken))
            return SanitizeText(SafeString(textToken));

        if (step is JValue jv)
            return SanitizeText(jv.ToString());

        return SanitizeText(step?.ToString());
}
}
