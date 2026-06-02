namespace FridgeScan.Models;

public class SavedRecipe
{
    public string RowId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageUrlBig { get; set; }
    public string? Description { get; set; }
    public string? Difficulty { get; set; }
    public string? TotalTime { get; set; }
    public string? RecipeSource { get; set; }
    public List<string> CookbookIds { get; set; } = new();
    public List<string> Ingredients { get; set; } = new();
    public List<string> MethodSteps { get; set; } = new();
}
