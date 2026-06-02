using System.Text.Json.Serialization;

namespace FridgeScan.Models;

public class AppwriteRowsResponse
{
    public int Total { get; set; }
    public List<AppwriteRow> Rows { get; set; } = new();
}

public class AppwriteRow
{
    [JsonPropertyName("$id")]
    public string Id { get; set; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement> Data { get; set; } = new();
}
