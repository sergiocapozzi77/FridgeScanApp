using FridgeScan.Helpers;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace FridgeScan.Services.RecipeImport;

public class JsonLdRecipeExtractor : RecipeExtractor
{
    private const string Tag = "FridgeScan.JsonLdExtractor";

    public override int Priority => 100;

    public override Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl)
    {
        var result = new RecipeExtractionResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var schema = ExtractRecipeSchema(doc);
        if (schema == null)
        {
            Logger.Debug(Tag, $"no JSON-LD Recipe schema found ({baseUrl.Host})");
            return Task.FromResult(result);
        }

        result.Success = true;
        result.RecipeSource = baseUrl.Host;

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
            result.MethodSteps = ExtractInstructions(instructions);
        }

        if (schema["nutrition"] is JObject nutrition)
        {
            result.Nutritions = nutrition.Properties()
                .Select(p => SanitizeText($"{p.Name}: {p.Value}"))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        var ingCount = result.Ingredients?.Count ?? 0;
        var stepCount = result.MethodSteps?.Count ?? 0;
        Logger.Debug(Tag, $"extracted name='{result.Name}', ingredients={ingCount}, steps={stepCount} ({baseUrl.Host})");

        return Task.FromResult(result);
    }

    private static JObject? ExtractRecipeSchema(HtmlDocument doc)
    {
        var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scriptNodes == null)
        {
            Logger.Debug(Tag, "no <script type='application/ld+json'> nodes found");
            return null;
        }

        foreach (var node in scriptNodes)
        {
            try
            {
                var json = JToken.Parse(node.InnerText);
                var typeNames = json["@type"]?.ToString() ?? "(no @type)";

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

                Logger.Debug(Tag, $"JSON-LD block has @type={typeNames} but no Recipe entry found");
            }
            catch (Exception ex)
            {
                Logger.Debug(Tag, $"JSON-LD parse error: {ex.Message}");
            }
        }

        return null;
    }

    private static JObject? FindRecipeInNode(JToken node)
    {
        if (node is JObject obj && IsRecipeType(obj["@type"]))
            return obj;

        if (node is JObject root && root["@graph"] is JArray graph)
        {
            foreach (var item in graph)
            {
                if (item is JObject go && IsRecipeType(go["@type"]))
                    return go;
            }
        }

        if (node is JObject root2 && root2["mainEntity"] is JObject me &&
            IsRecipeType(me["@type"]))
            return me;

        return null;
    }

    /// <summary>
    /// Checks whether a JSON-LD @type value indicates a Recipe.
    /// Handles both string form ("Recipe") and array form (["Recipe", "NewsArticle"]).
    /// </summary>
    private static bool IsRecipeType(JToken? typeToken)
    {
        if (typeToken is JValue jv && jv.Value is string s)
            return string.Equals(s, "Recipe", StringComparison.OrdinalIgnoreCase);

        if (typeToken is JArray arr)
            return arr.Values<string>().Any(v =>
                string.Equals(v, "Recipe", StringComparison.OrdinalIgnoreCase));

        return false;
    }

    private static string ExtractImageUrl(JToken? imageToken)
    {
        if (imageToken == null) return string.Empty;

        if (imageToken is JArray arr)
        {
            var first = arr.FirstOrDefault();
            if (first == null) return string.Empty;
            if (first is JObject firstObj && firstObj["url"] != null)
                return firstObj["url"]!.ToString();
            return first.ToString();
        }

        if (imageToken is JObject jObj && jObj["url"] != null)
            return jObj["url"]!.ToString();

        return imageToken.ToString();
    }

    private static List<InstructionSection>? ExtractInstructions(JArray instructions)
    {
        var sections = new List<InstructionSection>();

        foreach (var item in instructions)
        {
            // HowToSection: has name + itemListElement array of HowToStep
            if (item is JObject obj && obj["itemListElement"] is JArray subSteps)
            {
                var section = new InstructionSection
                {
                    Name = SanitizeText(SafeString(obj["name"])),
                    Steps = subSteps
                        .Select(ExtractSingleStepText)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList()
                };
                if (section.Steps.Count > 0)
                    sections.Add(section);
                continue;
            }

            // HowToStep with text, or plain string
            var stepText = ExtractSingleStepText(item);
            if (!string.IsNullOrWhiteSpace(stepText))
            {
                // Coalesce into the last unnamed section if one exists
                var last = sections.LastOrDefault();
                if (last != null && last.Name == null)
                    last.Steps.Add(stepText);
                else
                    sections.Add(new InstructionSection { Name = null, Steps = new List<string> { stepText } });
            }
        }

        return sections.Count > 0 ? sections : null;
    }

    private static string ExtractSingleStepText(JToken step)
    {
        if (step is JObject obj && obj.TryGetValue("text", out var textToken))
            return SanitizeText(SafeString(textToken));

        if (step is JValue jv)
            return SanitizeText(jv.ToString());

        return SanitizeText(step?.ToString());
    }
}
