using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FridgeScan.Models;

namespace FridgeScan.Services;

public class CookbookService
{
    private readonly HttpClient _http;
    private const string Endpoint = "https://fra.cloud.appwrite.io/v1";
    private const string ProjectId = "6954045e003c75c1c3bf";
    private const string DatabaseId = "695404ac0021bf7d9707";
    private const string CookbooksCollectionId = "cookbooks";
    private const string RecipesCollectionId = "recipes";

    public CookbookService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("X-Appwrite-Project", ProjectId);
        _http.DefaultRequestHeaders.Add("X-Appwrite-Key", Secrets.AppWriteApiKey);
    }

    // --- Cookbooks ---

    public async Task<List<Cookbook>> GetCookbooksAsync()
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CookbooksCollectionId}/rows";
            var allRows = await FetchAllRowsAsync(url);
            return allRows.Select(r => new Cookbook
            {
                RowId = r.Id,
                Name = GetStringOrNull(r, "name") ?? string.Empty
            }).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching cookbooks: {ex.Message}");
            return new List<Cookbook>();
        }
    }

    public async Task<Cookbook?> CreateCookbookAsync(string name)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CookbooksCollectionId}/rows";
            var body = new
            {
                rowId = GenerateId(),
                data = new { name }
            };
            var response = await _http.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            var row = await response.Content.ReadFromJsonAsync<AppwriteRow>();
            return row == null ? null : new Cookbook { RowId = row.Id, Name = name };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating cookbook: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteCookbookAsync(string rowId)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CookbooksCollectionId}/rows/{rowId}";
            var response = await _http.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting cookbook: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RenameCookbookAsync(string rowId, string name)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CookbooksCollectionId}/rows/{rowId}";
            var body = new { data = new { name } };
            var response = await _http.PatchAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error renaming cookbook: {ex.Message}");
            return false;
        }
    }

    // --- Recipes ---

    public async Task<List<SavedRecipe>> GetRecipesByCookbookIdAsync(string cookbookId)
    {
        try
        {
            var baseUrl = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows";
            var query = $"{{\"method\":\"contains\",\"attribute\":\"cookbookIds\",\"values\":[\"{cookbookId}\"]}}";
            var encoded = new List<string> { $"queries[0]={Uri.EscapeDataString(query)}" };
            var allRows = await FetchAllRowsAsync(baseUrl, encoded);
            return allRows.Select(MapToSavedRecipe).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching recipes: {ex.Message}");
            return new List<SavedRecipe>();
        }
    }

    public async Task<List<SavedRecipe>> GetAllSavedRecipesAsync()
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows";
            var allRows = await FetchAllRowsAsync(url);
            return allRows.Select(MapToSavedRecipe).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching all recipes: {ex.Message}");
            return new List<SavedRecipe>();
        }
    }

    public async Task<SavedRecipe?> GetRecipeByIdAsync(string recipeId)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows/{recipeId}";
            var row = await _http.GetFromJsonAsync<AppwriteRow>(url);
            return row == null ? null : MapToSavedRecipe(row);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching recipe: {ex.Message}");
            return null;
        }
    }

    public async Task<SavedRecipe?> SaveRecipeAsync(SavedRecipe recipe)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows";
            var body = new
            {
                rowId = GenerateId(),
                data = new
                {
                    url = recipe.Url ?? string.Empty,
                    name = recipe.Name ?? string.Empty,
                    cookbookIds = recipe.CookbookIds,
                    imageUrl = recipe.ImageUrl ?? string.Empty,
                    description = recipe.Description ?? string.Empty,
                    difficulty = recipe.Difficulty ?? string.Empty,
                    totalTime = recipe.TotalTime ?? string.Empty,
                    recipeSource = recipe.RecipeSource ?? string.Empty,
                    ingredients = recipe.Ingredients,
                    methodSteps = recipe.MethodSteps,
                    imageUrlBig = recipe.ImageUrlBig ?? string.Empty
                }
            };
            var response = await _http.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            var row = await response.Content.ReadFromJsonAsync<AppwriteRow>();
            if (row != null) recipe.RowId = row.Id;
            return recipe;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving recipe: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateRecipeCookbooksAsync(string recipeId, List<string> cookbookIds)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows/{recipeId}";
            var body = new { data = new { cookbookIds } };
            var response = await _http.PatchAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating recipe cookbooks: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteRecipeAsync(string rowId)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows/{rowId}";
            var response = await _http.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting recipe: {ex.Message}");
            return false;
        }
    }

    // --- Helpers ---

    private async Task<List<AppwriteRow>> FetchAllRowsAsync(string baseUrl, List<string>? baseQueries = null)
    {
        baseQueries ??= new List<string>();
        var allRows = new List<AppwriteRow>();
        const int perPage = 100;
        int offset = 0;
        int total = int.MaxValue;

        while (allRows.Count < total)
        {
            var encoded = new List<string>(baseQueries);
            var idx = encoded.Count;
            encoded.Add($"queries[{idx}]={Uri.EscapeDataString($"{{\"method\":\"limit\",\"values\":[{perPage}]}}")}");
            encoded.Add($"queries[{idx + 1}]={Uri.EscapeDataString($"{{\"method\":\"offset\",\"values\":[{offset}]}}")}");

            var queryString = "?" + string.Join("&", encoded);
            var response = await _http.GetFromJsonAsync<AppwriteRowsResponse>(baseUrl + queryString);

            if (response?.Rows == null || response.Rows.Count == 0) break;
            if (total == int.MaxValue) total = response.Total;

            allRows.AddRange(response.Rows);
            offset = allRows.Count;
            if (allRows.Count >= total) break;
        }

        return allRows;
    }

    private static SavedRecipe MapToSavedRecipe(AppwriteRow row)
    {
        return new SavedRecipe
        {
            RowId = row.Id,
            Name = GetStringOrNull(row, "name"),
            Url = GetStringOrNull(row, "url"),
            ImageUrl = GetStringOrNull(row, "imageUrl"),
            ImageUrlBig = GetStringOrNull(row, "imageUrlBig"),
            Description = GetStringOrNull(row, "description"),
            Difficulty = GetStringOrNull(row, "difficulty"),
            TotalTime = GetStringOrNull(row, "totalTime"),
            RecipeSource = GetStringOrNull(row, "recipeSource"),
            CookbookIds = GetStringList(row, "cookbookIds"),
            Ingredients = GetStringList(row, "ingredients"),
            MethodSteps = GetStringList(row, "methodSteps")
        };
    }

    private static string? GetStringOrNull(AppwriteRow row, string key)
    {
        if (row.Data.TryGetValue(key, out var el))
            return el.ValueKind == System.Text.Json.JsonValueKind.Null ? null : el.GetString();
        return null;
    }

    private static List<string> GetStringList(AppwriteRow row, string key)
    {
        if (row.Data.TryGetValue(key, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var list = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                var s = item.GetString();
                if (s != null) list.Add(s);
            }
            return list;
        }
        return new List<string>();
    }

    private static string GenerateId(int length = 20)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var buffer = new char[length];
        buffer[0] = chars[random.Next(chars.Length)];
        for (int i = 1; i < length; i++)
            buffer[i] = chars[random.Next(chars.Length)];
        return new string(buffer);
    }

    public class AppwriteRowsResponse
    {
        public int Total { get; set; }
        public List<AppwriteRow> Rows { get; set; } = new();
    }

    public class AppwriteRow
    {
        [JsonPropertyName("$id")]
        public string Id { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonExtensionData]
        public Dictionary<string, System.Text.Json.JsonElement> Data { get; set; } = new();
    }
}
