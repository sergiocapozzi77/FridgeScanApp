using System.Net.Http.Json;
using FridgeScan.Models;

namespace FridgeScan.Services;

public class CookbookService
{
    private const string Tag = "FridgeScan.CookbookService";

    private readonly HttpClient _http;
    private const string Endpoint = "https://fra.cloud.appwrite.io/v1";
    private const string ProjectId = "6954045e003c75c1c3bf";
    private const string DatabaseId = "695404ac0021bf7d9707";
    private const string CookbooksCollectionId = "cookbooks";

    public CookbookService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("X-Appwrite-Project", ProjectId);
        _http.DefaultRequestHeaders.Add("X-Appwrite-Key", Secrets.AppWriteApiKey);
    }

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
            Logger.Error(Tag, $"Error fetching cookbooks: {ex.Message}");
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
            Logger.Error(Tag, $"Error creating cookbook: {ex.Message}");
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
            Logger.Error(Tag, $"Error deleting cookbook: {ex.Message}");
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
            Logger.Error(Tag, $"Error renaming cookbook: {ex.Message}");
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

    private static string? GetStringOrNull(AppwriteRow row, string key)
    {
        if (row.Data.TryGetValue(key, out var el))
            return el.ValueKind == System.Text.Json.JsonValueKind.Null ? null : el.GetString();
        return null;
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
