using HtmlAgilityPack;

namespace FridgeScan.Services.RecipeImport;

public class MicrodataRecipeExtractor : RecipeExtractor
{
    public override int Priority => 60;

    public override Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl)
    {
        var result = new RecipeExtractionResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var recipeNode = doc.DocumentNode.SelectSingleNode("//*[contains(@itemtype,'Recipe')]");
        if (recipeNode == null)
            return Task.FromResult(result);

        var name = GetItempropText(recipeNode, "name")
                   ?? doc.DocumentNode.SelectSingleNode("//h1[@itemprop='name']")?.InnerText
                   ?? doc.DocumentNode.SelectSingleNode("//*[@itemprop='name']")?.InnerText;

        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(result);

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

        return Task.FromResult(result);
    }

    private static string? GetItempropText(HtmlNode parent, string itemprop)
    {
        var node = parent.SelectSingleNode($".//*[@itemprop='{itemprop}']");
        return node?.InnerText;
    }
}
