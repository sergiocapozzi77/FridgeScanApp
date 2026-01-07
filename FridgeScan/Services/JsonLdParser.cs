using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Net;

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


    private string SanitizeText(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        // 1. Decodifica le entità HTML (trasforma &frac12; in ½, &amp; in &, ecc.)
        string decoded = WebUtility.HtmlDecode(input);

        // 2. Rimuove eventuali tag HTML residui (es. <b> o <a>) tramite Regex
        string cleanText = System.Text.RegularExpressions.Regex.Replace(decoded, "<.*?>", String.Empty);

        return cleanText.Trim();
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
