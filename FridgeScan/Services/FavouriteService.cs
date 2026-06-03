using System.Net.Http.Json;
using FridgeScan.Models;

namespace FridgeScan.Services;

public class FavouriteService
{
    private const string Tag = "FridgeScan.FavouriteService";

    private readonly HttpClient _http;
    private const string Endpoint = "https://fra.cloud.appwrite.io/v1";
    private const string ProjectId = "6954045e003c75c1c3bf";
    private const string DatabaseId = "695404ac0021bf7d9707";
    private const string FavouritesCollectionId = "favourites";

    public FavouriteService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("X-Appwrite-Project", ProjectId);
        _http.DefaultRequestHeaders.Add("X-Appwrite-Key", Secrets.AppWriteApiKey);
    }

    public async Task<List<SavedRecipe>> GetFavouritesByCookbookIdAsync(string cookbookId)
    {
        try
        {
            var baseUrl = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{FavouritesCollectionId}/rows";
            var query = $"{{\"method\":\"contains\",\"attribute\":\"cookbookIds\",\"values\":[\"{cookbookId}\"]}}";
            var encoded = new List<string> { $"queries[0]={Uri.EscapeDataString(query)}" };
            var allRows = await FetchAllRowsAsync(baseUrl, encoded);
            return allRows.Select(MapToSavedRecipe).ToList();
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Error fetching favourites: {ex.Message}");
            return new List<SavedRecipe>();
        }
    }

    public async Task<List<SavedRecipe>> GetAllFavouritesAsync()
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{FavouritesCollectionId}/rows";
            var allRows = await FetchAllRowsAsync(url);
            return allRows.Select(MapToSavedRecipe).ToList();
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Error fetching all favourites: {ex.Message}");
            return new List<SavedRecipe>();
        }
    }

    public async Task<SavedRecipe?> GetFavouriteByIdAsync(string favouriteId)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{FavouritesCollectionId}/rows/{favouriteId}";
            var row = await _http.GetFromJsonAsync<AppwriteRow>(url);
            return row == null ? null : MapToSavedRecipe(row);
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Error fetching favourite: {ex.Message}");
            return null;
        }
    }

    public async Task<SavedRecipe?> SaveFavouriteAsync(SavedRecipe favourite)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{FavouritesCollectionId}/rows";
            var body = new
            {
                rowId = GenerateId(),
                data = new
                {
                    url = favourite.Url ?? string.Empty,
                    name = favourite.Name ?? string.Empty,
                    cookbookIds = favourite.CookbookIds,
                    imageUrl = favourite.ImageUrl ?? string.Empty,
                    description = favourite.Description ?? string.Empty,
                    difficulty = favourite.Difficulty ?? string.Empty,
                    totalTime = favourite.TotalTime ?? string.Empty,
                    recipeSource = favourite.RecipeSource ?? string.Empty,
                    ingredients = favourite.Ingredients,
                    methodSteps = favourite.MethodSteps,
                    imageUrlBig = favourite.ImageUrlBig ?? string.Empty
                }
            };
            var response = await _http.PostAsJsonAsync(url, body);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Logger.Error(Tag, $"Save favourite API error ({response.StatusCode}): {errorBody}");
            }
            response.EnsureSuccessStatusCode();
            var row = await response.Content.ReadFromJsonAsync<AppwriteRow>();
            if (row != null) favourite.RowId = row.Id;
            return favourite;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Error saving favourite: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateFavouriteCookbooksAsync(string favouriteId, List<string> cookbookIds)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{FavouritesCollectionId}/rows/{favouriteId}";
            var body = new { data = new { cookbookIds } };
            var response = await _http.PatchAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Error updating favourite cookbooks: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteFavouriteAsync(string rowId)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{FavouritesCollectionId}/rows/{rowId}";
            var response = await _http.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Error deleting favourite: {ex.Message}");
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
}
