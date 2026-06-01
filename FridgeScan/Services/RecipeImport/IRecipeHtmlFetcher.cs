namespace FridgeScan.Services.RecipeImport;

/// <summary>
/// Fetches raw HTML from a recipe URL using platform-native capabilities.
/// Used as a fallback when the standard HttpClient is blocked by bot protection.
/// </summary>
public interface IRecipeHtmlFetcher
{
    /// <summary>
    /// Loads the URL via a platform WebView (real browser) and returns the page HTML.
    /// Returns null on failure or if the platform doesn't support headless WebView usage.
    /// </summary>
    Task<string?> FetchHtmlAsync(string url, CancellationToken ct = default);
}
