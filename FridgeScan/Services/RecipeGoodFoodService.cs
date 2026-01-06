using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace FridgeScan.Services;

public class RecipeGoodFoodService : IRecipeService
{
    private readonly HttpClient _httpClient;
    private static readonly Random _rng = new Random();

    public RecipeGoodFoodService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<List<RecipeSuggestion>> GetRecipeSuggestionsAsync(List<string> ingredients, string dishType)
    {
        // 1. Shuffle iniziale degli ingredienti per cambiare la query alla base
        var shuffledQuery = string.Join(" ", ingredients.OrderBy(a => _rng.Next()));
        string encodedQuery = Uri.EscapeDataString(shuffledQuery);
        string encodedDishType = Uri.EscapeDataString(dishType.ToLower().Replace(" ", "-"));

        // 2. Prepariamo i Task per scaricare pagina 1 e pagina 2 in parallelo
        var pageTasks = new List<Task<List<RecipeSuggestion>>>
        {
            FetchPageAsync(encodedQuery, encodedDishType, 1),
            FetchPageAsync(encodedQuery, encodedDishType, 2)
        };

        // Attendiamo il completamento di entrambi
        var results = await Task.WhenAll(pageTasks);

        // 3. Uniamo i risultati e prendiamo 5 ricette a caso dal totale (circa 60 ricette)
        return results.SelectMany(x => x)
                      .OrderBy(a => _rng.Next())
                      .Take(5)
                      .ToList();
    }

    private async Task<List<RecipeSuggestion>> FetchPageAsync(string query, string dishType, int page)
    {
        var pageSuggestions = new List<RecipeSuggestion>();
        try
        {
            string url = $"https://www.bbcgoodfood.com/search?q={query}&dish_type={dishType}&page={page}";
            string html = await _httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var jsonNode = doc.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']");
            if (jsonNode == null) return pageSuggestions;

            var jsonData = JObject.Parse(jsonNode.InnerText);
            var items = jsonData["props"]?["pageProps"]?["searchResults"]?["items"];

            if (items != null)
            {
                foreach (var item in items)
                {
                    string relativeUrl = item["url"]?.ToString() ?? "";
                    pageSuggestions.Add(new RecipeSuggestion
                    {
                        Name = item["title"]?.ToString() ?? "No Title",
                        Url = relativeUrl.StartsWith("http") ? relativeUrl : $"https://www.bbcgoodfood.com{relativeUrl}",
                        ImageUrl = item["image"]?["url"]?.ToString() ?? "",
                        PrepTime = ParseMinutes(item["terms"]?.FirstOrDefault(t => t["slug"]?.ToString() == "time")?["display"]?.ToString()),
                        Difficulty = item["terms"]?.FirstOrDefault(t => t["slug"]?.ToString() == "skillLevel")?["display"]?.ToString() ?? "Easy"
                    });
                }
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