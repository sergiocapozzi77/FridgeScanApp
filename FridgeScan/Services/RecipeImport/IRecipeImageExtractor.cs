namespace FridgeScan.Services.RecipeImport;

public interface IRecipeImageExtractor
{
    Task<List<RecipeImage>> ExtractImagesAsync(string html, Uri baseUrl);
}
