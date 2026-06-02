# Recipe Import System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an extensible recipe import system with four HTML extractors, an image extractor, an ingredient parser, and an orchestrator that merges results.

**Architecture:** Interface-based extractors (`IRecipeExtractor`, `IRecipeImageExtractor`, `IRecipeIngredientParser`) with an abstract base class for shared utilities. An orchestrator (`RecipeImportService`) fetches a URL, runs all extractors in parallel, merges results by priority, and returns a populated `RecipeSuggestion`.

**Tech Stack:** C# / .NET 9, HtmlAgilityPack, Newtonsoft.Json (already referenced), CommunityToolkit.Mvvm

---

### Task 1: Create data models

**Files:**
- Create: `FridgeScan/Services/RecipeImport/RecipeExtractionResult.cs`
- Create: `FridgeScan/Services/RecipeImport/ParsedIngredient.cs`
- Create: `FridgeScan/Services/RecipeImport/RecipeImage.cs`

- [ ] **Step 1: Create RecipeExtractionResult.cs**

```csharp
namespace FridgeScan.Services.RecipeImport;

public class RecipeExtractionResult
{
    public bool Success { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Author { get; set; }
    public string? PrepTime { get; set; }
    public string? CookTime { get; set; }
    public string? TotalTime { get; set; }
    public string? Servings { get; set; }
    public string? Difficulty { get; set; }
    public float? RatingValue { get; set; }
    public int? RatingCount { get; set; }
    public List<string>? Ingredients { get; set; }
    public List<string>? MethodSteps { get; set; }
    public List<string>? Nutritions { get; set; }
    public bool IsPremium { get; set; }
    public string? ContentType { get; set; }
    public string? RecipeSource { get; set; }
}
```

- [ ] **Step 2: Create ParsedIngredient.cs**

```csharp
namespace FridgeScan.Services.RecipeImport;

public class ParsedIngredient
{
    public float? Quantity { get; set; }
    public string? Unit { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
```

- [ ] **Step 3: Create RecipeImage.cs**

```csharp
namespace FridgeScan.Services.RecipeImport;

public class RecipeImage
{
    public string Ref { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Commit**

```bash
git add FridgeScan/Services/RecipeImport/RecipeExtractionResult.cs FridgeScan/Services/RecipeImport/ParsedIngredient.cs FridgeScan/Services/RecipeImport/RecipeImage.cs
git commit -m "feat: add recipe import data models"
```

---

### Task 2: Create interfaces

**Files:**
- Create: `FridgeScan/Services/RecipeImport/IRecipeExtractor.cs`
- Create: `FridgeScan/Services/RecipeImport/IRecipeImageExtractor.cs`
- Create: `FridgeScan/Services/RecipeImport/IRecipeIngredientParser.cs`

- [ ] **Step 1: Create IRecipeExtractor.cs**

```csharp
namespace FridgeScan.Services.RecipeImport;

public interface IRecipeExtractor
{
    int Priority { get; }
    Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl);
}
```

- [ ] **Step 2: Create IRecipeImageExtractor.cs**

```csharp
namespace FridgeScan.Services.RecipeImport;

public interface IRecipeImageExtractor
{
    Task<List<RecipeImage>> ExtractImagesAsync(string html, Uri baseUrl);
}
```

- [ ] **Step 3: Create IRecipeIngredientParser.cs**

```csharp
namespace FridgeScan.Services.RecipeImport;

public interface IRecipeIngredientParser
{
    List<ParsedIngredient> Parse(List<string> rawIngredients);
}
```

- [ ] **Step 4: Commit**

```bash
git add FridgeScan/Services/RecipeImport/IRecipeExtractor.cs FridgeScan/Services/RecipeImport/IRecipeImageExtractor.cs FridgeScan/Services/RecipeImport/IRecipeIngredientParser.cs
git commit -m "feat: add recipe import interfaces"
```

---

### Task 3: Create abstract base RecipeExtractor

**Files:**
- Create: `FridgeScan/Services/RecipeImport/RecipeExtractor.cs`

This moves shared utility methods from the existing `JsonLdParser` into a reusable base class. The existing methods in `JsonLdParser` are kept (not modified) since other code references them.

- [ ] **Step 1: Create RecipeExtractor.cs**

```csharp
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace FridgeScan.Services.RecipeImport;

