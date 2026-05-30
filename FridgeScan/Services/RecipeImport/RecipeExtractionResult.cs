namespace FridgeScan.Services.RecipeImport;

public class RecipeExtractionResult
{
    public bool Success { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Author { get; set; }
    public string? PrepTime { get; set; }
    public string? CookTime { get; set; }
    public string? TotalTime { get; set; }
    public string? Servings { get; set; }
    public string? Difficulty { get; set; }
    public float? RatingValue { get; set; }
    public int? RatingCount { get; set; }
    public List<string>? Ingredients { get; set; }
    public List<string>? MethodSteps { get; set; }
    public List<string>? Nutritions { get; set; }
    public bool IsPremium { get; set; }
    public string? ContentType { get; set; }
    public string? RecipeSource { get; set; }
}
