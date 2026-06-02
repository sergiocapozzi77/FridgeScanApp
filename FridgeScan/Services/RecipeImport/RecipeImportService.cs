namespace FridgeScan.Services.RecipeImport;

public class RecipeImportService
{
    private readonly IReadOnlyList<IRecipeExtractor> _extractors;
    private readonly IRecipeImageExtractor _imageExtractor;
    private readonly IRecipeHtmlFetcher? _webViewFetcher;

    /// <summary>Holds the user-facing error message from the last failed import attempt.</summary>
    public string? LastErrorMessage { get; private set; }

    public RecipeImportService(
        IEnumerable<IRecipeExtractor> extractors,
        IRecipeImageExtractor imageExtractor,
        IRecipeHtmlFetcher? webViewFetcher = null)
    {
        _extractors = extractors.OrderByDescending(e => e.Priority).ToList();
        _imageExtractor = imageExtractor;
        _webViewFetcher = webViewFetcher;
    }

    private static readonly HttpClient _httpClient = CreateHttpClient();

    public async Task<RecipeSuggestion?> ImportFromUrlAsync(string url)
    {
        LastErrorMessage = null;
        var html = await FetchHtmlAsync(url);
        if (html == null)
            return null;

        var baseUrl = new Uri(url);

        var extractTasks = _extractors.Select(e => e.ExtractAsync(html, baseUrl));
        var results = await Task.WhenAll(extractTasks);

        var merged = MergeResults(results);
        if (!merged.Success)
            return null;

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

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
        };

        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        client.Timeout = TimeSpan.FromSeconds(15);

        return client;
    }

    /// <summary>
    /// Attempts to fetch HTML from the URL, first via HttpClient (fast path),
    /// falling back to a platform WebView when the server blocks the request.
    /// </summary>
    private async Task<string?> FetchHtmlAsync(string url)
    {
        // ---- Attempt 1: Direct HttpClient (fast, lightweight) ----
        try
        {
            return await _httpClient.GetStringAsync(url);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            var httpStatus = (int)ex.StatusCode;
            System.Diagnostics.Debug.WriteLine(
                $"RecipeImportService: HTTP {httpStatus} fetching {url}");

            // ---- Attempt 2: WebView fallback for server errors (bot protection) ----
            if (_webViewFetcher != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "RecipeImportService: WebView fallback...");
                var webHtml = await _webViewFetcher.FetchHtmlAsync(url);
                if (webHtml != null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"RecipeImportService: WebView fallback succeeded ({webHtml.Length} chars)");
                    return webHtml;
                }
                System.Diagnostics.Debug.WriteLine(
                    "RecipeImportService: WebView fallback also failed");
            }

            LastErrorMessage = httpStatus switch
            {
                402 => "The website blocked the request (HTTP 402). This usually means the site has bot protection enabled.",
                403 => "The website blocked the request (HTTP 403). This site may not allow recipe importing.",
                404 => "Recipe page not found (HTTP 404). The URL may be invalid.",
                >= 400 => $"Could not import recipe (server returned HTTP {httpStatus}).",
                _ => "Could not import recipe from this URL."
            };
            return null;
        }
        catch (HttpRequestException)
        {
            // Connection-level error (DNS, TLS, etc.) — WebView won't help
            System.Diagnostics.Debug.WriteLine($"RecipeImportService: connection error fetching {url}");
            LastErrorMessage = "Could not connect to the website. Check your internet connection.";
            return null;
        }
        catch (TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"RecipeImportService: timeout fetching {url}");
            LastErrorMessage = "Request timed out. The website may be too slow or unreachable.";
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RecipeImportService: fetch failed: {ex.Message} [{url}]");
            LastErrorMessage = "Could not import recipe from this URL.";
            return null;
        }
    }

}