public abstract class RecipeExtractor : IRecipeExtractor
{
    public abstract int Priority { get; }
    public abstract Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl);

    protected static readonly HttpClient HttpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36" }
        }
    };

    protected async Task<string> FetchHtmlAsync(string url)
    {
        var response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    protected static string SanitizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var decoded = WebUtility.HtmlDecode(input);
        decoded = Regex.Replace(decoded, "<.*?>", string.Empty);
        decoded = decoded
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ");
        decoded = Regex.Replace(decoded, @"\s+", " ");
        return decoded.Trim();
    }

    protected static string DecodeFractions(string text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;

        return text
            .Replace("&frac12;", "½").Replace("&#189;", "½")
            .Replace("&frac13;", "⅓")
            .Replace("&frac14;", "¼").Replace("&#188;", "¼")
            .Replace("&frac15;", "⅕")
            .Replace("&frac16;", "⅙")
            .Replace("&frac18;", "⅛")
            .Replace("&frac23;", "⅔")
            .Replace("&frac34;", "¾").Replace("&#190;", "¾")
            .Replace("&frac38;", "⅜")
            .Replace("&frac58;", "⅝")
            .Replace("&frac78;", "⅞");
    }

    protected static double ParseFraction(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0;

        input = input
            .Replace("½", "1/2").Replace("⅓", "1/3")
            .Replace("¼", "1/4").Replace("⅛", "1/8")
            .Replace("⅔", "2/3").Replace("¾", "3/4")
            .Replace("⅜", "3/8").Replace("⅝", "5/8")
            .Replace("⅞", "7/8").Replace("⅕", "1/5")
            .Replace("⅙", "1/6");

        if (input.Contains(' '))
        {
            var parts = input.Split(' ');
            return ParseFraction(parts[0]) + ParseFraction(parts[1]);
        }

        if (input.Contains('/'))
        {
            var parts = input.Split('/');
            if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var n) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d != 0)
                return n / d;
        }

        if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return value;

        return 0;
    }

    protected static readonly Dictionary<string, double> VolumeToMl = new()
    {
        { "cup", 240 }, { "cups", 240 },
        { "tbsp", 15 }, { "tablespoon", 15 }, { "tablespoons", 15 }, { "tbs", 15 },
        { "tsp", 5 }, { "teaspoon", 5 }, { "teaspoons", 5 },
        { "fl oz", 30 }, { "floz", 30 }
    };

    protected static readonly Dictionary<string, double> WeightToGrams = new()
    {
        { "oz", 28.35 }, { "ounce", 28.35 }, { "ounces", 28.35 },
        { "lb", 453.6 }, { "lbs", 453.6 }, { "pound", 453.6 }, { "pounds", 453.6 }
    };

    protected static string ConvertImperialToMetric(string ingredient)
    {
        if (string.IsNullOrWhiteSpace(ingredient))
            return ingredient;

        var parts = ingredient.Split(' ', 3);
        if (parts.Length < 2)
            return ingredient;

        double quantity = ParseFraction(parts[0]);
        string unit = parts[1].ToLowerInvariant();
        string rest = parts.Length > 2 ? parts[2] : "";

        if (VolumeToMl.TryGetValue(unit, out double mlFactor))
        {
            double ml = quantity * mlFactor;
            return $"{Math.Round(ml)} ml {rest}".Trim();
        }

        if (WeightToGrams.TryGetValue(unit, out double gFactor))
        {
            double grams = quantity * gFactor;
            return $"{Math.Round(grams)} g {rest}".Trim();
        }

        return ingredient;
    }

    protected static string ParseIso8601Duration(string? isoDuration)
    {
        if (string.IsNullOrEmpty(isoDuration)) return string.Empty;
        try
        {
            var duration = System.Xml.XmlConvert.ToTimeSpan(isoDuration);
            return $"{(int)duration.TotalMinutes} mins";
        }
        catch
        {
            return isoDuration;
        }
    }

    protected static string? ExtractNonNullString(Dictionary<string, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value is string s && !string.IsNullOrWhiteSpace(s))
            return s;
        return null;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/Services/RecipeImport/RecipeExtractor.cs
