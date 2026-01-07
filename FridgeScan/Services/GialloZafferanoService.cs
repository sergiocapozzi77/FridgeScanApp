using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Net;

namespace FridgeScan.Services;

public class GialloZafferanoService : IRecipeService
{
    private readonly HttpClient _httpClient;
    private static readonly Random _rng = new Random();
    private readonly JsonLdParser jsonLdParser;

    public GialloZafferanoService(JsonLdParser jsonLdParser)
    {
        this.jsonLdParser = jsonLdParser;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<RecipeSuggestion> GetFullRecipeDetailsAsync(string url)
    {
        return await jsonLdParser.GetFullRecipeDetailsAsync(url);
    }

    public async Task<List<RecipeSuggestion>> GetRecipeSuggestionsAsync(List<string> ingredients, string dishType, string? difficulty, string? totalTime)
    {
        // 1. Shuffle iniziale degli ingredienti per cambiare la query alla base
        var shuffledQuery = string.Join("+", ingredients.OrderBy(a => _rng.Next()));
        string encodedQuery = Uri.EscapeDataString(shuffledQuery);
        string encodedDishType = Uri.EscapeDataString(dishType.ToLower().Replace(" ", "-"));

        // 2. Prepariamo i Task per scaricare pagina 1 e pagina 2 in parallelo
        var pageTasks = new List<Task<List<RecipeSuggestion>>>
        {
            FetchPageAsync(encodedQuery, encodedDishType, difficulty, totalTime, 1),
            FetchPageAsync(encodedQuery, encodedDishType, difficulty, totalTime, 2)
        };

        // Attendiamo il completamento di entrambi
        var results = await Task.WhenAll(pageTasks);

        // 3. Uniamo i risultati e prendiamo 5 ricette a caso dal totale (circa 60 ricette)
        return results.SelectMany(x => x)
                      .OrderBy(a => _rng.Next())
                      .Take(5)
                      .ToList();
    }

    private async Task<List<RecipeSuggestion>> FetchPageAsync(
    string query,
    string dishType,
    string? difficulty,
    string? totalTime,
    int page)
    {
        var pageSuggestions = new List<RecipeSuggestion>();

        try
        {
            string url = $"https://www.giallozafferano.com/recipes-search/{query}/page{page}/";
            string html = await _httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // All recipe cards
            var cards = doc.DocumentNode.SelectNodes("//article[contains(@class,'gz-card')]");

            if (cards == null)
                return pageSuggestions;

            foreach (var card in cards)
            {
                // Title + link
                var linkNode = card.SelectSingleNode(".//h2[contains(@class,'gz-title')]/a");
                var title = linkNode?.InnerText.Trim() ?? "No title";
                var urlRecipe = linkNode?.GetAttributeValue("href", string.Empty) ?? "";

                // Absolute URL
                if (!urlRecipe.StartsWith("http"))
                    urlRecipe = $"https://www.giallozafferano.com{urlRecipe}";

                // Image
                var imgNode = card.SelectSingleNode(".//div[contains(@class,'gz-card-image')]//img");
                var imageUrl = imgNode?.GetAttributeValue("src", string.Empty) ?? "";

                // Description
                var descNode = card.SelectSingleNode(".//div[contains(@class,'gz-description')]");
                var description = HtmlEntity.DeEntitize(descNode?.InnerText.Trim() ?? "");

                // Difficulty + Time (inside <ul> list)
                var dataNodes = card.SelectNodes(".//ul[contains(@class,'gz-card-data')]//li");

                string difficultyText = "";
                string timeText = "";

                if (dataNodes != null)
                {
                    foreach (var li in dataNodes)
                    {
                        var text = li.InnerText.Trim();

                        if (text.Contains("easy", StringComparison.OrdinalIgnoreCase))
                            difficultyText = text;

                        if (text.Contains("min") || text.Contains("h"))
                            timeText = text;
                    }
                }

                pageSuggestions.Add(new RecipeSuggestion
                {
                    Name = title,
                    Url = urlRecipe,
                    ImageUrl = imageUrl,
                    PrepTime = ParseMinutes(timeText).ToString(),
                    Difficulty = string.IsNullOrWhiteSpace(difficultyText) ? "Unknown" : difficultyText,
                    RecipeSource = "giallozafferano",
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore a pagina {page}: {ex.Message}");
        }

        return pageSuggestions;
    }


    private int ParseMinutes(string timeInput)
    {
        if (string.IsNullOrEmpty(timeInput)) return 0;
        var numbers = new string(timeInput.Where(char.IsDigit).ToArray());
        return int.TryParse(numbers, out int result) ? result : 0;
    }

    
}