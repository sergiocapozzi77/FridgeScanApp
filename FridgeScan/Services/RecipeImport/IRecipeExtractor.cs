namespace FridgeScan.Services.RecipeImport;

public interface IRecipeExtractor
{
    int Priority { get; }
    Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl);
}
