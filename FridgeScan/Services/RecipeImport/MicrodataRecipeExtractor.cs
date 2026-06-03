using FridgeScan.Helpers;
using HtmlAgilityPack;

namespace FridgeScan.Services.RecipeImport;

public class MicrodataRecipeExtractor : RecipeExtractor
{
    private const string Tag = "FridgeScan.MicrodataExtractor";

    public override int Priority => 60;

    public override Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl)
    {
        var result = new RecipeExtractionResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var recipeNode = doc.DocumentNode.SelectSingleNode("//*[contains(@itemtype,'Recipe')]");
        if (recipeNode == null)
        {
            Logger.Debug(Tag, "no element with itemtype containing 'Recipe' found");
            return Task.FromResult(result);
        }

        var name = GetItempropText(recipeNode, "name")
                   ?? doc.DocumentNode.SelectSingleNode("//h1[@itemprop='name']")?.InnerText
                   ?? doc.DocumentNode.SelectSingleNode("//*[@itemprop='name']")?.InnerText;

        if (string.IsNullOrWhiteSpace(name))
        {
            Logger.Debug(Tag, $"found <article itemtype='Recipe'> but no itemprop='name' with text ({baseUrl.Host})");
            return Task.FromResult(result);
        }

        result.Success = true;
        result.RecipeSource = baseUrl.Host;
        result.Name = SanitizeText(name);

        var prepMeta = recipeNode.SelectSingleNode(".//meta[@itemprop='prepTime']");
        result.PrepTime = ParseIso8601Duration(prepMeta?.GetAttributeValue("content", null));

        var cookMeta = recipeNode.SelectSingleNode(".//meta[@itemprop='cookTime']");
        result.CookTime = ParseIso8601Duration(cookMeta?.GetAttributeValue("content", null));

        var yieldMeta = recipeNode.SelectSingleNode(".//meta[@itemprop='recipeYield']");
        result.Servings = SanitizeText(yieldMeta?.GetAttributeValue("content", null)
                                       ?? GetItempropText(recipeNode, "recipeYield"));

        result.ImageUrl = ExtractImageFromMicrodata(recipeNode);

        var ingredients = new List<string>();
        var ingredientNodes = recipeNode.SelectNodes(".//*[@itemprop='recipeIngredient']");
        if (ingredientNodes != null)
        {
            foreach (var node in ingredientNodes)
            {
                var text = SanitizeText(node.InnerText);
                if (!string.IsNullOrWhiteSpace(text))
                    ingredients.Add(ConvertImperialToMetric(text));
            }
        }
        result.Ingredients = ingredients;

        var steps = new List<string>();
        var instructionNodes = recipeNode.SelectNodes(".//*[@itemprop='recipeInstructions']//p");
        if (instructionNodes != null)
        {
            foreach (var node in instructionNodes)
            {
                if (node.HasClass("recipe-info") || node.HasClass("ing-header"))
                    continue;
                var text = SanitizeText(node.InnerText);
                if (!string.IsNullOrWhiteSpace(text) && text.Length > 30)
                    steps.Add(text);
            }
        }
        result.MethodSteps = steps;

        var ingCount = result.Ingredients?.Count ?? 0;
        var stepCount = result.MethodSteps?.Count ?? 0;
        Logger.Debug(Tag, $"extracted name='{result.Name}', ingredients={ingCount}, steps={stepCount} ({baseUrl.Host})");

        return Task.FromResult(result);
    }

    /// <summary>
    /// Extracts the recipe image URL from microdata.
    /// Checks: (1) &lt;meta itemprop="image" content="..."&gt;,
    /// (2) &lt;img itemprop="image" src="..."&gt;,
    /// (3) any element with itemprop="image" containing an image URL.
    /// </summary>
    private static string? ExtractImageFromMicrodata(HtmlNode recipeNode)
    {
        // 1) <meta itemprop="image" content="...">
        var metaImg = recipeNode.SelectSingleNode(".//meta[@itemprop='image']");
        if (metaImg != null)
        {
            var content = metaImg.GetAttributeValue("content", null);
            if (!string.IsNullOrWhiteSpace(content))
                return content;
        }

        // 2) <img itemprop="image" src="...">
        var imgNode = recipeNode.SelectSingleNode(".//img[@itemprop='image']");
        if (imgNode != null)
        {
            var src = imgNode.GetAttributeValue("src", null);
            if (!string.IsNullOrWhiteSpace(src))
                return src;
        }

        // 3) Any element with itemprop="image" that has a url-like attribute
        var anyImg = recipeNode.SelectSingleNode(".//*[@itemprop='image']");
        if (anyImg != null)
        {
            foreach (var attr in new[] { "content", "src", "href" })
            {
                var val = anyImg.GetAttributeValue(attr, null);
                if (!string.IsNullOrWhiteSpace(val) && val.StartsWith("http"))
                    return val;
            }
        }

        return null;
    }

    private static string? GetItempropText(HtmlNode parent, string itemprop)
    {
        var nodes = parent.SelectNodes($".//*[@itemprop='{itemprop}']");
        if (nodes == null) return null;

        // Skip void elements (meta, link) — they store values in attributes, not InnerText.
        // If a void element appears first (e.g. <meta itemprop='name'> inside an author block),
        // we need to continue searching for a visible-text element.
        foreach (var node in nodes)
        {
            if (node.Name is "meta" or "link") continue;
            var text = node.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return null;
    }
}
