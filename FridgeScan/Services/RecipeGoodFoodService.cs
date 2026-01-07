using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Net;

namespace FridgeScan.Services;

public class RecipeGoodFoodService : IRecipeService
{
    private readonly HttpClient _httpClient;
    private static readonly Random _rng = new Random();
    private readonly JsonLdParser jsonLdParser;

    public RecipeGoodFoodService(JsonLdParser jsonLdParser)
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
        var shuffledQuery = string.Join(" ", ingredients.OrderBy(a => _rng.Next()));
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

    private async Task<List<RecipeSuggestion>> FetchPageAsync(string query, string dishType, string? difficulty, string? totalTime, int page)
    {
        var pageSuggestions = new List<RecipeSuggestion>();
        try
        {
            string url = $"https://www.bbcgoodfood.com/search?q={query}&mealType={dishType}&page={page}";

            // 2. Aggiungiamo il filtro difficoltà solo se non è nullo o vuoto
            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                url += $"&difficulty={Uri.EscapeDataString(difficulty)}";
            }

            if (!string.IsNullOrEmpty(totalTime))
            {
                url += $"&totalTime={Uri.EscapeDataString(totalTime)}";
            }

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
                        PrepTime = ParseMinutes(item["terms"]?.FirstOrDefault(t => t["slug"]?.ToString() == "time")?["display"]?.ToString()).ToString(),
                        Difficulty = item["terms"]?.FirstOrDefault(t => t["slug"]?.ToString() == "skillLevel")?["display"]?.ToString() ?? "Easy",
                        RecipeSource = "goodfood"
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

    
    private List<string> ExtractSteps(JToken instructions)
{
    var steps = new List<string>();
    if (instructions == null) return steps;

    if (instructions is JArray arr)
    {
        foreach (var item in arr)
        {
            if (item["@type"]?.ToString() == "HowToStep")
                steps.Add(item["text"]?.ToString());
            else
                steps.Add(item.ToString());
        }
    }
    return steps.Where(s => !string.IsNullOrEmpty(s)).ToList();
}

private List<string> ExtractNutrition(JToken nutrition)
{
    var list = new List<string>();
    if (nutrition == null) return list;

    // Schema.org usa chiavi specifiche (calories, fatContent, ecc.)
    void AddIfPresent(string label, string key)
    {
        var val = nutrition[key]?.ToString();
        if (!string.IsNullOrEmpty(val)) list.Add($"{label}: {val}");
    }

    AddIfPresent("Calories", "calories");
    AddIfPresent("Fat", "fatContent");
    AddIfPresent("Saturated Fat", "saturatedFatContent");
    AddIfPresent("Carbs", "carbohydrateContent");
    AddIfPresent("Sugar", "sugarContent");
    AddIfPresent("Fiber", "fiberContent");
    AddIfPresent("Protein", "proteinContent");
    AddIfPresent("Sodium", "sodiumContent");

    return list;
}

}