git commit -m "feat: add abstract RecipeExtractor base class with shared utilities"
```

---

### Task 4: Create JsonLdRecipeExtractor

**Files:**
- Create: `FridgeScan/Services/RecipeImport/JsonLdRecipeExtractor.cs`

Ports JSON-LD extraction logic from the reference `main.js` `extractJsonLd()` and the existing `JsonLdParser.ExtractRecipeSchema()`.

- [ ] **Step 1: Create JsonLdRecipeExtractor.cs**

```csharp
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

        result.Name = SanitizeText((string?)schema["name"]);
        result.Description = SanitizeText((string?)schema["description"]);
        result.PrepTime = ParseIso8601Duration((string?)schema["prepTime"]);
        result.CookTime = ParseIso8601Duration((string?)schema["cookTime"]);
        result.Servings = SanitizeText((string?)schema["recipeYield"]);

        result.ImageUrl = ExtractImageUrl(schema["image"]);

        if (schema["author"] is JObject author)
            result.Author = SanitizeText((string?)author["name"]);
        else
            result.Author = SanitizeText((string?)schema["author"]);

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
        if (node is JObject obj && string.Equals((string?)obj["@type"], "Recipe", StringComparison.OrdinalIgnoreCase))
            return obj;

        // Check @graph array
        if (node is JObject root && root["@graph"] is JArray graph)
        {
            foreach (var item in graph)
            {
                if (item is JObject go && string.Equals((string?)go["@type"], "Recipe", StringComparison.OrdinalIgnoreCase))
                    return go;
            }
        }

        // Check mainEntity
        if (node is JObject root2 && root2["mainEntity"] is JObject me &&
            string.Equals((string?)me["@type"], "Recipe", StringComparison.OrdinalIgnoreCase))
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
            return SanitizeText(textToken?.ToString());

        if (step is JValue jv)
            return SanitizeText(jv.ToString());

        return SanitizeText(step?.ToString());
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/Services/RecipeImport/JsonLdRecipeExtractor.cs
git commit -m "feat: add JsonLdRecipeExtractor"
```

---

### Task 5: Create MicrodataRecipeExtractor

**Files:**
- Create: `FridgeScan/Services/RecipeImport/MicrodataRecipeExtractor.cs`

Ports microdata extraction from `main.js` `extractMicrodata()`.

- [ ] **Step 1: Create MicrodataRecipeExtractor.cs**

```csharp
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
        result.RecipeSource = "microdata";
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
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/Services/RecipeImport/MicrodataRecipeExtractor.cs
git commit -m "feat: add MicrodataRecipeExtractor"
```

---

### Task 6: Create NextDataRecipeExtractor

**Files:**
- Create: `FridgeScan/Services/RecipeImport/NextDataRecipeExtractor.cs`

Ports `__NEXT_DATA__` parsing from `main.js` `parseNextData()`.

- [ ] **Step 1: Create NextDataRecipeExtractor.cs**

```csharp
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

        // Try to find schema in pageProps
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

        // Parse ingredients from ingredients structure
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
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/Services/RecipeImport/NextDataRecipeExtractor.cs
git commit -m "feat: add NextDataRecipeExtractor"
```

---

### Task 7: Create PostContentRecipeExtractor

**Files:**
- Create: `FridgeScan/Services/RecipeImport/PostContentRecipeExtractor.cs`

Ports `__POST_CONTENT__` parsing from `main.js` `parsePostContent()`.

- [ ] **Step 1: Create PostContentRecipeExtractor.cs**

```csharp
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
        result.RecipeSource = "post-content";

        result.Name = SanitizeText((string?)root["title"]);
        result.Servings = SanitizeText((string?)root["servings"]);
        result.Difficulty = SanitizeText((string?)root["skillLevel"]);

        if (root["schema"] is JObject schema)
        {
            result.PrepTime = ParseIso8601Duration((string?)schema["prepTime"]);
            result.CookTime = ParseIso8601Duration((string?)schema["cookTime"]);
        }

        // Parse ingredients
        if (root["ingredients"] is JArray ingredientSections)
        {
            var ingredients = new List<string>();
            foreach (var section in ingredientSections)
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
                        ingredients.Add(ConvertImperialToMetric(line));
                }
            }
            result.Ingredients = ingredients;
        }

        // Parse method steps
        if (root["method"] is JArray methodSections)
        {
            var steps = new List<string>();
            foreach (var section in methodSections)
            {
                if (section["steps"] is not JArray stepItems) continue;
                foreach (var step in stepItems)
                {
                    var text = SanitizeText((string?)step["description"] ?? (string?)step["text"]);
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
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/Services/RecipeImport/PostContentRecipeExtractor.cs
git commit -m "feat: add PostContentRecipeExtractor"
```

---

### Task 8: Create RecipeImageExtractor

**Files:**
- Create: `FridgeScan/Services/RecipeImport/RecipeImageExtractor.cs`

Ports image extraction from `main.js` `extractStepImages()`.

- [ ] **Step 1: Create RecipeImageExtractor.cs**

```csharp
using HtmlAgilityPack;

namespace FridgeScan.Services.RecipeImport;

public class RecipeImageExtractor : IRecipeImageExtractor
{
    private static readonly string[] StepSelectors =
    {
        ".recipe-step-img img",
        ".step-image img",
        ".method-step img",
        ".mntl-sc-block-image img",
        ".structured-ingredients__list-item img",
        ".recipe__steps img",
        ".instructions img",
        ".mntl-sc-block-html img",
        "[class*='instruction'] img",
        "[class*='step'] img:not([class*='thumbnail'])",
        ".directions img",
        "li[class*='instruction'] img",
        "div[class*='instruction'] img",
        "ol[class*='step'] img",
    };

    private static readonly string[] ContentSelectors =
    {
        ".recipe-content img",
        "#recipe img",
        "article img",
        "[itemtype*='Recipe'] img",
    };

    public Task<List<RecipeImage>> ExtractImagesAsync(string html, Uri baseUrl)
    {
        var images = new List<RecipeImage>();
        var seenUrls = new HashSet<string>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Try step-level selectors first
        foreach (var selector in StepSelectors)
        {
            if (TryExtract(doc, selector)) break;
        }

        // Fall back to content-area selectors
        if (images.Count == 0)
        {
            foreach (var selector in ContentSelectors)
            {
                if (TryExtract(doc, selector)) break;
            }
        }

        return Task.FromResult(images);

        bool TryExtract(HtmlDocument d, string selector)
        {
            try
            {
                var nodes = d.DocumentNode.SelectNodes(selector);
                if (nodes == null) return false;

                foreach (var node in nodes)
                {
                    var src = node.GetAttributeValue("src", null)
                              ?? node.GetAttributeValue("data-src", null)
                              ?? node.GetAttributeValue("data-lazy-src", null)
                              ?? node.GetAttributeValue("data-original", null);

                    if (string.IsNullOrWhiteSpace(src)) continue;
                    if (src.StartsWith("data:")) continue;

                    // Skip tiny images (likely icons)
                    var wStr = node.GetAttributeValue("width", "0");
                    var hStr = node.GetAttributeValue("height", "0");
                    if (int.TryParse(wStr, out var w) && int.TryParse(hStr, out var h)
                        && w > 0 && w < 100 && h > 0 && h < 100)
                        continue;

                    string fullUrl;
                    try
                    {
                        fullUrl = new Uri(baseUrl, src).AbsoluteUri;
                    }
                    catch
                    {
                        continue;
                    }

                    if (!seenUrls.Add(fullUrl)) continue;

                    images.Add(new RecipeImage
                    {
                        Ref = $"img_{images.Count + 1}",
                        Url = fullUrl
                    });
                }

                return images.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/Services/RecipeImport/RecipeImageExtractor.cs
git commit -m "feat: add RecipeImageExtractor"
```

---

### Task 9: Create RecipeIngredientParser

**Files:**
- Create: `FridgeScan/Services/RecipeImport/RecipeIngredientParser.cs`

- [ ] **Step 1: Create RecipeIngredientParser.cs**

```csharp
using System.Globalization;
using System.Text.RegularExpressions;

namespace FridgeScan.Services.RecipeImport;

public class RecipeIngredientParser : IRecipeIngredientParser
{
    private static readonly HashSet<string> KnownUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "g", "kg", "mg",
        "ml", "l", "cl", "dl",
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
        "pack", "packet",
    };

    // Matches: "250g" or "1.5kg" or "½cup"
    private static readonly Regex AffixedQuantityRegex = new(
        @"^([\d.\s/½⅓¼⅛⅔¾⅜⅝⅞⅕⅙]+)\s*([a-zA-Z]+)$",
        RegexOptions.Compiled);

    // Matches a leading quantity (number, decimal, fraction, mixed)
    private static readonly Regex LeadingQuantityRegex = new(
        @"^([\d.,]+\s*(?:[½⅓¼⅛⅔¾⅜⅝⅞⅕⅙]|\d+/\d+)?)\s+(.*)",
        RegexOptions.Compiled);

    public List<ParsedIngredient> Parse(List<string> rawIngredients)
    {
        var results = new List<ParsedIngredient>();

        foreach (var raw in rawIngredients)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var ingredient = raw.Trim();

            // Normalize: convert comma decimal to dot
            ingredient = ingredient.Replace(',', '.');

            var parsed = new ParsedIngredient();

            // Try to extract a leading quantity + unit
            var remaining = TryExtractQuantityAndUnit(ingredient, parsed);

            // What's left is the name (possibly with notes)
            parsed.Name = remaining;

            results.Add(parsed);
        }

        return results;
    }

    private string TryExtractQuantityAndUnit(string input, ParsedIngredient parsed)
    {
        // Case: "250g flour" or "1.5kg meat" (unit affixed to quantity)
        var affixMatch = AffixedQuantityRegex.Match(input);
        if (affixMatch.Success)
        {
            var qtyStr = affixMatch.Groups[1].Value;
            var unitStr = affixMatch.Groups[2].Value.ToLowerInvariant();

            if (KnownUnits.Contains(unitStr))
            {
                parsed.Quantity = ParseFraction(qtyStr);
                parsed.Unit = unitStr;
                return input[(affixMatch.Length)..].Trim();
            }
        }

        // Case: "1 ½ cups flour" or "250 g flour" (space-separated)
        var leadMatch = LeadingQuantityRegex.Match(input);
        if (leadMatch.Success)
        {
            var qtyStr = leadMatch.Groups[1].Value;
            var rest = leadMatch.Groups[2].Value;

            // Check if rest starts with a known unit
            foreach (var unit in KnownUnits.OrderByDescending(u => u.Length))
            {
                if (rest.StartsWith(unit, StringComparison.OrdinalIgnoreCase))
                {
                    var afterUnit = rest[unit.Length..];
                    // Only match if followed by space or end
                    if (afterUnit.Length == 0 || char.IsWhiteSpace(afterUnit[0]))
                    {
                        parsed.Quantity = ParseFraction(qtyStr);
                        parsed.Unit = unit.ToLowerInvariant();
                        return afterUnit.Trim();
                    }
                }
            }

            // No unit found, just a leading number — whole rest is name
        }

        // Case: no quantity at all — the whole string is the name
        return input;
    }

    private static float? ParseFraction(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Normalize unicode fractions
        var normalized = input
            .Replace("½", "1/2").Replace("⅓", "1/3")
            .Replace("¼", "1/4").Replace("⅛", "1/8")
            .Replace("⅔", "2/3").Replace("¾", "3/4")
            .Replace("⅜", "3/8").Replace("⅝", "5/8")
            .Replace("⅞", "7/8").Replace("⅕", "1/5")
            .Replace("⅙", "1/6");

        // Mixed number like "1 1/2"
        var spaceIdx = normalized.IndexOf(' ');
        if (spaceIdx > 0)
        {
            var whole = ParseFraction(normalized[..spaceIdx]);
            var frac = ParseFraction(normalized[(spaceIdx + 1)..]);
            return (whole ?? 0) + (frac ?? 0);
        }

        // Fraction like "1/2"
        var slashIdx = normalized.IndexOf('/');
        if (slashIdx > 0)
        {
            if (float.TryParse(normalized[..slashIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var n) &&
                float.TryParse(normalized[(slashIdx + 1)..], NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d != 0)
                return n / d;
        }

        // Plain number
        if (float.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return value;

        return null;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/Services/RecipeImport/RecipeIngredientParser.cs
git commit -m "feat: add RecipeIngredientParser for structured ingredient parsing"
```

---

### Task 10: Create RecipeImportService (orchestrator)

**Files:**
- Create: `FridgeScan/Services/RecipeImport/RecipeImportService.cs`

- [ ] **Step 1: Create RecipeImportService.cs**

```csharp
namespace FridgeScan.Services.RecipeImport;

public class RecipeImportService
{
    private readonly IReadOnlyList<IRecipeExtractor> _extractors;
    private readonly IRecipeImageExtractor _imageExtractor;
    private readonly IRecipeIngredientParser _ingredientParser;

    public RecipeImportService(
        IEnumerable<IRecipeExtractor> extractors,
        IRecipeImageExtractor imageExtractor,
        IRecipeIngredientParser ingredientParser)
    {
        _extractors = extractors.OrderByDescending(e => e.Priority).ToList();
        _imageExtractor = imageExtractor;
        _ingredientParser = ingredientParser;
    }

    public async Task<RecipeSuggestion?> ImportFromUrlAsync(string url)
    {
        string html;
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(15);
            html = await client.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RecipeImportService: fetch failed: {ex.Message}");
            return null;
        }

        var baseUrl = new Uri(url);

        // Run all extractors in parallel
        var extractTasks = _extractors.Select(e => e.ExtractAsync(html, baseUrl));
        var results = await Task.WhenAll(extractTasks);

        // Merge results by priority
        var merged = MergeResults(results);
        if (!merged.Success)
            return null;

        // Parse ingredients
        List<ParsedIngredient> parsedIngredients = new();
        if (merged.Ingredients is { Count: > 0 })
        {
            parsedIngredients = _ingredientParser.Parse(merged.Ingredients);
        }

        // Extract images
        var images = await _imageExtractor.ExtractImagesAsync(html, baseUrl);

        // Build RecipeSuggestion
        var recipe = new RecipeSuggestion
        {
            Name = merged.Name ?? "Unknown Recipe",
            Url = url,
            Ingredients = merged.Ingredients ?? new List<string>(),
            MethodSteps = merged.MethodSteps ?? new List<string>(),
            PrepTime = merged.PrepTime ?? string.Empty,
            CookTime = merged.CookTime ?? string.Empty,
            Serving = merged.Servings ?? string.Empty,
            Difficulty = merged.Difficulty ?? string.Empty,
            ImageUrl = merged.ImageUrl ?? (images.FirstOrDefault()?.Url ?? string.Empty),
            RecipeSource = merged.RecipeSource ?? "import",
            Nutritions = merged.Nutritions ?? new List<string>(),
        };

        return recipe;
    }

    private static RecipeExtractionResult MergeResults(RecipeExtractionResult[] results)
    {
        var merged = new RecipeExtractionResult();

        foreach (var r in results)
        {
            if (!r.Success) continue;

            merged.Success = true;

            merged.Name ??= r.Name;
            merged.Description ??= r.Description;
            merged.ImageUrl ??= r.ImageUrl;
            merged.Author ??= r.Author;
            merged.PrepTime ??= r.PrepTime;
            merged.CookTime ??= r.CookTime;
            merged.TotalTime ??= r.TotalTime;
            merged.Servings ??= r.Servings;
            merged.Difficulty ??= r.Difficulty;
            merged.RatingValue ??= r.RatingValue;
            merged.RatingCount ??= r.RatingCount;
            merged.RecipeSource ??= r.RecipeSource;
            merged.ContentType ??= r.ContentType;
            merged.IsPremium = merged.IsPremium || r.IsPremium;

            if (merged.Ingredients is not { Count: > 0 })
                merged.Ingredients = r.Ingredients;

            if (merged.MethodSteps is not { Count: > 0 })
                merged.MethodSteps = r.MethodSteps;

            if (merged.Nutritions is not { Count: > 0 })
                merged.Nutritions = r.Nutritions;
        }

        return merged;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/Services/RecipeImport/RecipeImportService.cs
git commit -m "feat: add RecipeImportService orchestrator"
```

---

### Task 11: Wire up DI and update SharedRecipeViewModel + page

**Files:**
- Modify: `FridgeScan/MauiProgram.cs`
- Modify: `FridgeScan/ViewModels/SharedRecipeViewModel.cs`
- Modify: `FridgeScan/Views/SharedRecipePage.xaml`

- [ ] **Step 1: Register new services in MauiProgram.cs**

Add using at top of `MauiProgram.cs`:

```csharp
using FridgeScan.Services.RecipeImport;
```

Add after the existing `builder.Services.AddSingleton<SharedRecipeViewModel>();` line:

```csharp
// Recipe import pipeline
builder.Services.AddSingleton<IRecipeImageExtractor, RecipeImageExtractor>();
builder.Services.AddSingleton<IRecipeIngredientParser, RecipeIngredientParser>();
builder.Services.AddSingleton<IRecipeExtractor, JsonLdRecipeExtractor>();
builder.Services.AddSingleton<IRecipeExtractor, NextDataRecipeExtractor>();
builder.Services.AddSingleton<IRecipeExtractor, PostContentRecipeExtractor>();
builder.Services.AddSingleton<IRecipeExtractor, MicrodataRecipeExtractor>();
builder.Services.AddSingleton<RecipeImportService>();
```

- [ ] **Step 2: Rewrite SharedRecipeViewModel.cs with import logic**

Replace the entire file content:

```csharp
using FridgeScan.Services.RecipeImport;

namespace FridgeScan.ViewModels;

public partial class SharedRecipeViewModel : BaseViewModel, IQueryAttributable
{
    private readonly RecipeImportService _importService;

    [ObservableProperty]
    private string sharedUrl = string.Empty;

    [ObservableProperty]
    private string pageTitle = "Import Recipe";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private RecipeSuggestion? importedRecipe;

    [ObservableProperty]
    private bool hasRecipe;

    [ObservableProperty]
    private bool hasError;

    public SharedRecipeViewModel(RecipeImportService importService)
    {
        _importService = importService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("url", out var url))
        {
            var decoded = Uri.UnescapeDataString(url?.ToString() ?? string.Empty);
            SharedUrl = decoded;
            _ = ImportRecipeAsync(decoded);
        }
    }

    private async Task ImportRecipeAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        IsLoading = true;
        HasRecipe = false;
        HasError = false;
        PageTitle = "Importing...";

        try
        {
            ImportedRecipe = await _importService.ImportFromUrlAsync(url);

            if (ImportedRecipe != null)
            {
                HasRecipe = true;
                PageTitle = ImportedRecipe.Name ?? "Imported Recipe";
            }
            else
            {
                HasError = true;
                PageTitle = "Import Failed";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Import failed: {ex.Message}");
            HasError = true;
            PageTitle = "Import Failed";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Close()
    {
        await Shell.Current.GoToAsync("..");
    }
}
```

- [ ] **Step 3: Rewrite SharedRecipePage.xaml with import result UI**

Replace the entire file content:

```xml
<ContentPage
    x:Class="FridgeScan.Views.SharedRecipePage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:button="clr-namespace:Syncfusion.Maui.Buttons;assembly=Syncfusion.Maui.Buttons"
    xmlns:sfBusy="clr-namespace:Syncfusion.Maui.Core;assembly=Syncfusion.Maui.Core">

    <Shell.NavBarIsVisible>True</Shell.NavBarIsVisible>

    <Grid RowDefinitions="Auto,*" Padding="20">

        <!-- Header -->
        <VerticalStackLayout Grid.Row="0" Spacing="10">
            <Label
                FontAttributes="Bold"
                FontSize="24"
                Text="{Binding PageTitle}" />

            <Label
                FontSize="14"
                Text="{Binding SharedUrl}"
                TextColor="Gray"
                LineBreakMode="TailTruncation" />

            <BoxView HeightRequest="1" Color="LightGray" />
        </VerticalStackLayout>

        <!-- Content -->
        <ScrollView Grid.Row="1">
            <VerticalStackLayout Spacing="12">

                <!-- Loading -->
                <VerticalStackLayout IsVisible="{Binding IsLoading}" Spacing="8" VerticalOptions="Center" HorizontalOptions="Center" Padding="0,40">
                    <sfBusy:SfBusyIndicator
                        AnimationType="CircularMaterial"
                        IsRunning="{Binding IsLoading}"
                        HorizontalOptions="Center"
                        HeightRequest="60"
                        WidthRequest="60" />
                    <Label Text="Importing recipe..." HorizontalOptions="Center" TextColor="Gray" />
                </VerticalStackLayout>

                <!-- Imported recipe -->
                <VerticalStackLayout IsVisible="{Binding HasRecipe}" Spacing="12">

                    <Label FontSize="20" FontAttributes="Bold" Text="{Binding ImportedRecipe.Name}" />

                    <Image
                        Source="{Binding ImportedRecipe.ImageUrl}"
                        Aspect="AspectFill"
                        HeightRequest="200" />

                    <HorizontalStackLayout Spacing="16">
                        <Label FontSize="14" Text="{Binding ImportedRecipe.PrepTime, StringFormat='Prep: {0}'}" TextColor="Gray" />
                        <Label FontSize="14" Text="{Binding ImportedRecipe.CookTime, StringFormat='Cook: {0}'}" TextColor="Gray" />
                        <Label FontSize="14" Text="{Binding ImportedRecipe.Difficulty}" TextColor="Gray" />
                        <Label FontSize="14" Text="{Binding ImportedRecipe.Serving, StringFormat='Serves: {0}'}" TextColor="Gray" />
                    </HorizontalStackLayout>

                    <Label FontSize="16" FontAttributes="Bold" Text="Ingredients" Margin="0,8,0,0" />
                    <CollectionView ItemsSource="{Binding ImportedRecipe.Ingredients}" SelectionMode="None">
                        <CollectionView.ItemTemplate>
                            <DataTemplate x:DataType="x:String">
                                <Label FontSize="14" Text="{Binding .}" Padding="4,2" />
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>

                    <Label FontSize="16" FontAttributes="Bold" Text="Method" Margin="0,8,0,0" />
                    <CollectionView ItemsSource="{Binding ImportedRecipe.MethodSteps}" SelectionMode="None">
                        <CollectionView.ItemTemplate>
                            <DataTemplate x:DataType="x:String">
                                <HorizontalStackLayout Padding="4,4" Spacing="8">
                                    <Label FontSize="14" Text="&#x2022;" />
                                    <Label FontSize="14" Text="{Binding .}" />
                                </HorizontalStackLayout>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>
                </VerticalStackLayout>

                <!-- Error state -->
                <Label
                    IsVisible="{Binding HasError}"
                    FontSize="14"
                    Text="Could not import recipe from this URL."
                    TextColor="Gray"
                    HorizontalOptions="Center"
                    Padding="0,40" />

                <button:SfButton
                    Command="{Binding CloseCommand}"
                    CornerRadius="6"
                    HeightRequest="44"
                    HorizontalOptions="Center"
                    Text="Close"
                    WidthRequest="120" />
            </VerticalStackLayout>
        </ScrollView>
    </Grid>
</ContentPage>
```

- [ ] **Step 4: Commit**

```bash
git add FridgeScan/MauiProgram.cs FridgeScan/ViewModels/SharedRecipeViewModel.cs FridgeScan/Views/SharedRecipePage.xaml
git commit -m "feat: wire up RecipeImportService to SharedRecipeViewModel and UI"
```

---

### Task 12: Build verification

- [ ] **Step 1: Build the Android target**

Run: `dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android`
Expected: 0 errors

- [ ] **Step 2: Fix any build errors**

Inspect compiler output and fix any missing usings, type mismatches, or namespace issues.
