namespace FridgeScan.Models;

public class Cookbook
{
    public string RowId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RecipeCount { get; set; }
    public List<string> PreviewImageUrls { get; set; } = new();
    public bool IsSelected { get; set; }
}
