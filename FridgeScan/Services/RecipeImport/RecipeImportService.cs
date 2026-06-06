namespace FridgeScan.Services.RecipeImport;

public class RecipeImportService
{
    private const string Tag = "FridgeScan.RecipeImport";

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
        var fetchResult = await FetchHtmlAsync(url);
        if (fetchResult == null)
            return null;

        var html = fetchResult.Html;
        var baseUrl = fetchResult.FinalUri;

        var extractTasks = _extractors.Select(e => e.ExtractAsync(html, baseUrl));
        var results = await Task.WhenAll(extractTasks);

        // Log per-extractor results
        for (int i = 0; i < _extractors.Count; i++)
        {
            var r = results[i];
            Logger.Debug(Tag, $"extractor #{_extractors[i].Priority} success={r.Success}, name='{r.Name}', ingredients={r.Ingredients?.Count ?? 0}, steps={r.MethodSteps?.Count ?? 0}");
        }

        var merged = MergeResults(results);
        if (!merged.Success)
        {
            Logger.Debug(Tag, $"merge: all {results.Length} extractors returned empty results — cannot import");
            return null;
        }

        var images = await _imageExtractor.ExtractImagesAsync(html, baseUrl);

        var recipe = new RecipeSuggestion
        {
            Name = merged.Name ?? "Unknown Recipe",
            Url = baseUrl.ToString(),
            Ingredients = merged.Ingredients ?? new List<string>(),
            MethodSteps = merged.MethodSteps ?? new List<InstructionSection>(),
            PrepTime = merged.PrepTime ?? string.Empty,
            CookTime = merged.CookTime ?? string.Empty,
            Serving = merged.Servings ?? string.Empty,
            Difficulty = merged.Difficulty ?? string.Empty,
            ImageUrl = merged.ImageUrl ?? (images.FirstOrDefault()?.Url ?? string.Empty),
            RecipeSource = merged.RecipeSource ?? "import",
            Nutritions = merged.Nutritions ?? new List<string>(),
        };

        Logger.Debug(Tag, $"imported recipe: name='{recipe.Name}', url='{recipe.Url}', ingredients={recipe.Ingredients.Count}, steps={recipe.MethodSteps.Count}, source='{recipe.RecipeSource}'");

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
    /// Holds the fetched HTML together with the final URI after any HTTP redirects.
    /// This is critical for share.google and other short-link redirectors where
    /// the resolved URL is needed to correctly resolve relative image links.
    /// </summary>
    private sealed record FetchResult(string Html, Uri FinalUri);

    /// <summary>
    /// Attempts to fetch HTML from the URL, first via HttpClient (fast path),
    /// falling back to a platform WebView when the server blocks the request.
    /// Returns both the HTML content and the final URI after all redirects.
    /// </summary>
    private async Task<FetchResult?> FetchHtmlAsync(string url)
    {
        // ---- Attempt 1: Direct HttpClient (fast, lightweight) ----
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            // After following redirects, RequestUri reflects the final URL.
            var finalUri = response.RequestMessage?.RequestUri ?? new Uri(url);

            if (finalUri.ToString() != url)
                Logger.Debug(Tag, $"redirect resolved: '{url}' -> '{finalUri}'");

            return new FetchResult(html, finalUri);
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            var httpStatus = (int)ex.StatusCode;
            Logger.Debug(Tag, $"HTTP {httpStatus} fetching {url}");

            // ---- Attempt 2: WebView fallback for server errors (bot protection) ----
            if (_webViewFetcher != null)
            {
                Logger.Debug(Tag, "WebView fallback...");
                var webHtml = await _webViewFetcher.FetchHtmlAsync(url);
                if (webHtml != null)
                {
                    Logger.Debug(Tag, $"WebView fallback succeeded ({webHtml.Length} chars)");
                    // WebView doesn't track final redirect URL, so use the original
                    return new FetchResult(webHtml, new Uri(url));
                }
                Logger.Debug(Tag, "WebView fallback also failed");
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
            Logger.Debug(Tag, $"connection error fetching {url}");
            LastErrorMessage = "Could not connect to the website. Check your internet connection.";
            return null;
        }
        catch (TaskCanceledException)
        {
            Logger.Debug(Tag, $"timeout fetching {url}");
            LastErrorMessage = "Request timed out. The website may be too slow or unreachable.";
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"fetch failed: {ex.Message} [{url}]");
            LastErrorMessage = "Could not import recipe from this URL.";
            return null;
        }
    }

}
