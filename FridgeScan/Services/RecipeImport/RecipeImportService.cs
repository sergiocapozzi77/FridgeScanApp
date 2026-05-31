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

        var extractTasks = _extractors.Select(e => e.ExtractAsync(html, baseUrl));
        var results = await Task.WhenAll(extractTasks);

        var merged = MergeResults(results);
        if (!merged.Success)
            return null;

        List<ParsedIngredient> parsedIngredients = new();
        if (merged.Ingredients is { Count: > 0 })
        {
            parsedIngredients = _ingredientParser.Parse(merged.Ingredients);
            for (int i = 0; i < parsedIngredients.Count && i < merged.Ingredients.Count; i++)
            {
                parsedIngredients[i].Original = merged.Ingredients[i];
            }
        }

        var images = await _imageExtractor.ExtractImagesAsync(html, baseUrl);

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
            ParsedIngredients = parsedIngredients,
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
